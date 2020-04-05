using Unity.Entities;

public enum GateType
{
    AND = 1,
    OR = 2,
    XOR = 3,
    NOT = 4,
    BUTTON
}

public struct DagIsStale : IComponentData
{
    
}

public struct GateOutput : IComponentData
{
    public int Value;
}

public struct GateInput : IBufferElementData
{
    public Entity InputEntity;
}

// TODO(cort): this could be a shared component if we wanted to process each gate type separately
public struct GateTypeComponent : IComponentData
{
    public GateType Value;
}

public struct GateDagDepth : ISharedComponentData
{
    public int Value;
}