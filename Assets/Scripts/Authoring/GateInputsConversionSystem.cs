using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(GameObjectAfterConversionGroup))] // After = requires MeshRenderer conversion
[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
public class GateInputsConversionSystem : GameObjectConversionSystem
{
    void CreateWireEntities(Component rootComponent, Wire wirePrefab, Vector3 wireEndPos, params Gate[] inputNodes)
    {
        if (wirePrefab == null)
            return;
        var wireMaterials = wirePrefab.GetComponent<MaterialSwapper>();
        var wireMeshFilter = wirePrefab.GetComponent<MeshFilter>();
        var wireTransform = wirePrefab.GetComponent<Transform>();

        // Copy & modify the gate's RenderMesh to reference the correct mesh/material.
        var gateEntity = GetPrimaryEntity(rootComponent);
        var baseRenderMesh = DstEntityManager.GetSharedComponentData<RenderMesh>(gateEntity);
        baseRenderMesh.mesh = wireMeshFilter.sharedMesh;
        baseRenderMesh.material = wireMaterials.offMaterial;
        
        foreach (var inputNode in inputNodes)
        {
            // Create a linked entity for the wires leading from the input to this gate
            var wireStartPos = inputNode.outputAttachTransform.position;
            // TODO(cort): conversionSystem.InstantiateAdditionalEntity() would be ideal here. Instead, copy what we need from the prefab.
            var wireEntity = CreateAdditionalEntity(rootComponent);
#if UNITY_EDITOR
            DstEntityManager.SetName(wireEntity, "Wire"); // TODO: make this call [Conditional]
#endif
            DstEntityManager.AddComponents(wireEntity, new ComponentTypes(new ComponentType[]
            {
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(Translation),
                typeof(Rotation),
                typeof(NonUniformScale),
                typeof(MaterialPalette),
                typeof(WireInput)
            }));

            // Store the wire's palette of materials in a separate shared component
            DstEntityManager.SetComponentData(wireEntity, new MaterialPalette(new[]
            {
                wireMaterials.offMaterial,
                wireMaterials.onMaterial,
            }));
            DstEntityManager.AddSharedComponentData(wireEntity, baseRenderMesh);

            // TODO(https://github.com/cdwfs/RubyGates/issues/9): pre-bake the LocalToWorld and add the Static tag
            DstEntityManager.SetComponentData(wireEntity, new Translation
            {
                Value = wireStartPos,
            });

            var wireScale = wireTransform.localScale;
            wireScale.y = Vector3.Distance(wireStartPos, wireEndPos);
            DstEntityManager.SetComponentData(wireEntity, new NonUniformScale
            {
                Value = wireScale,
            });

            // We want to measure an angle relative to +Y while looking in -Z.
            float3 wireDir = wireEndPos - wireStartPos;
            float wireAngle = math.atan2(-wireDir.x, wireDir.y);
            DstEntityManager.SetComponentData(wireEntity, new Rotation
            {
                Value = quaternion.RotateZ(wireAngle),
            });

            DstEntityManager.SetComponentData(wireEntity, new WireInput
            {
                InputEntity = GetPrimaryEntity(inputNode.gameObject),
            });
        }
    }
    
    protected override void OnUpdate()
    {
        Entities.ForEach((GateInputs1 inputs1) =>
        {
            var gateEntity = GetPrimaryEntity(inputs1);
            var inputsBuffer = DstEntityManager.AddBuffer<NodeInput>(gateEntity);
            inputsBuffer.Capacity = 1;
            inputsBuffer.Add(new NodeInput {InputEntity = GetPrimaryEntity(inputs1.inputNode.gameObject)});

            CreateWireEntities(inputs1, inputs1.wirePrefab, inputs1.attachTransform.position, inputs1.inputNode);
        });
        
        Entities.ForEach((GateInputs2 inputs2) =>
        {
            var gateEntity = GetPrimaryEntity(inputs2);
            var inputsBuffer = DstEntityManager.AddBuffer<NodeInput>(gateEntity);
            inputsBuffer.Capacity = 2;
            inputsBuffer.Add(new NodeInput {InputEntity = GetPrimaryEntity(inputs2.inputNodeL.gameObject)});
            inputsBuffer.Add(new NodeInput {InputEntity = GetPrimaryEntity(inputs2.inputNodeR.gameObject)});

            CreateWireEntities(inputs2, inputs2.wirePrefab, inputs2.attachTransformL.position, inputs2.inputNodeL);
            CreateWireEntities(inputs2, inputs2.wirePrefab, inputs2.attachTransformR.position, inputs2.inputNodeR);
        });
        
        Entities.ForEach((GateInputsN inputsN) =>
        {
            var gateEntity = GetPrimaryEntity(inputsN);
            var inputsBuffer = DstEntityManager.AddBuffer<NodeInput>(gateEntity);
            inputsBuffer.Capacity = inputsN.inputNodes.Length;
            foreach(var inputNode in inputsN.inputNodes)
                inputsBuffer.Add(new NodeInput {InputEntity = GetPrimaryEntity(inputNode.gameObject)});

            CreateWireEntities(inputsN, inputsN.wirePrefab, inputsN.attachTransform.position, inputsN.inputNodes);
        });
    }
}