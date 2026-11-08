namespace WeroRagdoll.Core.Euphoria
{
    /// <summary>
    /// Phase 3 extension point — swap/enhance living muscle control without rewriting brain.
    /// </summary>
    public interface IMuscleDriver
    {
        MuscleModel Muscle { get; }
        void ApplyLiving(Actor actor, float globalMul);
        void ApplyLimp(Actor actor);
        void ApplyGetUp(Actor actor);
        void OnImpact(HitZone zone, float impactNormalized);
        void TickRecover(float dt, float recoverTargetMul);
    }
}
