using HarmonyLib;
using WeroRagdoll.Core;

namespace WeroRagdoll.Patches
{
    /// <summary>
    /// After Actor.LateUpdate builds animatedSoldierHeightOffset from position deltas,
    /// force it flat while we are recovering from ragdoll get-up.
    /// </summary>
    [HarmonyPatch(typeof(Actor), "LateUpdate")]
    internal static class ActorLateUpdateHeightPatch
    {
        private static void Postfix(Actor __instance)
        {
            if (!Plugin.Cfg.Enabled.Value || __instance == null || !__instance.aiControlled)
                return;

            var brain = __instance.GetComponent<WeroActorBrain>();
            if (brain == null || !brain.SuppressHeightOffset)
                return;

            ActorGetUp.ForceFlatVisual(__instance);
            // Also pull transform down if still floating this frame.
            ActorGetUp.TickGrounding(__instance);
        }
    }
}
