using BepInEx;
using RoR2;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using UnityEngine;
using System.Reflection;
using System;
using LeTai.Asset.TranslucentImage;
//using TMPro;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Events;
using BepInEx.Logging;
//using MiniRpcLib;
//using MiniRpcLib.Action;
using RoR2.UI;
//using TMPro;
using System.Collections;


namespace Paddywan
{
    /// <summary>
    /// Rebalance corpseBloom to provide greater benefits & proportional disadvantages.
    /// </summary>
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Paddywan.CorpseBloomRework", "CorpseBloomPlusPlus", "1.0.1")]
    public class CorpseBloomPlusPlus : BaseUnityPlugin
    {
        private float reserveMax = 0f;
        private float currentReserve = 0f;

        GameObject reserveRect = new GameObject();
        GameObject reserveBar = new GameObject();
        float percentReserve = 0f;
        HealthBar hpBar;
       

        public void Awake()
        {
            
            //Scale the stacks of corpseblooms to provide % HP / s increase per stack, and -%Reserve per stack
            IL.RoR2.HealthComponent.Heal += (il) =>
            {
                //Increase the amount of health that can be accumulated per second
                #region Benefit
                var c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchLdfld<HealthComponent>("repeatHealComponent"),
                    x => x.MatchLdcR4(0.1f)
                    );
                c.Index += 5;
                c.Remove();
                c.Emit(OpCodes.Mul);
                #endregion

                //decrease the total health reserve that is restored
                #region Disadvantage
                c.GotoNext(
                x => x.MatchMul(),
                x => x.MatchLdarg(0),
                x => x.MatchCallvirt<HealthComponent>("get_fullHealth")
                );
                c.Index += 3;

                //c.Emit(OpCodes.Ldc_R4, 1f); //push 1.0f to stack.
                c.Emit(OpCodes.Ldarg_0); //this.
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetField("repeatHealCount", BindingFlags.Instance | BindingFlags.NonPublic)); //this.repeatHealCount pushed to stack.
                c.Emit(OpCodes.Conv_R4); //Convert top() to float.
                c.Emit(OpCodes.Div); // (fullHealth * increaseHealingCount) / repeatHealCount

                //Do not multiply HP by rejuvi racks, appears that they either native increase the totalHP reserved, or create another instance which reserves the totalHP modified above; thus it already scales on rejuviracks positively
                //c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetField("repeatHealCount", BindingFlags.Instance | BindingFlags.NonPublic)); //this.repeatHealCount pushed to stack.
                //c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetFieldCached("repeatHealCount"));

                c.EmitDelegate<Func<float, float>>((fhp) =>
                {
                    //return 1f;
                    reserveMax = fhp;
                    return fhp;
                });
                #endregion
            };

            //Build reserve while fullHP, do not consume. Update currentReserve
            IL.RoR2.HealthComponent.RepeatHealComponent.FixedUpdate += (il) =>
            {
                var c = new ILCursor(il);
                ILLabel lab = il.DefineLabel();

                #region updateCurrentReserveHP
                c.GotoNext( //match the timer and loading of 0f onto the stack. Insert before.
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld("RoR2.HealthComponent/RepeatHealComponent", "timer"),
                    x => x.MatchLdcR4(0f)
                    );

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetNestedType("RepeatHealComponent", BindingFlags.Instance | BindingFlags.NonPublic).GetFieldCached("reserve")); //Load reserve onto the stack.
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetNestedType("RepeatHealComponent", BindingFlags.Instance | BindingFlags.NonPublic).GetFieldCached("healthComponent"));
                c.EmitDelegate<Action<float, HealthComponent>> ((curHP, hc) =>
                {
                    //if (PlayerCharacterMasterController.instances[0].Equals(hc.body.master))
                    if(LocalUserManager.GetFirstLocalUser().cachedBody == hc.body)
                    { 
                        currentReserve = curHP; //Update currentHP value
                        Debug.Log("HC: " + hc.isLocalPlayer.ToString() + ". Body: " + hc.body.localPlayerAuthority.ToString() + ". PCMC: " + PlayerCharacterMasterController.instances[0].master.isLocalPlayer);
                    }
                });
                #endregion

                #region BuildReserves
                c.GotoNext( //match the timer and loading of 0f onto the stack, increment 4 instuctions to palce ourselves here: if(this.timer > 0f<here>)
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld("RoR2.HealthComponent/RepeatHealComponent", "timer"),
                    x => x.MatchLdcR4(0f)
                    );
                c.Index += 4;

                c.Emit(OpCodes.Ldarg_0); //load (this) onto the stack
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetNestedType("RepeatHealComponent", BindingFlags.Instance | BindingFlags.NonPublic).GetFieldCached("healthComponent")); //Load HealthComponent RepeatHealComponent.healthComponent onto the stack
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetFieldCached("health")); //load healthComponent.health onto the stack.

                c.Emit(OpCodes.Ldarg_0); //load (this) onto the stack
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetNestedType("RepeatHealComponent", BindingFlags.Instance | BindingFlags.NonPublic).GetFieldCached("healthComponent")); //Load HealthComponent RepeatHealComponent.healthComponent onto the stack
                c.Emit(OpCodes.Call, typeof(HealthComponent).GetMethod("get_fullHealth")); //load healthComponent.fullHealth onto the stack.
                c.Emit(OpCodes.Bge_Un_S, lab); //branch to return if health > fullhealth

                //Mark return address
                c.GotoNext(
                    x => x.MatchRet()
                    );
                c.MarkLabel(lab);
                #endregion
            };

            //Add regen over time to health reserve
            IL.RoR2.HealthComponent.FixedUpdate += (il) =>
            {
                var c = new ILCursor(il);
                //Logger.LogInfo(il.ToString());

                //GoTo: this.regenAccumulator -= num;
                c.GotoNext(
                    x => x.MatchLdfld<HealthComponent>("regenAccumulator"),
                    x => x.MatchLdloc(0),
                    x => x.MatchSub(),
                    x => x.MatchStfld<HealthComponent>("regenAccumulator")
                );
                c.Index += 4; //NextLine

                //c.RemoveRange(8); //We don't remove the range(hc.Heal(regenAccumulator, default(ProcChainMask), false);) because this is only applied to default health regen effects, not repeatheal.

                c.Emit(OpCodes.Ldarg_0);//push (this) to pass to the Delegate
                c.Emit(OpCodes.Ldarg_0);//Push (this) to pass to getFieldCached
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetFieldCached("regenAccumulator")); //push regenAccumulator to the stack

                //pass this & regenAccumulor to delegate
                c.EmitDelegate<Action<HealthComponent, float>>((hc, regenAccumulator) =>
                {
                    if (hc.body.inventory.GetItemCount(ItemIndex.RepeatHeal) > 0) //Check if we have a CorpseBloom
                    {
                        hc.Heal(regenAccumulator, default(ProcChainMask), true); //Add regen to reserve. duplicating this does not matter since they are different heal types cought by differe
                    }
                });
                //Debug.Log(il.ToString());
            };


            On.RoR2.SceneDirector.Start += (orig, self) =>
            {
                initializeReserveUI(0f);
                orig(self);
            };

            //On.RoR2.UI.HealthBar.Start += (self, orig) =>
            //{
            //    self(orig);
            //};

            //On.RoR2.UI.HighlightRect.DoUpdate += (self, orig) =>
            //{
            //    self(orig);
            //};

            On.RoR2.UI.HUD.Start += (self, orig) =>
            {
                self(orig);
                reserveRect.transform.SetParent(orig.healthBar.transform, false);
                hpBar = orig.healthBar;
                //Debug.Log("Added ReserveBar to Parent");
            };
            Logger.Log(LogLevel.Info, "Run started");
        }

        public void Update()
        {
            if (hpBar != null)
            {
                if (hpBar.source.body.inventory != null)
                {
                    if (hpBar.source.body.inventory.GetItemCount(ItemIndex.RepeatHeal) != 0)
                    {
                        if (reserveRect.activeSelf == false)
                        {
                            //Debug.Log("CorpsePickup");
                            reserveRect.SetActive(true);
                        }
                        else
                        {
                            //Debug.Log("PlayerHasCorpse");
                            percentReserve = -0.5f + (currentReserve / reserveMax);
                            reserveBar.GetComponent<RectTransform>().anchorMax = new Vector2(percentReserve, 0.5f);
                        }
                    }
                    else
                    {
                        if (reserveRect.activeSelf == true)
                        {
                            reserveRect.SetActive(false);
                        }
                    }
                }
            }
            //Debug.Log($"{currentReserve}/{reserveMax}:{percentReserve}");
            TestHelper.itemSpawnHelper();
        }

        public void initializeReserveUI(float offset)
        {
            reserveRect = new GameObject();
            reserveRect.name = "ReserveRect";
            reserveRect.AddComponent<RectTransform>();
            reserveRect.GetComponent<RectTransform>().position = new Vector3(0f, 0f);
            reserveRect.GetComponent<RectTransform>().anchoredPosition = new Vector2(210, 0);
            reserveRect.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
            reserveRect.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0);
            reserveRect.GetComponent<RectTransform>().offsetMin = new Vector2(210, 0);
            reserveRect.GetComponent<RectTransform>().offsetMax = new Vector2(210, 10);
            reserveRect.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 10);
            reserveRect.GetComponent<RectTransform>().pivot = new Vector2(0, 0);

            reserveBar = new GameObject();
            reserveBar.name = "ReserveBar";
            reserveBar.transform.SetParent(reserveRect.GetComponent<RectTransform>().transform);
            reserveBar.AddComponent<RectTransform>().pivot = new Vector2(0, 0);
            reserveBar.GetComponent<RectTransform>().sizeDelta = reserveRect.GetComponent<RectTransform>().sizeDelta;
            reserveBar.AddComponent<Image>().color = new Color(1, 0.33f, 0, 1);
        }
    }
}