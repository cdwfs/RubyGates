using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public enum GateType
{
    And = 1,
    Or = 2,
    Xor = 3,
    Not = 4,
    Sink = 5,
}

// Singleton tag component whose presence indicates that the node DAG needs to be re-sorted.
public struct DagIsStale : IComponentData
{

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

// The nodes are topologically sorted and processed according to their depth.
public struct DagDepth : ISharedComponentData
{
    public int Value;
}

public struct WireInput : IComponentData
{
    public Entity InputEntity;
}

public struct WireMaterials : ISharedComponentData, IEquatable<WireMaterials> {
    public WireMaterials(in Material offMaterial, in Material onMaterial)
    {
        OffMaterial = offMaterial;
        OnMaterial = onMaterial;
    }
    public Material OffMaterial;
    public Material OnMaterial;

    public bool Equals(WireMaterials other) {
        return OffMaterial == other.OffMaterial && OnMaterial == other.OnMaterial;
    }
    public override int GetHashCode() {
        return OnMaterial.GetHashCode() ^ OffMaterial.GetHashCode();
    }
}

public struct ClickableNode : IComponentData
{
    public float2 RectMin;
    public float2 RectMax;
}

public struct SinkTag : IComponentData
{
    
}

public struct SinkMaterials : ISharedComponentData, IEquatable<SinkMaterials> {
    public SinkMaterials(in Material offMaterial, in Material onMaterial)
    {
        OffMaterial = offMaterial;
        OnMaterial = onMaterial;
    }
    public Material OffMaterial;
    public Material OnMaterial;

    public bool Equals(SinkMaterials other) {
        return OffMaterial == other.OffMaterial && OnMaterial == other.OnMaterial;
    }
    public override int GetHashCode() {
        return OnMaterial.GetHashCode() ^ OffMaterial.GetHashCode();
    }
}

