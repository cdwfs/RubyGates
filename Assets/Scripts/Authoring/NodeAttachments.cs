using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Assertions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[DisallowMultipleComponent]
public class NodeAttachments : MonoBehaviour, IDeclareReferencedPrefabs
{
    public Wire wirePrefab;
    public Transform outputAttachTransform;
    public Transform[] inputAttachTransforms;
    public NodeAttachments[] inputNodes;
    
    protected void DrawGizmos()
    {
        // Draw lines from this node's inputs' output attach points to this node's input attach point.
        Gizmos.color = Color.yellow;
        int inputCount = inputNodes.Length;
        for(int iInput=0; iInput<inputCount; ++iInput)
        {
            Gizmos.DrawLine(
                inputNodes[iInput].outputAttachTransform.position,
                inputAttachTransforms[iInput].position);
        }

        // Little tip on each line
        Gizmos.color = Color.blue;
        for(int iInput=0; iInput<inputCount; ++iInput)
        {
            Gizmos.DrawLine(
                Vector3.Lerp(inputNodes[iInput].outputAttachTransform.position, inputAttachTransforms[iInput].position, 0.9f),
                    inputAttachTransforms[iInput].position);
        }
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(wirePrefab.gameObject);
    }
}

[UpdateInGroup(typeof(GameObjectAfterConversionGroup))] // After = requires MeshRenderer conversion
[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
public class NodeAttachPointConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((NodeAttachments nodeAttachments) =>
        {
            var nodeEntity = GetPrimaryEntity(nodeAttachments);

            int inputCount = nodeAttachments.inputAttachTransforms.Length;
            Assert.AreEqual(inputCount, nodeAttachments.inputNodes.Length);
            if (inputCount == 0)
                return; // skip nodes with no inputs

            // Store references to the input gate entities
            {
                var inputsBuffer = DstEntityManager.AddBuffer<NodeInput>(nodeEntity);
                inputsBuffer.Capacity = inputCount;
                for (int iInput = 0; iInput < inputCount; ++iInput)
                {
                    var inputNode = nodeAttachments.inputNodes[iInput];
                    inputsBuffer.Add(new NodeInput {InputEntity = GetPrimaryEntity(inputNode.gameObject)});
                }
            }

            // Temporary wire-conversion stuff
            var wireMaterials = nodeAttachments.wirePrefab.GetComponent<MaterialSwapper>();
            var wireMeshFilter = nodeAttachments.wirePrefab.GetComponent<MeshFilter>();
            var wireTransform = nodeAttachments.wirePrefab.GetComponent<Transform>();
            for (int iInput = 0; iInput < inputCount; ++iInput)
            {
                var inputNode = nodeAttachments.inputNodes[iInput];
                
                // Create a linked entity for the wires leading from the input to this gate
                var wireStartPos = inputNode.outputAttachTransform.position;
                var wireEndPos = nodeAttachments.inputAttachTransforms[iInput].position;
                // TODO(cort): conversionSystem.InstantiateAdditionalEntity() would be ideal here. Instead, copy what we need from the prefab.
                var wireEntity = CreateAdditionalEntity(nodeAttachments);
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
                // Copy & modify the gate's RenderMesh to reference the correct mesh/material.
                var baseRenderMesh = DstEntityManager.GetSharedComponentData<RenderMesh>(nodeEntity);
                baseRenderMesh.mesh = wireMeshFilter.sharedMesh;
                baseRenderMesh.material = wireMaterials.offMaterial;
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
        });
    }
}