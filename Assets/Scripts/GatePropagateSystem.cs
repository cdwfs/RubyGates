using Unity.Entities;

[UpdateAfter(typeof(GateDagSortSystem))]
public class GatePropagateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .ForEach((ref GateOutput output, in DynamicBuffer<GateInput> inputs, in GateTypeComponent gateType) =>
            {
                //output.Value += 1;
            }).ScheduleParallel();
    }
}