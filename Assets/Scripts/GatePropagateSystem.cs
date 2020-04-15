using System.Collections.Generic;
using Unity.Entities;

[UpdateAfter(typeof(HandleInputSystem))]
public class GatePropagateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var nodeOutputs = GetComponentDataFromEntity<NodeOutput>(true);

        var validNodeDepths = new List<DagDepth>();
        EntityManager.GetAllUniqueSharedComponentData(validNodeDepths);
        
        foreach(var depth in validNodeDepths)
        {
            if (depth.Value == 0)
                continue; // skip depth-zero nodes; they should all be buttons.
            Dependency = Entities
                .WithName("GatePropagateSystem")
                .WithNativeDisableContainerSafetyRestriction(nodeOutputs) // DAG sort ensures each pass writes to different outputs than it reads
                .WithSharedComponentFilter(depth)
                .ForEach((ref NodeOutput output, in DynamicBuffer<NodeInput> inputs, in GateInfo gateInfo) =>
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
                            break;
                    }
                }).ScheduleParallel(Dependency);
        }
    }
}