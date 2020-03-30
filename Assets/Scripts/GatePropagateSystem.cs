using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[UpdateAfter(typeof(GateDagSortSystem))]
public class GatePropagateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .WithoutBurst()
            .ForEach((ref GateOutput output, in DynamicBuffer<GateInput> inputs, in GateTypeComponent gateType) =>
                {
                    output.value += 1;
                }).Run();
    }
}
