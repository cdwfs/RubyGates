using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class Gate : MonoBehaviour
{
    public GateType gateType;
}

[UpdateInGroup(typeof(GameObjectConversionGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
public class GateConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Gate gate) =>
        {
            var gateEntity = GetPrimaryEntity(gate);

            DstEntityManager.AddComponents(gateEntity, new ComponentTypes(new ComponentType[]
            {
                typeof(GateInfo),
                typeof(NodeOutput),
                typeof(DagDepth),
            }));

            DstEntityManager.SetComponentData(gateEntity, new GateInfo {Type = gate.gateType});
        });
    }
}
