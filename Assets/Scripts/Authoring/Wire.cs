using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class Wire : MonoBehaviour, IConvertGameObjectToEntity
{
    public Material offMaterial;
    public Material onMaterial;
    public void Convert(Entity wireEntity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // TODO(cort): this is currently unused, because wire entities are configured in GateNodeAuthoring from scratch.
        // Need a InstantiateAdditionalEntity() for this to be useful.
        dstManager.AddComponents(wireEntity, new ComponentTypes(new ComponentType[] {
            typeof(WireInput),
            typeof(MaterialPalette),
        }));
        dstManager.SetComponentData(wireEntity, new MaterialPalette(new[] {
            offMaterial,
            onMaterial,
        }));
    }
}
