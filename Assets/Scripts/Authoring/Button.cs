using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class Button : MonoBehaviour, IConvertGameObjectToEntity
{
    public bool initiallyOn;

    public void Convert(Entity buttonEntity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var componentTypes = new List<ComponentType>
        {
            typeof(ClickableNode),
            typeof(NodeOutput),
            typeof(DagDepth),
        };
        dstManager.AddComponents(buttonEntity, new ComponentTypes(componentTypes.ToArray()));

        var box = GetComponent<BoxCollider2D>();
        var bounds = box.bounds;
        var boundsMin = bounds.min;
        var boundsMax = bounds.max;
        dstManager.SetComponentData(buttonEntity, new ClickableNode
        {
            RectMin = new float2(boundsMin.x, boundsMin.y),
            RectMax = new float2(boundsMax.x, boundsMax.y),
        });

        if (initiallyOn)
        {
            // Set initial NodeOutput
            dstManager.SetComponentData(buttonEntity, new NodeOutput {Value = 1});
            // Override default material
            var matSwapper = GetComponent<MaterialSwapper>();
            var renderMesh = dstManager.GetSharedComponentData<RenderMesh>(buttonEntity);
            renderMesh.material = matSwapper.onMaterial;
            dstManager.SetSharedComponentData(buttonEntity, renderMesh);
        }
 
    }
}
