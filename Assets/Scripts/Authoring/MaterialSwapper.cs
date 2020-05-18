using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class MaterialSwapper : MonoBehaviour
{
    public Material offMaterial;
    public Material onMaterial;
}

[UpdateInGroup(typeof(GameObjectConversionGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
public class MaterialPaletteConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((MaterialSwapper matSwapper) =>
        {
            var targetEnt = GetPrimaryEntity(matSwapper);
            DstEntityManager.AddComponentData(targetEnt,
                new MaterialPalette(new[]
                {
                    matSwapper.offMaterial,
                    matSwapper.onMaterial,
                }));
        });
    }
}