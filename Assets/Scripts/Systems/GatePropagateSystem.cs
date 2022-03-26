using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;

public enum GateType
{
    And = 1,
    Or = 2,
    Xor = 3,
    Not = 4,
    Sink = 5,
    Button = 6,
    BranchOn = 7,
    BranchOff = 8,
}

// A node's current output value (0 or 1)
[MaterialProperty("_NodeOutput", MaterialPropertyFormat.Float)]
public struct NodeOutput : IComponentData
{
    public float Value; // TODO: this should be an int, but MaterialProperies must currently be floats.
}

// A buffer of the node entities (0+) whose outputs feed into this node.
[InternalBufferCapacity(2)] // We never expect more than 2 inputs per node, right?  (Aside from Sinks)
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
public partial class GatePropagateSystem : SystemBase
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

        foreach(var depth in ValidDagDepths)
        {
            if (depth.Value == 0)
                continue; // skip depth-zero nodes; they should all be buttons.
            var ecb = _beginPresEcbSystem.CreateCommandBuffer().AsParallelWriter();
            Dependency = Entities
                .WithName("GatePropagateSystem")
                .WithNativeDisableContainerSafetyRestriction(nodeOutputs) // DAG sort ensures each pass writes to different outputs than it reads
                .WithSharedComponentFilter(depth)
                .ForEach((Entity nodeEntity, int entityInQueryIndex, ref NodeOutput output,
                    in DynamicBuffer<NodeInput> inputs, in GateInfo gateInfo) =>
                {
                    float newOutput = 0.0f;
                    switch (gateInfo.Type)
                    {
                        case GateType.And:
                            newOutput = (int)nodeOutputs[inputs[0].InputEntity].Value & (int)nodeOutputs[inputs[1].InputEntity].Value;
                            break;
                        case GateType.Or:
                            newOutput = (int)nodeOutputs[inputs[0].InputEntity].Value | (int)nodeOutputs[inputs[1].InputEntity].Value;
                            break;
                        case GateType.Xor:
                            newOutput = (int)nodeOutputs[inputs[0].InputEntity].Value ^ (int)nodeOutputs[inputs[1].InputEntity].Value;
                            break;
                        case GateType.Not:
                            newOutput = 1 - (int)nodeOutputs[inputs[0].InputEntity].Value;
                            break;
                        case GateType.Sink:
                            newOutput = 1;
                            for(int i=0; i<inputs.Length; ++i)
                            {
                                if (nodeOutputs[inputs[i].InputEntity].Value == 0)
                                {
                                    newOutput = 0;
                                    break;
                                }
                            }
                            // Add the tag that begins the "end of level" flow
                            if (newOutput == 1 && output.Value == 0)
                            {
                                ecb.AddComponent<VictoryTag>(entityInQueryIndex, nodeEntity);
                            }
                            break;
                        case GateType.Button:
                            break; // handled in HandleInputSystem, and skipped because dagDepth=0
                        case GateType.BranchOn:
                            newOutput = nodeOutputs[inputs[0].InputEntity].Value;
                            break;
                        case GateType.BranchOff:
                            newOutput = 0;
                            break;
                    }
                    output.Value = newOutput;
                }).ScheduleParallel(Dependency);
            _beginPresEcbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}