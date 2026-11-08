using HarmonyLib;
using WeroRagdoll.Core;

namespace WeroRagdoll.Patches
{
    [HarmonyPatch(typeof(AiActorController), "UpdateMovementSpeed")]
    internal static class AiMovementPatch
    {
        private static readonly AccessTools.FieldRef<AiActorController, float> MovementSpeedRef =
            AccessTools.FieldRefAccess<AiActorController, float>("movementSpeed");

        private static void Postfix(AiActorController __instance)
        {
            if (!Plugin.Cfg.Enabled.Value || __instance == null || __instance.actor == null)
                return;
            if (!__instance.actor.aiControlled)
                return;

            var brain = __instance.actor.GetComponent<WeroActorBrain>();
            if (brain == null || brain.MoveSpeedMul >= 0.999f)
                return;

            try
            {
                MovementSpeedRef(__instance) *= brain.MoveSpeedMul;
            }
            catch
            {
                // Field layout changed — fail quiet.
            }
        }
    }
}
