using BepInEx;
using RoR2;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using UnityEngine;
using System.Reflection;
using System;


namespace Paddywan
{
    /// <summary>
    /// Rebalance corpseBloom to provide greater benefits & proportional disadvantages.
    /// </summary>
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Paddywan.CorpseBloomRework", "CorpseBloomRework", "1.0.0")]
    public class CorpseBloomRework : BaseUnityPlugin
    {
        public void Awake()
        {


            //Scale the stacks of corpseblooms to provide % HP / s increase per stack
            IL.RoR2.HealthComponent.Heal += (il) =>
            {
                var c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchLdfld<HealthComponent>("repeatHealComponent"),
                    x => x.MatchLdcR4(0.1f)
                    );
                c.Index += 5;
                c.Remove();
                c.Emit(OpCodes.Mul);
            };

            //Scale the stacks of corpseblooms to decrease the total reservePool per stack, but allow rejuvination racks to scale in a positive manner.
            IL.RoR2.HealthComponent.Heal += (il) =>
            {
                //Logger.LogInfo(il.ToString());
                var c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchMul(),
                    x => x.MatchLdarg(0),
                    x => x.MatchCallvirt<HealthComponent>("get_fullHealth")
                    );
                c.Index += 3;
                c.Emit(OpCodes.Ldarg_0); //this.
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetField("increaseHealingCount", BindingFlags.Instance | BindingFlags.NonPublic)); //this.increaseHealingCount pushed to stack.
                //c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetFieldCached("increaseHealingCount"));
                c.Emit(OpCodes.Conv_R4); //Convert top() to float.
                c.Emit(OpCodes.Mul); //get_fullHealth * this.increaseHealingCount

                c.Emit(OpCodes.Ldarg_0); //this.
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetField("repeatHealCount", BindingFlags.Instance | BindingFlags.NonPublic)); //this.repeatHealCount pushed to stack.
                //c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetFieldCached("repeatHealCount")); //this.repeatHealCount pushed to stack.
                //c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetProperty("get_fullHealth"));
                c.Emit(OpCodes.Conv_R4); //Convert top() to float.
                c.Emit(OpCodes.Ldc_R4, 1f); //push 1.0f to stack.
                c.Emit(OpCodes.Add); //add(1.0 + this.repeatHealCount)

                c.Emit(OpCodes.Div); // (fullHealth * increaseHealingCount) / repeatHealCount
                //Logger.LogInfo(il.ToString());
            };

            IL.RoR2.HealthComponent.RepeatHealComponent.FixedUpdate += (il) =>
            {
                var c = new ILCursor(il);
                //Logger.LogInfo(il.ToString());
                ILLabel lab = il.DefineLabel();
                c.GotoNext( //match the timer and loading of 0f onto the stack, increment 3 instuctions to palce ourselves here: if(this.timer > 0f<here>)
                    x => x.MatchLdfld("RoR2.HealthComponent/RepeatHealComponent", "timer"),
                    x => x.MatchLdcR4(0f)
                    );
                c.Index += 3;

                c.Emit(OpCodes.Ldarg_0); //load (this) onto the stack
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetNestedType("RepeatHealComponent", BindingFlags.Instance | BindingFlags.NonPublic).GetFieldCached("healthComponent")); //Load HealthComponent RepeatHealComponent.healthComponent onto the stack
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetFieldCached("health")); //load healthComponent.health onto the stack.

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetNestedType("RepeatHealComponent", BindingFlags.Instance | BindingFlags.NonPublic).GetFieldCached("healthComponent")); //Load HealthComponent RepeatHealComponent.healthComponent onto the stack
                c.Emit(OpCodes.Call, typeof(HealthComponent).GetMethod("get_fullHealth")); //load healthComponent.fullHealth onto the stack.

                c.Emit(OpCodes.Bge_Un_S, lab); //branch to return if health > fullhealth
                c.GotoNext(
                    x => x.MatchRet()
                    );
                c.MarkLabel(lab);
                //Logger.LogInfo(il.ToString());
            };


            //Add regen over time to health reserve
            IL.RoR2.HealthComponent.FixedUpdate += (il) =>
            {
                var c = new ILCursor(il);
                Logger.LogInfo(il.ToString());
                //GoTo: this.regenAccumulator -= num;
                c.GotoNext(
                    x => x.MatchLdfld<HealthComponent>("regenAccumulator"),
                    x => x.MatchLdloc(0),
                    x => x.MatchSub(),
                    x => x.MatchStfld<HealthComponent>("regenAccumulator")
                );
                c.Index += 4; //NextLine

                //c.RemoveRange(8);

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetFieldCached("regenAccumulator"));

                c.EmitDelegate<Action<HealthComponent, float>>((hc, regenAccumulator) =>
                {
                    hc.Heal(regenAccumulator, default(ProcChainMask), true);
                    //hc.Heal(regenAccumulator, default(ProcChainMask), false);
                    //if (hc.body.inventory)
                    //{
                    //    Logger.LogInfo(hc.body.inventory.GetItemCount(ItemIndex.RepeatHeal));
                    //    if (hc.body.inventory.GetItemCount(ItemIndex.RepeatHeal) == 0)
                    //    {
                    //        hc.Heal(regenAccumulator, default(ProcChainMask), true);
                    //    }
                    //    else
                    //    {
                    //        //hc.Heal(regenAccumulator, default(ProcChainMask), false);
                    //    }
                    //}
                });
                Debug.Log(il.ToString());
            };
        }
        public void Update()
        {
            TestHelper.itemSpawnHelper();
        }
    }
}