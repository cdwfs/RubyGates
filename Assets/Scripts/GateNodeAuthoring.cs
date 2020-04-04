using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class GateNodeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public GateType GateType;
    public GateNodeAuthoring[] inputs;
    public Transform inputAttachTransform;
    public Transform outputAttachTransform;

    private void OnDrawGizmos()
    {
        // Draw lines from this node's output attach points to this node's input attach point.
        Gizmos.color = Color.yellow;
        foreach (var input in inputs)
        {
            Gizmos.DrawLine(input.outputAttachTransform.position, inputAttachTransform.position);
        }
        // Little tip on each line
        Gizmos.color = Color.blue;
        foreach (var input in inputs)
        {
            Gizmos.DrawLine(
                Vector3.Lerp(input.outputAttachTransform.position, inputAttachTransform.position, 0.9f),
                inputAttachTransform.position);
        }
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponents(entity, new ComponentTypes(
            typeof(GateOutput),
            typeof(GateTypeComponent),
            typeof(GateInput),
            typeof(GateDagDepth)));

        dstManager.SetComponentData(entity, new GateTypeComponent {value = GateType});

        var inputsBuffer = dstManager.GetBuffer<GateInput>(entity);
        inputsBuffer.Capacity = inputs.Length;
        foreach (var input in inputs)
        {
            inputsBuffer.Add(new GateInput {inputEntity = conversionSystem.TryGetPrimaryEntity(input.gameObject)});
        }
    }
}
