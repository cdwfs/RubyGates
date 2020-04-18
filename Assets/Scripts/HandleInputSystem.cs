using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class HandleInputSystem : SystemBase
{
    BeginPresentationEntityCommandBufferSystem beginPresEcbSystem;
    protected override void OnCreate() {
        beginPresEcbSystem = World.GetExistingSystem<BeginPresentationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        if (!Input.GetMouseButtonDown(0))
            return;
        if (Camera.main == null)
            return;
        float2 clickPos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var ecb = beginPresEcbSystem.CreateCommandBuffer().ToConcurrent();
        // TODO: currently only handles buttons. Switches will require additional components.
        var job = Entities
            .WithName("HandleInputSystem")
            .ForEach((Entity buttonEntity, int entityInQueryIndex, ref NodeOutput output, in ClickableNode clickable) =>
            {
                if (math.all(clickPos > clickable.RectMin) && math.all(clickPos < clickable.RectMax))
                {
                    output.PrevValue = output.Value;
                    output.Value = 1 - output.Value;
                    // Change material
                    ecb.AddComponent(entityInQueryIndex, buttonEntity, new MaterialChange {
                        entity = buttonEntity,
                        materialIndex = output.Value,
                    });
                }
            }).ScheduleParallel(Dependency);
        beginPresEcbSystem.AddJobHandleForProducer(job);
        Dependency = job;
    }
}