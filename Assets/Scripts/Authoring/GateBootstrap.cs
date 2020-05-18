using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class GateBootstrap : MonoBehaviour
{
    // Just a placeholder to force a GateBootstrap entity to be created
}

[UpdateInGroup(typeof(GameObjectAfterConversionGroup))] // After = requires MeshRenderer conversion
[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
public class GateBootstrapConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((GateBootstrap bootstrap) =>
        {
            var bootstrapEntity = GetPrimaryEntity(bootstrap);
            DstEntityManager.AddComponent<DagIsStale>(bootstrapEntity);
        });
    }
}