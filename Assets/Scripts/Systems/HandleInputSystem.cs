using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct ClickableNode : IComponentData
{
    public float2 RectMin;
    public float2 RectMax;
}

public struct BranchPartner : IComponentData
{
    public Entity PartnerEntity;
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
        
        var gateInfos = GetComponentDataFromEntity<GateInfo>(false);

        var ecb = _beginPresEcbSystem.CreateCommandBuffer().AsParallelWriter();
        var clickJob = Entities
            .WithName("HandleInputSystem")
            .WithNativeDisableContainerSafetyRestriction(gateInfos)
            .ForEach((Entity clickableEntity, int entityInQueryIndex, ref NodeOutput output, ref GateInfo gateInfo, in ClickableNode clickable) =>
            {
                if (math.all(clickPos > clickable.RectMin) && math.all(clickPos < clickable.RectMax))
                {
                    int newMaterialIndex = -1;
                    switch(gateInfo.Type)
                    {
                        // Button: clicking changes output value
                        case GateType.Button:
                        {
                            output.PrevValue = output.Value;
                            output.Value = 1 - output.Value;
                            newMaterialIndex = output.Value;
                            break;
                        }
                        // Branch: clicking changes node type
                        case GateType.BranchOff:
                        {
                            gateInfo.Type = GateType.BranchOn;
                            newMaterialIndex = 0;
                            // Update partner entity's type
                            var partnerEntity = GetComponent<BranchPartner>(clickableEntity).PartnerEntity;
                            var partnerGateInfo = gateInfos[partnerEntity];
                            partnerGateInfo.Type = GateType.BranchOff;
                            gateInfos[partnerEntity] = partnerGateInfo;
                            break;
                        }
                        case GateType.BranchOn:
                        {
                            gateInfo.Type = GateType.BranchOff;
                            newMaterialIndex = 1;
                            // Update partner entity's type
                            var partnerEntity = GetComponent<BranchPartner>(clickableEntity).PartnerEntity;
                            var partnerGateInfo = gateInfos[partnerEntity];
                            partnerGateInfo.Type = GateType.BranchOn;
                            gateInfos[partnerEntity] = partnerGateInfo;
                            break;
                        }
                    }
                    
                    // Change material
                    ecb.AddComponent(entityInQueryIndex, clickableEntity, new MaterialChange {
                        entity = clickableEntity,
                        materialIndex = newMaterialIndex,
                    });
                }
            }).ScheduleParallel(Dependency);
        _beginPresEcbSystem.AddJobHandleForProducer(clickJob);
        Dependency = clickJob;
    }
}