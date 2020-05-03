using Unity.Entities;
using UnityEngine;


[DisallowMultipleComponent]
[RequiresEntityConversion]
public class Sink : MonoBehaviour, IConvertGameObjectToEntity
{
    public Material offMaterial;
    public Material onMaterial;
    public void Convert(Entity sinkEntity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponents(sinkEntity, new ComponentTypes(new ComponentType[] {
            typeof(MaterialPalette),
        }));
        dstManager.SetComponentData(sinkEntity, new MaterialPalette(new[] {
            offMaterial,
            onMaterial,
        }));
    }
}
