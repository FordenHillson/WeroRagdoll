namespace WeroRagdoll.Core
{
    public enum HitZone
    {
        Unknown = 0,
        Head,
        Torso,
        ArmL,
        ArmR,
        LegL,
        LegR
    }

    public enum ReactionTier
    {
        None = 0,
        Soft,
        Medium,
        Hard,
        Lethal
    }

    public enum WeroState
    {
        Normal = 0,
        Flinch,
        Stagger,
        ActiveStruggle,
        GetUp,
        WoundDebuff,
        LimpDeath
    }
}
