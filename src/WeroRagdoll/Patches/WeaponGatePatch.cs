using HarmonyLib;
using WeroRagdoll.Core;

namespace WeroRagdoll.Patches
{
    [HarmonyPatch(typeof(Actor), "CanFireWeapon")]
    internal static class CanFireWeaponPatch
    {
        private static void Postfix(Actor __instance, ref bool __result)
        {
            if (!__result || __instance == null || !__instance.aiControlled || !Plugin.Cfg.Enabled.Value)
                return;

            var brain = __instance.GetComponent<WeroActorBrain>();
            if (brain != null && brain.BlockWeapon)
                __result = false;
        }
    }

    [HarmonyPatch(typeof(Actor), "CanUnholsterWeapon")]
    internal static class CanUnholsterWeaponPatch
    {
        private static void Postfix(Actor __instance, ref bool __result)
        {
            if (!__result || __instance == null || !__instance.aiControlled || !Plugin.Cfg.Enabled.Value)
                return;

            var brain = __instance.GetComponent<WeroActorBrain>();
            if (brain != null && brain.BlockWeapon)
                __result = false;
        }
    }
}
