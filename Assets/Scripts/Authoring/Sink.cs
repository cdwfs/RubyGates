using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class Sink : MonoBehaviour
{
    // Just a vehicle for the ParticleSystem at the moment
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
        });
    }
}
