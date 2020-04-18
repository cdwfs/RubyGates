using Unity.Entities;
using UnityEngine;

[UpdateAfter(typeof(GatePropagateSystem))]
public class SinkSystem : SystemBase
{
    BeginPresentationEntityCommandBufferSystem beginPresEcbSystem;
    protected override void OnCreate() {
        beginPresEcbSystem = World.GetExistingSystem<BeginPresentationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        // Kinda wasteful to go find these every frame, but...enh, the system only runs once/frame
        var po = GameObject.Find("VictoryParticles");
        var particles = po.GetComponent<ParticleSystem>();

        var ecb = beginPresEcbSystem.CreateCommandBuffer();
        // Has to be main thread & non-bursted due to managed component manipulation.
        Entities
            .WithName("SinkSystem")
            .WithoutBurst()
            .WithAll<SinkTag>()
            .ForEach((Entity sinkEntity, in NodeOutput output) =>
            {
                if (output.Value == 1 && output.Changed)
                {
                    // Victory!
                    
                    // Spew particles
                    if (particles != null)
                        particles.Play();

                    // Enqueue material change request
                    ecb.AddComponent(sinkEntity, new MaterialChange {
                        entity = sinkEntity,
                        materialIndex = 1,
                    });
                }
            }).Run();
    }
}