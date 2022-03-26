using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class Sink : MonoBehaviour
{
}

[UpdateInGroup(typeof(GameObjectConversionGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
public class SinkConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Sink sink, ParticleSystem ps, ParticleSystemRenderer psr) =>
        {
            // Every scene needs a Sink, so it can drive the DAG sorting.
            var sinkEntity = GetPrimaryEntity(sink);
            DstEntityManager.AddComponent<DagIsStale>(sinkEntity);
            DstEntityManager.AddComponentObject(sinkEntity, ps);
            DstEntityManager.AddComponentObject(sinkEntity, psr);
        });
    }
}
