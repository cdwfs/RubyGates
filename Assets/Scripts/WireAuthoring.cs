using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class WireAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public Material offMaterial;
    public Material onMaterial;
    public void Convert(Entity wireEntity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponents(wireEntity, new ComponentTypes(
            typeof(WireInput) // TODO(cort): this is currently unused, because wire entities are configured in GateNodeAuthoring from scratch.
            ));
        dstManager.AddSharedComponentData(wireEntity, new WireMaterials(offMaterial, onMaterial));
    }
}
