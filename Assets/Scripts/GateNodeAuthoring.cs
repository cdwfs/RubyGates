using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class GateNodeAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public GateType gateType;
    public GateNodeAuthoring[] inputs;
    public Transform inputAttachTransform;
    public Transform outputAttachTransform;
    public GameObject wirePrefab;

    private void OnDrawGizmos()
    {
        // Draw lines from this node's output attach points to this node's input attach point.
        Gizmos.color = Color.yellow;
        foreach (var input in inputs)
        {
            Gizmos.DrawLine(input.outputAttachTransform.position, inputAttachTransform.position);
        }

        // Little tip on each line
        Gizmos.color = Color.blue;
        foreach (var input in inputs)
        {
            Gizmos.DrawLine(
                Vector3.Lerp(input.outputAttachTransform.position, inputAttachTransform.position, 0.9f),
                inputAttachTransform.position);
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
            typeof(GateOutput),
            typeof(GateTypeComponent),
            typeof(GateInput),
            typeof(GateDagDepth),
        };
        // Gates with box colliders are clickable
        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            gateComponentTypes.Add(typeof(ClickableGate));
        }

        dstManager.AddComponents(gateEntity, new ComponentTypes(gateComponentTypes.ToArray()));

        if (box != null)
        {
            var bounds = box.bounds;
            var boundsMin = bounds.min;
            var boundsMax = bounds.max;
            dstManager.SetComponentData(gateEntity, new ClickableGate
            {
                RectMin = new float2(boundsMin.x, boundsMin.y),
                RectMax = new float2(boundsMax.x, boundsMax.y),
            });
        }

        dstManager.SetComponentData(gateEntity, new GateTypeComponent {Value = gateType});

        {
            var inputsBuffer = dstManager.GetBuffer<GateInput>(gateEntity);
            inputsBuffer.Capacity = inputs.Length;
            foreach (var input in inputs)
            {
                // Store a reference to the input gate entity
                inputsBuffer.Add(new GateInput
                    {InputEntity = conversionSystem.TryGetPrimaryEntity(input.gameObject)});
            }
        }

        var wireAuthoring = wirePrefab.GetComponent<WireAuthoring>();
        foreach (var input in inputs)
        {
            // Create a linked entity for the wires leading from the input to this gate
            var wireStartPos = input.outputAttachTransform.position;
            var wireEndPos = inputAttachTransform.position;
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
                typeof(WireInput)}));

            // Store the wire's palette of materials in a separate shared component
            var wireMaterials = new WireMaterials(wireAuthoring.offMaterial, wireAuthoring.onMaterial);
            dstManager.AddSharedComponentData(wireEntity, wireMaterials);
            // Copy & modify the gate's RenderMesh to reference the correct mesh/material.
            var baseRenderMesh = dstManager.GetSharedComponentData<RenderMesh>(gateEntity);
            baseRenderMesh.mesh = wirePrefab.GetComponent<MeshFilter>().sharedMesh;
            baseRenderMesh.material = wireMaterials.OffMaterial;
            dstManager.AddSharedComponentData(wireEntity, baseRenderMesh);

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