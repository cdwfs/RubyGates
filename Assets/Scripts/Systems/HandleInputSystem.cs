using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
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

[MaterialProperty("_BranchState", MaterialPropertyFormat.Float)]
public struct BranchState : IComponentData
{
    public float Value;
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
        var branchStates = GetComponentDataFromEntity<BranchState>(false);

        var ecb = _beginPresEcbSystem.CreateCommandBuffer().AsParallelWriter();
        var clickJob = Entities
            .WithName("HandleInputSystem")
            .WithNativeDisableContainerSafetyRestriction(gateInfos)
            .WithNativeDisableContainerSafetyRestriction(branchStates)
            .ForEach((Entity clickableEntity, int entityInQueryIndex, ref NodeOutput output, ref GateInfo gateInfo, in ClickableNode clickable) =>
            {
                if (math.all(clickPos > clickable.RectMin) && math.all(clickPos < clickable.RectMax))
                {
                    switch(gateInfo.Type)
                    {
                        // Button: clicking changes output value
                        case GateType.Button:
                        {
                            output.Value = 1 - output.Value;
                            break;
                        }
                        // Branch: clicking changes node type
                        case GateType.BranchOff:
                        {
                            gateInfo.Type = GateType.BranchOn;
                            // Update partner entity's type
                            var partnerEntity = GetComponent<BranchPartner>(clickableEntity).PartnerEntity;
                            var partnerGateInfo = gateInfos[partnerEntity];
                            partnerGateInfo.Type = GateType.BranchOff;
                            gateInfos[partnerEntity] = partnerGateInfo;
                            // Toggle branch state
                            var branchState = branchStates[clickableEntity];
                            branchState.Value = 1 - branchState.Value;
                            branchStates[clickableEntity] = branchState;
                            break;
                        }
                        case GateType.BranchOn:
                        {
                            gateInfo.Type = GateType.BranchOff;
                            // Update partner entity's type
                            var partnerEntity = GetComponent<BranchPartner>(clickableEntity).PartnerEntity;
                            var partnerGateInfo = gateInfos[partnerEntity];
                            partnerGateInfo.Type = GateType.BranchOn;
                            gateInfos[partnerEntity] = partnerGateInfo;
                            // Toggle branch state
                            var branchState = branchStates[clickableEntity];
                            branchState.Value = 1 - branchState.Value;
                            branchStates[clickableEntity] = branchState;
                            break;
                        }
                    }
                }
            }).ScheduleParallel(Dependency);
        _beginPresEcbSystem.AddJobHandleForProducer(clickJob);
        Dependency = clickJob;
    }
}