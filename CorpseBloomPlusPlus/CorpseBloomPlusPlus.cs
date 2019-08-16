using BepInEx;
using RoR2;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using System.Reflection;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using RoR2.UI;
using MiniRpcLib;
using MiniRpcLib.Action;
using MiniRpcLib.Func;
using System.Collections.Generic;

namespace Paddywan
{
    /// <summary>
    /// Rebalance corpseBloom to provide greater benefits & proportional disadvantages.
    /// </summary>
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency(MiniRpcPlugin.Dependency)]
    [BepInPlugin(modGuid, modName, modVer)]
    public class CorpseBloomPlusPlus : BaseUnityPlugin
    {
        private const string modVer = "1.0.8";
        private const string modName = "CorpseBloomPlusPlus";
        private const string modGuid = "com.Paddywan.CorpseBloomRework";
        private float reserveMax = 110f;
        private float currentReserve = 110f;
        private float percentReserve = 1f;
        private GameObject reserveRect = new GameObject();
        private GameObject reserveBar = new GameObject();
        private HealthBar hpBar;
        private Dictionary<NetworkInstanceId, CorpseReserve> playerReserves = new Dictionary<NetworkInstanceId, CorpseReserve>();
        private Dictionary<NetworkInstanceId, bool> clientHasCorpseMod = new Dictionary<NetworkInstanceId, bool>();
        private Dictionary<NetworkInstanceId, bool> clientIsPinged = new Dictionary<NetworkInstanceId, bool>();
        public IRpcAction<CorpseReserve> updateReserveCommand { get; set; }
        public IRpcFunc<bool, bool> clientPingCheck { get; set; }

        public CorpseBloomPlusPlus()
        {
            var miniRpc = MiniRpc.CreateInstance(modGuid);
            updateReserveCommand = miniRpc.RegisterAction(Target.Client, (NetworkUser user, CorpseReserve cr) =>
            {
                if (cr != null)
                {
                    currentReserve = cr.currentReserve;
                    reserveMax = cr.maxReserve;
                    //Debug.Log($"CR: {currentReserve}; MR: {reserveMax};");
                }
            });

            //This is to fix severe desync when a modded server communicates with a vanilla client. Ping the client and check for response before we send updates.
            clientPingCheck = miniRpc.RegisterFunc<bool, bool>(Target.Client, (user, x) =>
            {
                Debug.Log($"[Client] HOST sent us: {x}, returning true");
                return true;
                //return $"Hello from the server, received {x}!";
            });
        }

        public void Awake()
        {
            //Scale the stacks of corpseblooms to provide % HP / s increase per stack, and -%Reserve per stack
            IL.RoR2.HealthComponent.Heal += (il) =>
            {
                //Increase the amount of reserve consumed to heal per second
                #region Benefit
                var c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchLdfld<HealthComponent>("repeatHealComponent"),
                    x => x.MatchLdcR4(0.1f)
                    ); //match: this.repeatHealComponent.healthFractionToRestorePerSecond = 0.1f / (float)this.repeatHealCount; line 197
                //Becomes this.repeatHealComponent.healthFractionToRestorePerSecond = 0.1f * (float)this.repeatHealCount;
                c.Index += 5;
                c.Remove();
                c.Emit(OpCodes.Mul);
                #endregion

                #region Disadvantage

                //remove multiplicative scaling (amount*increaseHealingCount*repeatHealingCount)
                #region healingMultiplier 
                c.GotoNext(
                x => x.MatchLdcI4(1),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<HealthComponent>("repeatHealCount"),
                x => x.MatchAdd()
                ); //match this.repeatHealComponent.AddReserve(amount * (float)(1 + this.repeatHealCount), this.fullHealth); line 198
                //Replace with this.repeatHealComponent.AddReserve(amount * (float)(1), this.fullHealth);
                c.Index += 1;
                c.RemoveRange(3);
                #endregion

                //decrease the total health reserve that is restored
                #region modifyMaxReserve
                c.GotoNext(
                x => x.MatchMul(),
                x => x.MatchLdarg(0),
                x => x.MatchCallvirt<HealthComponent>("get_fullHealth")
                ); //Match this.repeatHealComponent.AddReserve(amount * (float)(1), this.fullHealth); line 198
                //Replace with this.repeatHealComponent.AddReserve(amount * (float)(1), (this.fullHealth * 1.0f + (float)this.increaseHealingCount) / this.repeatHealCount);
                c.Index += 3;

                c.Emit(OpCodes.Ldc_R4, 1f); 
                c.Emit(OpCodes.Ldarg_0); 
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetField("increaseHealingCount", BindingFlags.Instance | BindingFlags.NonPublic)); 
                c.Emit(OpCodes.Conv_R4);
                c.Emit(OpCodes.Add);
                c.Emit(OpCodes.Mul);

                c.Emit(OpCodes.Ldarg_0); 
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetField("repeatHealCount", BindingFlags.Instance | BindingFlags.NonPublic)); 
                c.Emit(OpCodes.Conv_R4); 
                c.Emit(OpCodes.Div); 
                c.Emit(OpCodes.Ldarg_0);

                //Update each CorpseBloom owner's FullHealthReserve value and store in corpseReserve
                c.EmitDelegate<Func<float, HealthComponent, float>>((fhp, hc) =>
                {
                    if (hc.body != null)
                    {
                        if (LocalUserManager.GetFirstLocalUser().cachedBody != null)
                        {
                            if (hc.body.Equals(LocalUserManager.GetFirstLocalUser().cachedBody))
                            {
                                reserveMax = fhp;
                            }
                        }
                        if (playerReserves.ContainsKey(hc.body.netId))
                        {
                            playerReserves[hc.body.netId].maxReserve = fhp;
                        }
                        else
                        {
                            playerReserves.Add(hc.body.netId, new CorpseReserve());
                            playerReserves[hc.body.netId].maxReserve = fhp;
                        }
                    }
                    return fhp;
                });
                #endregion

                //cut multiplicative healing, only gets applied to reserves, and not to health restored
                #region multiplicativeHealing

                c.GotoNext(
                    x => x.MatchRet(),
                    x => x.MatchLdarg(1)
                    );
                c.Index += 2;
                //Debug.Log(c.ToString());
                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldarg_3);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetField("increaseHealingCount", BindingFlags.Instance | BindingFlags.NonPublic)); 
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetFieldCached("repeatHealComponent")); //I'm not sure why, but we can properly existance check the private subtype RepeatHealComponent when its cast as a HealthComponent

                c.EmitDelegate<Func<float, bool, int, HealthComponent, float>> ((amnt, rgn, incHealingCount, repHealComponent) =>
                {
                    if (rgn && repHealComponent) //If nonRegen flag is set, and the client has a repeatHealComponent, and the procChain has type.RepeatHeal (ommited due to code prior to delegate returning if has procType.)
                    {
                        amnt /= 1f + (float)incHealingCount;
                        return amnt; //return the modified amount.
                    }
                    return amnt; //otherwise we do nothing here.
                });
                c.Emit(OpCodes.Starg_S, (byte)1);
                #endregion
                #endregion
            };

            //Build reserve while fullHP, do not consume. Update currentReserve
            IL.RoR2.HealthComponent.RepeatHealComponent.FixedUpdate += (il) =>
            {
                var c = new ILCursor(il);
                ILLabel lab = il.DefineLabel();

                #region updateCurrentReserveHP
                c.GotoNext( 
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld("RoR2.HealthComponent/RepeatHealComponent", "timer"),
                    x => x.MatchLdcR4(0f)
                    ); //match if (this.timer <= 0f) line 1226
                //push reserve & HealthComponent onto stack & emitDelegate
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetNestedType("RepeatHealComponent", BindingFlags.Instance | BindingFlags.NonPublic).GetFieldCached("reserve")); 
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetNestedType("RepeatHealComponent", BindingFlags.Instance | BindingFlags.NonPublic).GetFieldCached("healthComponent"));
                
                //Update each CorpseBloom owner's CurrentReserve value
                c.EmitDelegate<Action<float, HealthComponent>> ((curHP, hc) =>
                {
                    if (hc.body != null)
                    {
                        if (LocalUserManager.GetFirstLocalUser().cachedBody != null)
                        {
                            if (LocalUserManager.GetFirstLocalUser().cachedBody.Equals(hc.body))
                            {
                                currentReserve = curHP; 
                            }
                        }
                        if (playerReserves.ContainsKey(hc.body.netId))
                        {
                            playerReserves[hc.body.netId].currentReserve = curHP;
                        }
                        else
                        {
                            playerReserves.Add(hc.body.netId, new CorpseReserve(curHP));
                        }
                    }
                });
                #endregion

                #region DontConsume
                c.GotoNext( //match the timer and loading of 0f onto the stack, increment 4 instuctions to palce ourselves here: if(this.timer > 0f<here>)
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld("RoR2.HealthComponent/RepeatHealComponent", "reserve"),
                    x => x.MatchLdcR4(0f)
                    ); //Match if (this.timer <= 0f) line 1228
                //add condition if(healthComponent.health < healthComponent.get_FullHealth) {

                //Old non-malechite logic
                //c.Index += 4;
                //c.Emit(OpCodes.Ldarg_0); 
                //c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetNestedType("RepeatHealComponent", BindingFlags.Instance | BindingFlags.NonPublic).GetFieldCached("healthComponent")); 
                //c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetFieldCached("health")); 

                //c.Emit(OpCodes.Ldarg_0); //load (this) onto the stack
                //c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetNestedType("RepeatHealComponent", BindingFlags.Instance | BindingFlags.NonPublic).GetFieldCached("healthComponent"));
                //c.Emit(OpCodes.Call, typeof(HealthComponent).GetMethod("get_fullHealth")); 
                //c.Emit(OpCodes.Bge_Un_S, lab); //branch to return if health > fullhealth

                //Remove unwanted instructions and setup the stack for our delegate.
                c.Index += 2;
                c.RemoveRange(2);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetNestedType("RepeatHealComponent", BindingFlags.Instance | BindingFlags.NonPublic).GetFieldCached("healthComponent"));

                c.Emit(OpCodes.Ldarg_0); 
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetNestedType("RepeatHealComponent", BindingFlags.Instance | BindingFlags.NonPublic).GetFieldCached("healthComponent")); 
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetFieldCached("health"));

                c.Emit(OpCodes.Ldarg_0); //load (this) onto the stack
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetNestedTypeCached("RepeatHealComponent").GetFieldCached("healthComponent"));
                c.Emit(OpCodes.Call, typeof(HealthComponent).GetMethod("get_fullHealth"));

                c.EmitDelegate<Func<float,HealthComponent,float,float,bool>>((reserve,hc,health,fullhp) =>
                {
                    if(reserve > 0f && health < fullhp && !hc.body.HasBuff(BuffIndex.HealingDisabled)) return true;
                    return false;
                });

                //Mark return address
                c.Emit(OpCodes.Brfalse, lab);
                c.GotoNext(
                    x => x.MatchRet()
                    );
                c.MarkLabel(lab);
                #endregion
            };

            //Add regen over time to health reserve
            IL.RoR2.HealthComponent.ServerFixedUpdate += (il) =>
            {
                var c = new ILCursor(il);
                //Logger.LogInfo(il.ToString());

                //GoTo: this.regenAccumulator -= num;
                c.GotoNext(
                    x => x.MatchLdfld<HealthComponent>("regenAccumulator"),
                    x => x.MatchLdloc(0),
                    x => x.MatchSub(),
                    x => x.MatchStfld<HealthComponent>("regenAccumulator")
                ); //Match this.regenAccumulator -= num; line 802

                //emitDelgate
                c.Index += 4; //NextLine
                c.RemoveRange(8);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_0);
                c.EmitDelegate<Action<HealthComponent, float>>((hc, num) =>
                {
                    ProcChainMask procChainMask = default(ProcChainMask);
                    if (hc.body.inventory && hc.body.inventory.GetItemCount(ItemIndex.RepeatHeal) > 0) //Check if we have a CorpseBloom
                    {
                        hc.Heal(num, procChainMask, true); //Add regen to reserve. 
                    }
                    else
                    {
                        hc.Heal(num, procChainMask, false); //Add regen to health
                    }
                });
            };

            //Add reserveUI to HealthBar
            On.RoR2.UI.HUD.Start += (self, orig) =>
            {
                self(orig);
                initializeReserveUI();
                reserveRect.transform.SetParent(orig.healthBar.transform, false);
                hpBar = orig.healthBar;
            };
        }

        //Update reserveUI & distribute CorpseReserve's to network players
        public void Update()
        {
            //TestHelper.spawnItem(KeyCode.F7, ItemIndex.RepeatHeal);
            //TestHelper.spawnItem(KeyCode.F8, ItemIndex.HealWhileSafe);
            //Send CorpseReserves to all CorpseBloom owners
            #region updateNetClientReserves
            if (NetworkServer.active)
            {
                foreach (NetworkUser nu in NetworkUser.readOnlyInstancesList)
                {
                    if (nu.GetCurrentBody() != null && nu.GetCurrentBody().healthComponent.alive)
                    {
                        if (playerReserves.ContainsKey(nu.GetCurrentBody().netId)) //client has CorpseBloom?
                        {
                            if (clientHasCorpseMod.ContainsKey(nu.GetCurrentBody().netId) && clientHasCorpseMod[nu.GetCurrentBody().netId]) 
                            {
                                updateReserveCommand.Invoke(playerReserves[nu.GetCurrentBody().netId], nu); //update client
                            }
                            else //client has not been pinged
                            {
                                if (!clientIsPinged.ContainsKey(nu.GetCurrentBody().netId))
                                {
                                    Debug.Log("[HOST]Sending PingCheck");
                                    clientIsPinged.Add(nu.GetCurrentBody().netId, true); //ping client
                                    clientPingCheck.Invoke(false, result =>
                                    {
                                        if(result)
                                        {
                                            clientHasCorpseMod.Add(nu.GetCurrentBody().netId, true); //client is modded
                                            Debug.Log($"[HOST] Received response from {nu.GetCurrentBody().netId}: {result}, player has mod.");
                                        }
                                        else
                                        {
                                            Debug.Log($"[HOST] Received response from {nu.GetCurrentBody().netId}: {result}, player is not modded.");
                                            clientHasCorpseMod.Add(nu.GetCurrentBody().netId, false); //client is vanilla
                                        }
                                    }, nu);
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            //Create reserveUI when player owns CorpseBloom, update sizes.
            #region updateReserveUI
            try
            {
                if (hpBar != null)
                {
                    if (/*hpBar.source && hpBar.source.body != null && */hpBar.source.body.inventory != null)
                    {
                        //Debug.Log("has invent");
                        if (hpBar.source.body.inventory.GetItemCount(ItemIndex.RepeatHeal) != 0)
                        {
                            //Debug.Log("has corpse");
                            percentReserve = -0.5f + (currentReserve / reserveMax);
                            reserveBar.GetComponent<RectTransform>().anchorMax = new Vector2(percentReserve, 0.5f);
                            if (reserveRect.activeSelf == false)
                            {
                                reserveBar.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.5f);
                                Debug.Log("was false, now true");
                                reserveRect.SetActive(true);
                            }
                        }
                        else
                        {
                            if (reserveRect.activeSelf == true)
                            {
                                //Debug.Log("was true, now false");
                                reserveRect.SetActive(false);
                            }
                        }
                    }
                }
            }
            catch (NullReferenceException ex) { }
            #endregion
        }

        //Create UI components
        void initializeReserveUI()
        {
            #region reserveBarContainer
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
            #endregion

            #region reserveBar
            reserveBar = new GameObject();
            reserveBar.name = "ReserveBar";
            reserveBar.transform.SetParent(reserveRect.GetComponent<RectTransform>().transform);
            reserveBar.AddComponent<RectTransform>().pivot = new Vector2(0.5f, 1.0f);
            reserveBar.GetComponent<RectTransform>().sizeDelta = reserveRect.GetComponent<RectTransform>().sizeDelta;
            reserveBar.AddComponent<Image>().color = new Color(1, 0.33f, 0, 1);
            #endregion
        }
    }
}