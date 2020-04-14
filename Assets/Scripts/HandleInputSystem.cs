using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class HandleInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (!Input.GetMouseButtonDown(0))
            return;
        if (Camera.main == null)
            return;
        float2 clickPos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // TODO: currently only handles buttons. Switches will require additional components.
        Entities
            .WithName("HandleInputSystem")
            .ForEach((ref NodeOutput output, in ClickableNode clickable) =>
            {
                if (math.all(clickPos > clickable.RectMin) && math.all(clickPos < clickable.RectMax))
                {
                    output.PrevValue = output.Value;
                    output.Value = 1 - output.Value;
                }
            }).ScheduleParallel();
    }
}