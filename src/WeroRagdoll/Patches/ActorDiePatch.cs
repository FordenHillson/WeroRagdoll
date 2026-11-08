using HarmonyLib;
using WeroRagdoll.Core;

namespace WeroRagdoll.Patches
{
    [HarmonyPatch(typeof(Actor), "Die", typeof(DamageInfo), typeof(bool))]
    internal static class ActorDiePatch
    {
        private static bool Prefix(Actor __instance)
        {
            // Immortal test mode: cancel AI death (guns / accidental overkill).
            // Lethal Test Panel hits set a short bypass window.
            return !TestHarness.ShouldBlockDeath(__instance);
        }

        private static void Postfix(Actor __instance, DamageInfo info)
        {
            if (!Plugin.Cfg.Enabled.Value || __instance == null || !__instance.aiControlled)
                return;

            var brain = WeroActorBrain.Ensure(__instance);
            brain?.NotifyDied(info);
        }
    }
}
