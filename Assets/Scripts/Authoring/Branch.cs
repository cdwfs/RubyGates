﻿using System.Collections.Generic;
using Unity.Assertions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[DisallowMultipleComponent]
public class Branch : MonoBehaviour
{
    public bool initiallyRight;
    public Gate partnerGate;
}

[UpdateInGroup(typeof(GameObjectAfterConversionGroup))] // After = requires MeshRenderer and Gate conversion to complete
[UpdateAfter(typeof(GateInputsConversionSystem))] // adjusts input node for sub-entity
[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
public class SwitchConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Branch branch, BoxCollider2D box, MaterialSwapper matSwapper) =>
        {
            var branchEntity = GetPrimaryEntity(branch);

            // Branch prefabs have a child object that is also a gate
            var partnerGate = branch.partnerGate;
            Assert.IsNotNull(partnerGate);
            Assert.AreEqual(branch.gameObject, partnerGate.transform.parent.gameObject);
            var partnerEntity = GetPrimaryEntity(partnerGate);
            // And both entities should already have their NodeInput component added
            Assert.IsTrue(DstEntityManager.HasComponent<NodeInput>(branchEntity));
            Assert.IsTrue(DstEntityManager.HasComponent<NodeInput>(partnerEntity));
            
            DstEntityManager.AddComponents(branchEntity, new ComponentTypes(new ComponentType[]
            {
                typeof(ClickableNode),
                typeof(BranchPartner),
            }));
            
            // Partner entity should use the same input node as the base branch entity
            DstEntityManager.SetComponentData(branchEntity, new BranchPartner {PartnerEntity = partnerEntity});
            
            // Partner entity should use the same input node as the base branch entity
            var branchInputBuffer = DstEntityManager.GetBuffer<NodeInput>(branchEntity);
            var partnerInputBuffer = DstEntityManager.GetBuffer<NodeInput>(partnerEntity);
            Assert.AreEqual(1, branchInputBuffer.Length);
            Assert.AreEqual(1, partnerInputBuffer.Length);
            partnerInputBuffer[0] = branchInputBuffer[0];
            
            // Convert bounding box to ClickableNode
            var bounds = box.bounds;
            var boundsMin = bounds.min;
            var boundsMax = bounds.max;
            DstEntityManager.SetComponentData(branchEntity, new ClickableNode
            {
                RectMin = new float2(boundsMin.x, boundsMin.y),
                RectMax = new float2(boundsMax.x, boundsMax.y),
            });

            if (branch.initiallyRight)
            {
                // Set initial GateType for both sides of the branch
                var branchGateInfo = DstEntityManager.GetComponentData<GateInfo>(branchEntity);
                branchGateInfo.Type = GateType.BranchOff;
                DstEntityManager.SetComponentData(branchEntity, branchGateInfo);
                var partnerGateInfo = DstEntityManager.GetComponentData<GateInfo>(partnerEntity);
                partnerGateInfo.Type = GateType.BranchOn;
                DstEntityManager.SetComponentData(partnerEntity, partnerGateInfo);
                // Override default material
                var renderMesh = DstEntityManager.GetSharedComponentData<RenderMesh>(branchEntity);
                renderMesh.material = matSwapper.onMaterial;
                DstEntityManager.SetSharedComponentData(branchEntity, renderMesh);
            }
            else
            {
                // Set initial GateType for both sides of the branch
                var branchGateInfo = DstEntityManager.GetComponentData<GateInfo>(branchEntity);
                branchGateInfo.Type = GateType.BranchOn;
                DstEntityManager.SetComponentData(branchEntity, branchGateInfo);
                var partnerGateInfo = DstEntityManager.GetComponentData<GateInfo>(partnerEntity);
                partnerGateInfo.Type = GateType.BranchOff;
                DstEntityManager.SetComponentData(partnerEntity, partnerGateInfo);
                // Override default material
                var renderMesh = DstEntityManager.GetSharedComponentData<RenderMesh>(branchEntity);
                renderMesh.material = matSwapper.offMaterial;
                DstEntityManager.SetSharedComponentData(branchEntity, renderMesh);
            }
        });
    }
}