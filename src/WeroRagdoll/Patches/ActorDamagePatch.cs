using HarmonyLib;
using WeroRagdoll.Core;

namespace WeroRagdoll.Patches
{
    [HarmonyPatch(typeof(Actor), "Damage", typeof(DamageInfo))]
    internal static class ActorDamagePatch
    {
        private static void Prefix(Actor __instance, out bool __state)
        {
            __state = __instance != null && __instance.dead;
        }

        private static void Postfix(Actor __instance, DamageInfo info, bool __state, bool __result)
        {
            if (!Plugin.Cfg.Enabled.Value || __instance == null || !__instance.aiControlled)
                return;

            // __result false means damage was ignored.
            if (!__result && !__instance.dead)
                return;

            var brain = WeroActorBrain.Ensure(__instance);
            if (brain == null)
                return;

            brain.HandleDamage(info, __state);
        }
    }
}
