using Unity.Assertions;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class VictorySystem : SystemBase
{
    EntityQuery _clickableNodeQuery;
    EndInitializationEntityCommandBufferSystem _endInitEcbSystem;
    protected override void OnCreate() {
        _clickableNodeQuery = GetEntityQuery(typeof(ClickableNode), typeof(DagDepth));
        _endInitEcbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = _endInitEcbSystem.CreateCommandBuffer();
        Entities
            .WithName("VictorySystem")
            .WithoutBurst() // Manipulates GameObjects
            .WithAll<VictoryTag>()
            .ForEach((Entity sinkEntity, in NodeOutput output) =>
            {
                // Remove the Victory tag so this system only runs once
                ecb.RemoveComponent<VictoryTag>(sinkEntity);

                // Disable mouse interaction with nodes once victory is detected
                ecb.RemoveComponent<ClickableNode>(_clickableNodeQuery);

                // Victory!
                var victory = GameObject.Find("Victory").GetComponent<Victory>();
                Assert.IsNotNull(victory);
                victory.DoVictoryDance();
            }).Run();
    }
}