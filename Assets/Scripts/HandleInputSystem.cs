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
        Entities
            .WithName("HandleInputSystem")
            .ForEach((ref GateOutput output, in ClickableGate clickable, in GateTypeComponent gateType) =>
            {
                if (math.all(clickPos > clickable.RectMin) && math.all(clickPos < clickable.RectMax))
                {
                    output.Value = 1 - output.Value;
                }
            }).ScheduleParallel();
    }
}