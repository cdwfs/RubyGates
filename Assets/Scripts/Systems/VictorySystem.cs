using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public struct VictoryTag : IComponentData {
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class VictorySystem : SystemBase
{
    EntityQuery _clickableNodeQuery;
    EndInitializationEntityCommandBufferSystem _endInitEcbSystem;
    protected override void OnCreate() {
        _clickableNodeQuery = GetEntityQuery(typeof(ClickableNode), typeof(DagDepth));
        _endInitEcbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
        RequireSingletonForUpdate<VictoryTag>();
    }

    protected override void OnUpdate()
    {
        var totalToggleCount = new NativeReference<int>(Allocator.TempJob);
        Entities.ForEach((in ToggleCount toggleCount) =>
        {
            totalToggleCount.Value = totalToggleCount.Value + toggleCount.Value;
        }).Run();

        var ecb = _endInitEcbSystem.CreateCommandBuffer();
        Entities
            .WithName("VictorySystem")
            .WithoutBurst() // Manipulates GameObjects
            .WithAll<VictoryTag>()
            .ForEach((Entity sinkEntity, ParticleSystem particles, in NodeOutput output) =>
            {
                // Remove the Victory tag so this system only runs once
                ecb.RemoveComponent<VictoryTag>(sinkEntity);

                // Disable mouse interaction with nodes once victory is detected
                ecb.RemoveComponent<ClickableNode>(_clickableNodeQuery);

                particles.Play();

                // Victory!
                var uiMgr = GameObject.FindObjectOfType<UIManager>();
                Assert.IsNotNull(uiMgr);
                uiMgr.ShowVictoryPanel(true, totalToggleCount.Value);
            }).Run();

        totalToggleCount.Dispose();
    }
}