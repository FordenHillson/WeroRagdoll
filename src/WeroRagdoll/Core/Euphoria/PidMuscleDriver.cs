namespace WeroRagdoll.Core.Euphoria
{
    /// <summary>
    /// Default driver: existing joint-drive + per-zone strength lerp (Phase 1/2).
    /// Later NaturalMotion-like solvers can replace this implementation.
    /// </summary>
    public sealed class PidMuscleDriver : IMuscleDriver
    {
        public MuscleModel Muscle { get; } = new MuscleModel();

        public void ApplyLiving(Actor actor, float globalMul)
        {
            DriveApplier.ApplyLiving(actor, Muscle, globalMul);
        }

        public void ApplyLimp(Actor actor)
        {
            DriveApplier.ApplyLimp(actor);
        }

        public void ApplyGetUp(Actor actor)
        {
            DriveApplier.ApplyGetUp(actor, Muscle);
        }

        public void OnImpact(HitZone zone, float impactNormalized)
        {
            Muscle.ApplyImpact(zone, impactNormalized);
        }

        public void TickRecover(float dt, float recoverTargetMul)
        {
            Muscle.TickRecover(dt, recoverTargetMul);
        }
    }
}
