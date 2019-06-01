using BepInEx;
using RoR2;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;

namespace Paddywan
{
    /// <summary>
    /// Rebalance corpseBloom to provide greater benefits & proportional disadvantages.
    /// </summary>
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Paddywan.CorpseBloomRework", "CorpseBloomRework", "1.0.1")]
    public class CorpseBloomRework : BaseUnityPlugin
    {
        public void Awake()
        {
            //Scale the stacks of corpseblooms to provide %HP/s increase per stack
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
                Logger.LogInfo(il.ToString());
                var c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchMul(),
                    x => x.MatchLdarg(0),
                    x => x.MatchCallvirt<HealthComponent>("get_fullHealth")
                    );
                c.Index += 3;
                c.Emit(OpCodes.Ldarg_0); //this.
                //c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetField("increaseHealingCount", BindingFlags.Instance | BindingFlags.NonPublic)); //this.increaseHealingCount pushed to stack.
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetFieldCached("increaseHealingCount"));
                c.Emit(OpCodes.Conv_R4); //Convert top() to float.
                c.Emit(OpCodes.Mul); //get_fullHealth * this.increaseHealingCount
                
                c.Emit(OpCodes.Ldarg_0); //this.
                //c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetField("repeatHealCount", BindingFlags.Instance | BindingFlags.NonPublic)); //this.repeatHealCount pushed to stack.
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetFieldCached("repeatHealCount")); //this.repeatHealCount pushed to stack.
                c.Emit(OpCodes.Conv_R4); //Convert top() to float.
                c.Emit(OpCodes.Ldc_R4, 1f); //push 1.0f to stack.
                c.Emit(OpCodes.Add); //add(1.0 + this.repeatHealCount)

                c.Emit(OpCodes.Div); // (fullHealth * increaseHealingCount) / repeatHealCount
                Logger.LogInfo(il.ToString());
            };
        }
    }
}