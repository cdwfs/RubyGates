using Unity.Entities;

[UpdateAfter(typeof(GatePropagateSystem))]
public class WireSystem : SystemBase
{
    BeginPresentationEntityCommandBufferSystem beginPresEcbSystem;
    protected override void OnCreate() {
        beginPresEcbSystem = World.GetExistingSystem<BeginPresentationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = beginPresEcbSystem.CreateCommandBuffer().ToConcurrent();
        var job = Entities
            .WithName("WireSystem")
            .ForEach((Entity wireEntity, int entityInQueryIndex, in WireInput wireInput) =>
            {
                var inputGateOutput = GetComponent<NodeOutput>(wireInput.InputEntity);
                if (inputGateOutput.Changed)
                {
                    ecb.AddComponent(entityInQueryIndex, wireEntity, new MaterialChange {
                        entity = wireEntity,
                        materialIndex = inputGateOutput.Value,
                    });
                }
            }).ScheduleParallel(Dependency);
        beginPresEcbSystem.AddJobHandleForProducer(job);
        Dependency = job;
    }
}