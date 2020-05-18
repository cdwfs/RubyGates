using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[DisallowMultipleComponent]
public class Button : MonoBehaviour
{
    public bool initiallyOn;
}

[UpdateInGroup(typeof(GameObjectAfterConversionGroup))] // After = requires MeshRenderer conversion
[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
public class ButtonConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Button button, BoxCollider2D box, MaterialSwapper matSwapper) =>
        {
            var buttonEntity = GetPrimaryEntity(button);

            DstEntityManager.AddComponents(buttonEntity, new ComponentTypes(new ComponentType[]
            {
                typeof(ClickableNode),
                typeof(NodeOutput),
                typeof(DagDepth),
            }));
            
            // Convert bounding box to ClickableNode
            var bounds = box.bounds;
            var boundsMin = bounds.min;
            var boundsMax = bounds.max;
            DstEntityManager.SetComponentData(buttonEntity, new ClickableNode
            {
                RectMin = new float2(boundsMin.x, boundsMin.y),
                RectMax = new float2(boundsMax.x, boundsMax.y),
            });

            if (button.initiallyOn)
            {
                // Set initial NodeOutput
                DstEntityManager.SetComponentData(buttonEntity, new NodeOutput {Value = 1});
                // Override default material
                var renderMesh = DstEntityManager.GetSharedComponentData<RenderMesh>(buttonEntity);
                renderMesh.material = matSwapper.onMaterial;
                DstEntityManager.SetSharedComponentData(buttonEntity, renderMesh);
            }
            
        });
    }
}
