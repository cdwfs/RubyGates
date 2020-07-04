using Unity.Entities;
using Unity.Rendering;

public struct WireInput : IComponentData {
    public Entity InputEntity;
}

[MaterialProperty("_WireState", MaterialPropertyFormat.Float)]
public struct WireState : IComponentData
{
    public float Value;
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
        var wireJob = Entities
            .WithName("WireSystem")
            .ForEach((ref WireState wireState, in WireInput wireInput) =>
            {
                var inputGateOutput = GetComponent<NodeOutput>(wireInput.InputEntity);
                wireState.Value = inputGateOutput.Value;
            }).ScheduleParallel(Dependency);
        Dependency = wireJob;
    }
}