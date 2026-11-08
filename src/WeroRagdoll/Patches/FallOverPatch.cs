using HarmonyLib;
using WeroRagdoll.Core;

namespace WeroRagdoll.Patches
{
    /// <summary>
    /// When vanilla knocks a bot over mid-reaction, keep living muscle drive instead of dead limp.
    /// </summary>
    [HarmonyPatch(typeof(Actor), "FallOver")]
    internal static class FallOverPatch
    {
        private static void Postfix(Actor __instance)
        {
            if (!Plugin.Cfg.Enabled.Value || __instance == null || !__instance.aiControlled || __instance.dead)
                return;

            var brain = __instance.GetComponent<WeroActorBrain>();
            if (brain == null)
                return;

            if (brain.State == WeroState.ActiveStruggle
                || brain.State == WeroState.Stagger
                || brain.State == WeroState.GetUp)
            {
                DriveApplier.ApplyLiving(__instance, brain.Muscle, brain.State == WeroState.GetUp ? 0.65f : 1f);
            }
        }
    }

    [HarmonyPatch(typeof(Actor), "KnockOver", typeof(UnityEngine.Vector3))]
    internal static class KnockOverPatch
    {
        private static void Postfix(Actor __instance)
        {
            if (!Plugin.Cfg.Enabled.Value || __instance == null || !__instance.aiControlled || __instance.dead)
                return;

            var brain = __instance.GetComponent<WeroActorBrain>();
            if (brain == null)
                return;

            if (brain.State == WeroState.ActiveStruggle || brain.State == WeroState.Stagger)
                DriveApplier.ApplyLiving(__instance, brain.Muscle, 1f);
            else if (brain.State == WeroState.LimpDeath)
                DriveApplier.ApplyLimp(__instance);
        }
    }
}
