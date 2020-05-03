using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct ClickableNode : IComponentData
{
    public float2 RectMin;
    public float2 RectMax;
}

public class HandleInputSystem : SystemBase
{
    BeginPresentationEntityCommandBufferSystem _beginPresEcbSystem;
    private UIManager _uiManager;
    
    protected override void OnCreate() {
        _beginPresEcbSystem = World.GetExistingSystem<BeginPresentationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        // If the UI system has a modal dialog up, ignore in-game input
        if (_uiManager == null)
            _uiManager = GameObject.FindObjectOfType<UIManager>();
        if (_uiManager != null && _uiManager.IsModalDialogActive)
        {
            return;
        }
        
        if (!Input.GetMouseButtonDown(0))
            return;
        
        if (Camera.main == null)
            return;
        float2 clickPos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        var ecb = _beginPresEcbSystem.CreateCommandBuffer().ToConcurrent();
        // TODO(https://github.com/cdwfs/RubyGates/issues/4): currently only handles buttons. Switches will require additional components.
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
        _beginPresEcbSystem.AddJobHandleForProducer(job);
        Dependency = job;
    }
}