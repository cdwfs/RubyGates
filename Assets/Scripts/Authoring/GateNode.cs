using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class GateNode : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public GateType gateType;
    public NodeAttachPoints[] inputs;
    public GameObject wirePrefab;
    public Material offMaterial;
    public Material onMaterial;

    private void OnDrawGizmos()
    {
        // Draw lines from this node's inputs' output attach points to this node's input attach point.
        Gizmos.color = Color.yellow;
        var inputAttachPoint = GetComponent<NodeAttachPoints>().inputAttachTransform.position;
        foreach (var input in inputs)
        {
            Gizmos.DrawLine(
                input.outputAttachTransform.position,
                inputAttachPoint);
        }

        // Little tip on each line
        Gizmos.color = Color.blue;
        foreach (var input in inputs)
        {
            Gizmos.DrawLine(
                Vector3.Lerp(input.outputAttachTransform.position, inputAttachPoint, 0.9f),
                inputAttachPoint);
        }
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(wirePrefab);
    }

    public void Convert(Entity gateEntity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var gateComponentTypes = new List<ComponentType>
        {
            typeof(GateInfo),
            typeof(NodeInput),
            typeof(NodeOutput),
            typeof(DagDepth),
            typeof(MaterialPalette),
        };
        dstManager.AddComponents(gateEntity, new ComponentTypes(gateComponentTypes.ToArray()));

        dstManager.SetComponentData(gateEntity, new GateInfo {Type = gateType});

        dstManager.SetComponentData(gateEntity, new MaterialPalette(new[] {
            offMaterial,
            onMaterial,
        }));
        
        {
            var inputsBuffer = dstManager.GetBuffer<NodeInput>(gateEntity);
            inputsBuffer.Capacity = inputs.Length;
            foreach (var input in inputs)
            {
                // Store a reference to the input gate entity
                inputsBuffer.Add(new NodeInput
                    {InputEntity = conversionSystem.TryGetPrimaryEntity(input.gameObject)});
            }
        }

        var wireAuthoring = wirePrefab.GetComponent<Wire>();
        var inputAttachPoint = GetComponent<NodeAttachPoints>().inputAttachTransform.position;
        foreach (var input in inputs)
        {
            // Create a linked entity for the wires leading from the input to this gate
            var wireStartPos = input.outputAttachTransform.position;
            var wireEndPos = inputAttachPoint;
            // TODO(cort): conversionSystem.InstantiateAdditionalEntity() would be ideal here. Instead, copy what we need from the prefab.
            var wireEntity = conversionSystem.CreateAdditionalEntity(gameObject);
#if UNITY_EDITOR
            dstManager.SetName(wireEntity, "Wire"); // TODO: make this call [Conditional]
#endif
            dstManager.AddComponents(wireEntity, new ComponentTypes(new ComponentType[]{
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(Translation),
                typeof(Rotation),
                typeof(NonUniformScale),
                typeof(MaterialPalette),
                typeof(WireInput)}));

            // Store the wire's palette of materials in a separate shared component
            dstManager.SetComponentData(wireEntity, new MaterialPalette(new[] {
                wireAuthoring.offMaterial,
                wireAuthoring.onMaterial,
            }));
            // Copy & modify the gate's RenderMesh to reference the correct mesh/material.
            var baseRenderMesh = dstManager.GetSharedComponentData<RenderMesh>(gateEntity);
            baseRenderMesh.mesh = wirePrefab.GetComponent<MeshFilter>().sharedMesh;
            baseRenderMesh.material = wireAuthoring.offMaterial;
            dstManager.AddSharedComponentData(wireEntity, baseRenderMesh);

            // TODO(https://github.com/cdwfs/RubyGates/issues/9): pre-bake the LocalToWorld and add the Static tag
            dstManager.SetComponentData(wireEntity, new Translation
            {
                Value = wireStartPos,
            });

            var wireScale = wirePrefab.transform.localScale;
            wireScale.y = Vector3.Distance(wireStartPos, wireEndPos);
            dstManager.SetComponentData(wireEntity, new NonUniformScale
            {
                Value = wireScale,
            });

            // We want to measure an angle relative to +Y while looking in -Z.
            float3 wireDir = wireEndPos - wireStartPos;
            float wireAngle = math.atan2(-wireDir.x, wireDir.y);
            dstManager.SetComponentData(wireEntity, new Rotation
            {
                Value = quaternion.RotateZ(wireAngle),
            });

            dstManager.SetComponentData(wireEntity, new WireInput
            {
                InputEntity = conversionSystem.TryGetPrimaryEntity(input.gameObject),
            });
        }
    }
}