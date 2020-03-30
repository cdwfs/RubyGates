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
    public int value;
}

public struct GateInput : IBufferElementData
{
    public Entity inputEntity;
}

// TODO(cort): this could be a shared component if we wanted to process each gate type separately
public struct GateTypeComponent : IComponentData
{
    public GateType value;
}

public struct GateDagDepth : ISharedComponentData
{
    public int value;
}