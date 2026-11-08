namespace WeroRagdoll.Core.Euphoria
{
    public interface IEuphoriaBehavior
    {
        string Name { get; }
        bool Wants(EuphoriaContext ctx);
        void Enter(EuphoriaContext ctx);
        void Tick(EuphoriaContext ctx, float dt);
        void Exit(EuphoriaContext ctx);
    }
}
