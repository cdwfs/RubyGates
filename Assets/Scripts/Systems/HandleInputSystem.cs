using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public struct ToggleCount : IComponentData
{
    public int Value;
}

public struct ClickableNode : IComponentData
{
    public float2 RectMin;
    public float2 RectMax;
}

[MaterialProperty("_IsHovering", MaterialPropertyFormat.Float)]
public struct IsMouseHovering : IComponentData
{
    public float Value;
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

public partial class HandleInputSystem : SystemBase
{
    private UIManager _uiManager;

    protected override void OnUpdate()
    {
        // If the UI system has a modal dialog up, ignore in-game input
        if (_uiManager == null)
            _uiManager = GameObject.FindObjectOfType<UIManager>();
        if (_uiManager != null && _uiManager.IsModalDialogActive)
        {
            return;
        }
        
        if (Camera.main == null)
            return;
        float2 mousePos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

        bool clicked = Input.GetMouseButtonDown(0);
        var gateInfos = GetComponentDataFromEntity<GateInfo>(false);
        var branchStates = GetComponentDataFromEntity<BranchState>(false);
        var clickJob = Entities
            .WithName("HandleMouseInput")
            .WithNativeDisableContainerSafetyRestriction(gateInfos)
            .WithNativeDisableContainerSafetyRestriction(branchStates)
            .ForEach((Entity clickableEntity, ref NodeOutput output, ref GateInfo gateInfo, ref IsMouseHovering isMouseHovering,
                ref ToggleCount toggleCount, in ClickableNode clickable) =>
            {
                bool toggledAThing = false;
                isMouseHovering.Value =
                    math.all(mousePos > clickable.RectMin) && math.all(mousePos < clickable.RectMax) ? 1.0f : 0.0f;
                if (isMouseHovering.Value > 0.0f && clicked)
                {
                    switch(gateInfo.Type)
                    {
                        // Button: clicking changes output value
                        case GateType.Button:
                        {
                            output.Value = 1 - output.Value;
                            toggledAThing = true;
                            break;
                        }
                        // Branch: clicking changes node type
                        case GateType.BranchOff:
                        {
                            gateInfo.Type = GateType.BranchOn;
                            toggledAThing = true;
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
                            toggledAThing = true;
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
                if (toggledAThing)
                {
                    toggleCount.Value += 1;
                }
            }).ScheduleParallel(Dependency);
        Dependency = clickJob;
    }
}