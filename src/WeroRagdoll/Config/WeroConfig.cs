using BepInEx.Configuration;

namespace WeroRagdoll.Config
{
    public sealed class WeroConfig
    {
        public ConfigEntry<bool> Enabled { get; }
        public ConfigEntry<float> SoftImpact { get; }
        public ConfigEntry<float> MediumImpact { get; }
        public ConfigEntry<float> HardImpact { get; }
        public ConfigEntry<float> HeadImpactBias { get; }
        public ConfigEntry<float> LooseStrengthRate { get; }
        public ConfigEntry<float> GainStrengthRate { get; }
        public ConfigEntry<float> StruggleSeconds { get; }
        public ConfigEntry<float> MinGroundedBeforeGetUp { get; }
        public ConfigEntry<float> GetUpFailsafeSeconds { get; }
        public ConfigEntry<float> FlinchSeconds { get; }
        public ConfigEntry<float> StaggerSeconds { get; }
        public ConfigEntry<float> GetUpSeconds { get; }
        public ConfigEntry<float> DebuffSeconds { get; }
        public ConfigEntry<float> DeathImpulseScale { get; }
        public ConfigEntry<float> StruggleDriveMul { get; }
        public ConfigEntry<float> MuscleSpring { get; }
        public ConfigEntry<float> MuscleDamping { get; }
        public ConfigEntry<float> LimpSpring { get; }
        public ConfigEntry<float> LimpDamping { get; }
        public ConfigEntry<float> ImpulseScale { get; }
        public ConfigEntry<float> FullLodDistance { get; }
        public ConfigEntry<float> CheapLodDistance { get; }
        public ConfigEntry<int> MaxConcurrentStruggle { get; }
        public ConfigEntry<float> WoundLegSpeedMul { get; }
        public ConfigEntry<float> CoreBalanceSoftFloor { get; }
        public ConfigEntry<float> CoreBalanceHardCeil { get; }
        public ConfigEntry<bool> AutoGetUp { get; }
        public ConfigEntry<bool> DebugHud { get; }
        public ConfigEntry<bool> TestRangeEnabled { get; }
        public ConfigEntry<string> TestRangeHotkey { get; }
        public ConfigEntry<string> TestUiHotkey { get; }
        public ConfigEntry<bool> TestUiStartOpen { get; }

        // Phase 2
        public ConfigEntry<bool> WoundAssistEnabled { get; }
        public ConfigEntry<float> WoundHandSpring { get; }
        public ConfigEntry<float> WoundAssistStrength { get; }
        public ConfigEntry<float> WoundIkWeight { get; }
        public ConfigEntry<bool> DeathSquirmEnabled { get; }
        public ConfigEntry<float> DeathTenseSeconds { get; }
        public ConfigEntry<float> DeathSquirmSeconds { get; }
        public ConfigEntry<float> DeathFadeSeconds { get; }
        public ConfigEntry<float> DeathSquirmPulse { get; }
        public ConfigEntry<float> DeathSquirmImpulse { get; }
        public ConfigEntry<float> MaxUpwardImpulse { get; }
        public ConfigEntry<float> MaxHorizontalImpulse { get; }

        // Phase 3 — Euphoria primitives
        public ConfigEntry<bool> EuphoriaEnabled { get; }
        public ConfigEntry<float> EuphoriaDriveMul { get; }
        public ConfigEntry<float> EuphoriaProtectHeadForce { get; }
        public ConfigEntry<float> EuphoriaBraceForce { get; }
        public ConfigEntry<float> EuphoriaStepForce { get; }
        public ConfigEntry<float> EuphoriaStepInterval { get; }
        public ConfigEntry<float> EuphoriaWritheForce { get; }
        public ConfigEntry<float> EuphoriaWritheInterval { get; }

        public WeroConfig(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true, "Master toggle.");
            DebugHud = config.Bind("General", "DebugHud", false, "Show tiny on-screen state for nearest bot reaction.");
            TestRangeEnabled = config.Bind("TestRange", "Enabled", true, "Bots stand idle and do not fire (for ragdoll testing).");
            TestRangeHotkey = config.Bind("TestRange", "Hotkey", "F8", "Toggle Test Range in-game (Unity KeyCode name).");
            TestUiHotkey = config.Bind("TestRange", "UiHotkey", "F7", "Toggle Test Panel UI (Unity KeyCode name).");
            TestUiStartOpen = config.Bind("TestRange", "UiStartOpen", true, "Open Test Panel when game starts.");

            SoftImpact = config.Bind("Thresholds", "SoftImpact", 8f, "Below this → flinch (animated).");
            MediumImpact = config.Bind("Thresholds", "MediumImpact", 18f, "At/above → stagger / possible ragdoll.");
            HardImpact = config.Bind("Thresholds", "HardImpact", 32f, "At/above → active struggle ragdoll.");
            HeadImpactBias = config.Bind("Thresholds", "HeadImpactBias", 1.45f, "Multiplies head hit impact score.");
            CoreBalanceSoftFloor = config.Bind("Thresholds", "CoreBalanceSoftFloor", 0.55f, "Core balance above this favors Soft.");
            CoreBalanceHardCeil = config.Bind("Thresholds", "CoreBalanceHardCeil", 0.28f, "Core balance below this forces Hard.");

            LooseStrengthRate = config.Bind("Muscle", "LooseStrengthRate", 2.4f, "How fast strength drops on hit.");
            GainStrengthRate = config.Bind("Muscle", "GainStrengthRate", 0.55f, "How fast strength recovers after impact.");
            MuscleSpring = config.Bind("Muscle", "MuscleSpring", 1600f, "Living ragdoll spring.");
            MuscleDamping = config.Bind("Muscle", "MuscleDamping", 30f, "Living ragdoll damping.");
            LimpSpring = config.Bind("Muscle", "LimpSpring", 60f, "Dead/limp spring.");
            LimpDamping = config.Bind("Muscle", "LimpDamping", 4f, "Dead/limp damping.");
            ImpulseScale = config.Bind("Muscle", "ImpulseScale", 0.55f, "Scales KnockOver / force impulse.");
            DeathImpulseScale = config.Bind("Muscle", "DeathImpulseScale", 1.15f, "Extra impulse mul on lethal hits.");
            StruggleDriveMul = config.Bind("Muscle", "StruggleDriveMul", 0.72f, "Living spring mul while struggling (<1 shows hits better).");
            MaxUpwardImpulse = config.Bind("Muscle", "MaxUpwardImpulse", 36f, "Clamp Y impulse to reduce launches.");
            MaxHorizontalImpulse = config.Bind("Muscle", "MaxHorizontalImpulse", 260f, "Clamp horizontal knock force.");

            FlinchSeconds = config.Bind("Timing", "FlinchSeconds", 0.28f, "Soft flinch duration.");
            StaggerSeconds = config.Bind("Timing", "StaggerSeconds", 0.7f, "Stagger duration before escalate/recover.");
            StruggleSeconds = config.Bind("Timing", "StruggleSeconds", 2.4f, "Minimum time in active struggle before get-up.");
            MinGroundedBeforeGetUp = config.Bind("Timing", "MinGroundedBeforeGetUp", 1.1f, "Extra settled time after struggle timer before get-up.");
            GetUpSeconds = config.Bind("Timing", "GetUpSeconds", 2.2f, "Animated get-up wait window.");
            GetUpFailsafeSeconds = config.Bind("Timing", "GetUpFailsafeSeconds", 3.5f, "If still down, InstantGetUp failsafe.");
            DebuffSeconds = config.Bind("Timing", "DebuffSeconds", 8f, "Wound debuff duration after get-up.");
            AutoGetUp = config.Bind("Timing", "AutoGetUp", true, "Alive bots try to get up after struggle.");

            FullLodDistance = config.Bind("LOD", "FullLodDistance", 45f, "Full active struggle within this camera distance.");
            CheapLodDistance = config.Bind("LOD", "CheapLodDistance", 85f, "Beyond this: cheap death/fall only.");
            MaxConcurrentStruggle = config.Bind("LOD", "MaxConcurrentStruggle", 10, "Cap simultaneous ActiveStruggle bots.");

            WoundLegSpeedMul = config.Bind("Wound", "WoundLegSpeedMul", 0.45f, "Move speed mul while leg debuffed.");

            WoundAssistEnabled = config.Bind("Phase2", "WoundAssistEnabled", true, "Hand clutch wound + brace.");
            WoundHandSpring = config.Bind("Phase2", "WoundHandSpring", 28f, "Physics spring for ragdoll hands.");
            WoundAssistStrength = config.Bind("Phase2", "WoundAssistStrength", 1.1f, "Global mul for wound/brace springs.");
            WoundIkWeight = config.Bind("Phase2", "WoundIkWeight", 0.85f, "Animator IK weight while upright.");

            DeathSquirmEnabled = config.Bind("Phase2", "DeathSquirmEnabled", true, "Tense → squirm → limp on death.");
            DeathTenseSeconds = config.Bind("Phase2", "DeathTenseSeconds", 0.75f, "Ballistic loose body after lethal hit (impulse reads here).");
            DeathSquirmSeconds = config.Bind("Phase2", "DeathSquirmSeconds", 1.6f, "Light writhe duration.");
            DeathFadeSeconds = config.Bind("Phase2", "DeathFadeSeconds", 1.0f, "Fade into limp.");
            DeathSquirmPulse = config.Bind("Phase2", "DeathSquirmPulse", 0.22f, "Seconds between squirm impulses.");
            DeathSquirmImpulse = config.Bind("Phase2", "DeathSquirmImpulse", 16f, "Impulse magnitude per squirm pulse.");

            EuphoriaEnabled = config.Bind("Phase3", "EuphoriaEnabled", true, "Enable NaturalMotion-like behavior primitives.");
            EuphoriaDriveMul = config.Bind("Phase3", "DriveMul", 0.32f, "Joint spring mul while Euphoria behaviors run (lower = more visible thrash).");
            EuphoriaProtectHeadForce = config.Bind("Phase3", "ProtectHeadForce", 14f, "Hand-to-head protection spring (clamped; high values stretch limbs).");
            EuphoriaBraceForce = config.Bind("Phase3", "BraceForce", 16f, "Hand-to-ground brace spring (clamped; high values stretch limbs).");
            EuphoriaStepForce = config.Bind("Phase3", "StepForce", 14f, "Compensatory step impulse.");
            EuphoriaStepInterval = config.Bind("Phase3", "StepInterval", 0.22f, "Seconds between compensatory steps.");
            EuphoriaWritheForce = config.Bind("Phase3", "WritheForce", 6f, "Zone writhe kick strength (Δv capped; high values stretch limbs).");
            EuphoriaWritheInterval = config.Bind("Phase3", "WritheInterval", 0.18f, "Seconds between writhe pulses.");
        }
    }
}
