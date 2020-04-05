using Unity.Entities;
using Unity.Mathematics;

public enum GateType
{
    And = 1,
    Or = 2,
    Xor = 3,
    Not = 4,
    Button
}

// Singleton tag component whose presence indicates that the node DAG needs to be re-sorted.
public struct DagIsStale : IComponentData
{

}

// A gate's current output value (0 or 1)
public struct GateOutput : IComponentData
{
    public int Value;
}

// A buffer of the gate entities (0+) whose outputs feed into this gate.
public struct GateInput : IBufferElementData
{
    public Entity InputEntity;
}

// TODO(cort): this could be a shared component if we wanted to process each gate type separately
public struct GateTypeComponent : IComponentData
{
    public GateType Value;
}

// The gates are topologically sorted and processed according to their depth.
public struct GateDagDepth : ISharedComponentData
{
    public int Value;
}

public struct ClickableGate : IComponentData
{
    public float2 RectMin;
    public float2 RectMax;
}