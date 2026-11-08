using HarmonyLib;
using WeroRagdoll.Core;

namespace WeroRagdoll.Patches
{
    [HarmonyPatch(typeof(AiActorController), "Update")]
    internal static class AiUpdateTestRangePatch
    {
        private static bool Prefix(AiActorController __instance)
        {
            if (!TestRange.Active || __instance == null || __instance.actor == null)
                return true;
            if (!__instance.actor.aiControlled || __instance.actor.dead)
                return true;

            // Allow AI while bot is mid active reaction so get-up/state can settle via actor scripts.
            var brain = __instance.actor.GetComponent<WeroActorBrain>();
            if (brain != null && (brain.State == WeroState.ActiveStruggle || brain.State == WeroState.GetUp))
                return true;

            return false; // skip AI update → stand still
        }
    }

    [HarmonyPatch(typeof(AiActorController), "FixedUpdate")]
    internal static class AiFixedUpdateTestRangePatch
    {
        private static bool Prefix(AiActorController __instance)
        {
            if (!TestRange.Active || __instance == null || __instance.actor == null)
                return true;
            if (!__instance.actor.aiControlled || __instance.actor.dead)
                return true;

            var brain = __instance.actor.GetComponent<WeroActorBrain>();
            if (brain != null && (brain.State == WeroState.ActiveStruggle || brain.State == WeroState.GetUp))
                return true;

            return false;
        }
    }

    [HarmonyPatch(typeof(AiActorController), "WantsToFire")]
    internal static class AiWantsToFireTestRangePatch
    {
        private static void Postfix(AiActorController __instance, ref bool __result)
        {
            if (!TestRange.Active || __instance == null)
                return;
            __result = false;
        }
    }

    [HarmonyPatch(typeof(Actor), "CanFireWeapon")]
    internal static class ActorCanFireTestRangePatch
    {
        private static void Postfix(Actor __instance, ref bool __result)
        {
            if (!__result || !TestRange.Active || __instance == null || !__instance.aiControlled)
                return;
            __result = false;
        }
    }
}
