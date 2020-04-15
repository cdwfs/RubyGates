using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

[UpdateAfter(typeof(GatePropagateSystem))]
public class SinkSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Kinda wasteful to go find these every frame, but...enh, the system only runs once/frame
        var po = GameObject.Find("VictoryParticles");
        var particles = po.GetComponent<ParticleSystem>();
        
        // Has to be main thread & non-bursted due to managed component manipulation & structural changes.
        Entities
            .WithName("SinkSystem")
            .WithoutBurst()
            .WithStructuralChanges()
            .WithAll<SinkTag>()
            .ForEach((Entity sinkEntity, in NodeOutput output) =>
            {
                if (output.Value == 1 && output.Changed)
                {
                    // Victory!
                    
                    // Spew particles
                    if (particles != null)
                        particles.Play();
                    
                    // Change material
                    var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(sinkEntity);
                    var sinkMaterials = EntityManager.GetSharedComponentData<SinkMaterials>(sinkEntity);
                    renderMesh.material = sinkMaterials.OnMaterial;
                    EntityManager.SetSharedComponentData<RenderMesh>(sinkEntity, renderMesh);
                }
            }).Run();
    }
}