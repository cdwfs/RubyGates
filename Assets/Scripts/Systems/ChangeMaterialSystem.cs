using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;

// TODO(cort): Managed components XP
public class MaterialPalette : IComponentData {
    public MaterialPalette() {
        Materials = new Material[0];
    }
    public MaterialPalette(in Material[] materials) {
        Materials = materials;
    }
    public Material[] Materials;
}

public struct MaterialChange : IComponentData
{
    public Entity entity;
    public int materialIndex;
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateBefore(typeof(RenderMeshSystemV2))]
public class ChangeMaterialSystem : SystemBase
{
    EntityQuery _materialChangeQuery;
    protected override void OnCreate() {
        _materialChangeQuery = GetEntityQuery(ComponentType.ReadOnly<MaterialChange>());
    }
    protected override void OnUpdate()
    {
        Entities
            .WithName("ChangeMaterialSystem")
            .WithoutBurst() // Manipulates managed components
            .WithStructuralChanges() // Calls EntityManager.GetSharedComponentData and .SetSharedComponentData()
            .ForEach((Entity entity, in MaterialChange change, in MaterialPalette palette) => {
                var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(change.entity);
                Assert.IsTrue(change.materialIndex >= 0 || change.materialIndex < palette.Materials.Length);
                //Debug.Log($"Changing {EntityManager.GetName(change.entity)} mat from {renderMesh.material.name} to {change.materialIndex} ({palette.Materials[change.materialIndex].name})");
                renderMesh.material = palette.Materials[change.materialIndex];
                EntityManager.SetSharedComponentData(change.entity, renderMesh);
            }).Run();
        // Remove change request components
        EntityManager.RemoveComponent<MaterialChange>(_materialChangeQuery);
    }
}