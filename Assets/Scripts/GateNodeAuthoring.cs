using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class GateNodeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public GateType GateType;
    public GateNodeAuthoring[] inputs;

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
