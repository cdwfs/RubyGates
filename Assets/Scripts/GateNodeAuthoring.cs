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
    public GameObject activeWirePrefab;
    public GameObject inactiveWirePrefab;

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
        referencedPrefabs.Add(activeWirePrefab);
        referencedPrefabs.Add(inactiveWirePrefab);
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

        var flip = true; // temp hack so I can see both wire prefabs
        foreach (var input in inputs)
        {
            // Create a linked entity for the wires leading from the input to this gate
            var wireStartPos = input.outputAttachTransform.position;
            var wireEndPos = inputAttachTransform.position;
            // TODO(cort): conversionSystem.InstantiateAdditionalEntity() would be ideal here. Instead, copy what we need from the prefab.
            var wireEntity = conversionSystem.CreateAdditionalEntity(gameObject);
            dstManager.SetName(wireEntity, "Wire");
            dstManager.AddComponents(wireEntity, new ComponentTypes(
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(Translation),
                typeof(Rotation),
                typeof(NonUniformScale)));

            var activeWirePrefabEntity = conversionSystem.GetPrimaryEntity(activeWirePrefab);
            var inactiveWirePrefabEntity = conversionSystem.GetPrimaryEntity(inactiveWirePrefab);
            dstManager.AddSharedComponentData(wireEntity,
                flip
                    ? dstManager.GetSharedComponentData<RenderMesh>(activeWirePrefabEntity)
                    : dstManager.GetSharedComponentData<RenderMesh>(inactiveWirePrefabEntity));
            flip = !flip;

            dstManager.SetComponentData(wireEntity, new Translation
            {
                Value = wireStartPos,
            });

            var wireScale = activeWirePrefab.transform.localScale;
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
        }
    }
}