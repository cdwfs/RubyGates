using Unity.Entities;

[UpdateAfter(typeof(GateDagSortSystem))]
public class GatePropagateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .WithName("GatePropagateSystem")
            .ForEach((ref NodeOutput output, in DynamicBuffer<NodeInput> inputs, in GateInfo gateInfo) =>
            {
                //output.Value += 1;
            }).ScheduleParallel();
    }
}