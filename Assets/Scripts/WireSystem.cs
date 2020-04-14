using Unity.Entities;

[UpdateAfter(typeof(GatePropagateSystem))]
public class WireSystem : SystemBase
{
    ChangeMaterialSystem changeMaterialSys;
    protected override void OnCreate()
    {
        changeMaterialSys = World.GetExistingSystem<ChangeMaterialSystem>();
    }

    protected override void OnUpdate()
    {
        var changeQueueWriter = changeMaterialSys.ChangeQueueParallelWriter;
        var job = Entities
            .WithName("WireSystem")
            .ForEach((Entity wireEntity, in WireInput wireInput) =>
            {
                var inputGateOutput = GetComponent<NodeOutput>(wireInput.InputEntity);
                if (inputGateOutput.Changed)
                {
                    changeQueueWriter.Enqueue(new MaterialChange {
                        entity = wireEntity,
                        materialIndex = inputGateOutput.Value,
                    });
                }
            }).ScheduleParallel(Dependency);
        changeMaterialSys.AddJobHandleForProducer(job);
        Dependency = job;
    }
}