using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class SinkAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public Material offMaterial;
    public Material onMaterial;
    public void Convert(Entity sinkEntity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<SinkTag>(sinkEntity);
        dstManager.AddSharedComponentData(sinkEntity, new SinkMaterials(offMaterial, onMaterial));
    }
}
