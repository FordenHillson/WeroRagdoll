using UnityEngine;

namespace WeroRagdoll.Core.Euphoria
{
    public sealed class EuphoriaContext
    {
        public Actor Actor;
        public IMuscleDriver Driver;
        public WeroState State;
        public HitZone LastZone = HitZone.Unknown;
        public Vector3 ForceDir = Vector3.forward;
        public Vector3 WoundPoint;
        public float TimeInState;
        public bool Lethal;

        public Rigidbody HipBody()
        {
            return Body(HumanBodyBones.Hips);
        }

        public Rigidbody Body(HumanBodyBones bone)
        {
            if (Actor == null || Actor.ragdoll == null)
                return null;
            try
            {
                Transform t = Actor.ragdoll.IsRagdoll()
                    ? Actor.ragdoll.HumanBoneTransformRagdoll(bone)
                    : Actor.ragdoll.HumanBoneTransformActive(bone);
                return t != null ? t.GetComponent<Rigidbody>() : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
