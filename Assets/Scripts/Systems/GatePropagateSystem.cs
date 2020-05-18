﻿using System.Collections.Generic;
using Unity.Entities;

public enum GateType
{
    And = 1,
    Or = 2,
    Xor = 3,
    Not = 4,
    Sink = 5,
}

// A node's current output value (0 or 1)
public struct NodeOutput : IComponentData
{
    public int Value;
    public int PrevValue;
    public bool Changed => Value != PrevValue;
}

// A buffer of the node entities (0+) whose outputs feed into this node.
[InternalBufferCapacity(2)] // We never expect more than 2 inputs per node, right?
public struct NodeInput : IBufferElementData
{
    public Entity InputEntity;
}

// TODO(cort): this could be a shared component if we wanted to process each gate type separately
public struct GateInfo : IComponentData
{
    public GateType Type;
}

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