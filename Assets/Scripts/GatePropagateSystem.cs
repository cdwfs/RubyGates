using System.Collections.Generic;
using Unity.Entities;

[UpdateAfter(typeof(HandleInputSystem))]
public class GatePropagateSystem : SystemBase
{
    public List<DagDepth> ValidDagDepths;
    private BeginPresentationEntityCommandBufferSystem _beginPresEcbSystem;
    protected override void OnCreate()
    {
        ValidDagDepths = new List<DagDepth>();
        _beginPresEcbSystem = World.GetExistingSystem<BeginPresentationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var nodeOutputs = GetComponentDataFromEntity<NodeOutput>(true);

        var ecb = _beginPresEcbSystem.CreateCommandBuffer().ToConcurrent();
        foreach(var depth in ValidDagDepths)
        {
            if (depth.Value == 0)
                continue; // skip depth-zero nodes; they should all be buttons.
            var job = Entities
                .WithName("GatePropagateSystem")
                .WithNativeDisableContainerSafetyRestriction(nodeOutputs) // DAG sort ensures each pass writes to different outputs than it reads
                .WithSharedComponentFilter(depth)
                .ForEach((Entity nodeEntity, int entityInQueryIndex, ref NodeOutput output,
                    in DynamicBuffer<NodeInput> inputs, in GateInfo gateInfo) =>
                {
                    output.PrevValue = output.Value;
                    switch (gateInfo.Type)
                    {
                        case GateType.And:
                            output.Value = nodeOutputs[inputs[0].InputEntity].Value & nodeOutputs[inputs[1].InputEntity].Value;
                            break;
                        case GateType.Or:
                            output.Value = nodeOutputs[inputs[0].InputEntity].Value | nodeOutputs[inputs[1].InputEntity].Value;
                            break;
                        case GateType.Xor:
                            output.Value = nodeOutputs[inputs[0].InputEntity].Value ^ nodeOutputs[inputs[1].InputEntity].Value;
                            break;
                        case GateType.Not:
                            output.Value = 1 - nodeOutputs[inputs[0].InputEntity].Value;
                            break;
                        case GateType.Sink:
                            output.Value = 1;
                            for(int i=0; i<inputs.Length; ++i)
                            {
                                if (nodeOutputs[inputs[i].InputEntity].Value == 0)
                                {
                                    output.Value = 0;
                                    break;
                                }
                            }
                            // Add the tag that begins the "end of level" flow
                            if (output.Value == 1 && output.Changed)
                            {
                                ecb.AddComponent<VictoryTag>(entityInQueryIndex, nodeEntity);
                            }
                            break;
                    }
                    // Change material based on node state
                    if (output.Changed)
                    {
                        ecb.AddComponent(entityInQueryIndex, nodeEntity, new MaterialChange
                        {
                            entity = nodeEntity,
                            materialIndex = output.Value,
                        });
                    }
                }).ScheduleParallel(Dependency);
            _beginPresEcbSystem.AddJobHandleForProducer(job);
            Dependency = job;
        }
    }
}