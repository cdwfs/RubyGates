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
            AddHybridComponent(ps);
            AddHybridComponent(psr);

            // Every scene needs a Sink, so it can drive the DAG sorting.
            var sinkEntity = GetPrimaryEntity(sink);
            DstEntityManager.AddComponent<DagIsStale>(sinkEntity);
        });
    }
}
