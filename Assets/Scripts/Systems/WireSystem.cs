using Unity.Entities;

public struct WireInput : IComponentData {
    public Entity InputEntity;
}

[UpdateAfter(typeof(GatePropagateSystem))]
public class WireSystem : SystemBase
{
    BeginPresentationEntityCommandBufferSystem _beginPresEcbSystem;
    protected override void OnCreate() {
        _beginPresEcbSystem = World.GetExistingSystem<BeginPresentationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = _beginPresEcbSystem.CreateCommandBuffer().ToConcurrent();
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
        _beginPresEcbSystem.AddJobHandleForProducer(job);
        Dependency = job;
    }
}