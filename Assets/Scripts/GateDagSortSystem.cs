using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


public class GateDagSortSystem : SystemBase
{
    private EntityQuery gateQuery;
    
    protected override void OnCreate()
    {
        // TODO: is this supposed to be called in OnCreate or OnUpdate?
        RequireSingletonForUpdate<DagIsStale>();
        
        gateQuery = GetEntityQuery(
            ComponentType.ReadOnly<GateOutput>());
    }

    struct SortableGate : IDisposable
    {
        public Entity entity;
        public NativeList<int> inputs;
        public NativeList<int> outputs;

        public void Dispose()
        {
            inputs.Dispose();
            outputs.Dispose();
        }
    }

    protected override void OnUpdate()
    {
        // Retrieve an array of all the gate nodes and their inputs
        var gateEntities = gateQuery.ToEntityArray(Allocator.TempJob);

        // Generate an entity-to-index lookup table, so we can track inter-entity references as
        // list indices while sorting.
        // TODO(cort): NativeHashMap is currently optimized for large datasets with a parallel writer, and is not
        // great for small sets + single reader/writer.
        var entityToIndex = new Dictionary<Entity, int>(gateEntities.Length);
        for (int i = 0; i < gateEntities.Length; ++i)
        {
            entityToIndex[gateEntities[i]] = i;
        }

        var gateInputs = GetBufferFromEntity<GateInput>(true);
        var gatesToSort = new List<SortableGate>(gateEntities.Length);
        var gateUnsortedInputCounts = new NativeArray<int>(gateEntities.Length, Allocator.Temp);
        var readyToSortIndices = new NativeList<int>(gateEntities.Length, Allocator.Temp);
        var gateDepths = new NativeArray<int>(gateEntities.Length, Allocator.Temp);
        for (int i = 0; i < gateEntities.Length; ++i)
        {
            var ent = gateEntities[i];
            var inputs = gateInputs[ent];
            var sg = new SortableGate
            {
                entity = gateEntities[i],
                inputs = new NativeList<int>(inputs.Length, Allocator.Temp),
                // output count could technically go up to gateEntities.Length, but we'll start capacity at 8 for now to avoid O(N^2) mem usage
                outputs = new NativeList<int>(8, Allocator.Temp),
            };
            foreach (var input in inputs)
            {
                int inputIndex = entityToIndex[input.inputEntity];
                sg.inputs.Add(inputIndex);
            }
            gatesToSort.Add(sg);

            gateUnsortedInputCounts[i] = gatesToSort[i].inputs.Length;
            if (gatesToSort[i].inputs.Length == 0)
            {
                readyToSortIndices.Add(i);
            }
            gateDepths[i] = -1;
        }
        gateEntities.Dispose();
        // Loop again to populate output lists
        for (int i = 0; i < gatesToSort.Count; ++i)
        {
            foreach (var input in gatesToSort[i].inputs)
            {
                gatesToSort[input].outputs.Add(i);
            }
        }

        // And now the topological sort.
        // Basically using Kahn's algorithm (https://en.wikipedia.org/wiki/Topological_sort#Kahn's_algorithm)
        // adapted to compute the depth of each output element (https://cs.stackexchange.com/questions/2524/getting-parallel-items-in-dependency-resolution)
        int numToSort = gatesToSort.Count;
        while (readyToSortIndices.Length > 0)
        {
            // Grab the next item from the ready list
            int indexToSort = readyToSortIndices[0];
            readyToSortIndices.RemoveAtSwapBack(0);
            Debug.Assert(gateUnsortedInputCounts[indexToSort] == 0);
            // Decrement all its outputs
            foreach (var outputIndex in gatesToSort[indexToSort].outputs)
            {
                Debug.Assert(gatesToSort[outputIndex].inputs.AsArray().Contains(indexToSort));
                Debug.Assert(gateUnsortedInputCounts[outputIndex] > 0);
                gateUnsortedInputCounts[outputIndex] -= 1;
                if (gateUnsortedInputCounts[outputIndex] == 0)
                {
                    readyToSortIndices.Add(outputIndex);
                }
            }
            // Sort this output. Instead of appending to a sorted output list, take the max of
            // all the depths of our inputs, and add 1.
            Debug.Assert(gateDepths[indexToSort] == -1);
            int maxInputDepth = -1;
            foreach (var inputIndex in gatesToSort[indexToSort].inputs)
            {
                Debug.Assert(gateDepths[inputIndex] >= 0);
                maxInputDepth = math.max(maxInputDepth, gateDepths[inputIndex]);
            }
            gateDepths[indexToSort] = maxInputDepth + 1;
            EntityManager.SetSharedComponentData(gatesToSort[indexToSort].entity,
                new GateDagDepth {value = maxInputDepth + 1});
            numToSort -= 1;
        }

        // TODO(cort): Find & report actual nodes that form a cycle
        Debug.Assert(numToSort == 0,
            "Cycle detected while sorting node graph. Cyclic dependencies are not supported.");

        foreach (var gate in gatesToSort)
            gate.Dispose();
        gateUnsortedInputCounts.Dispose();
        readyToSortIndices.Dispose();
        gateDepths.Dispose();

        EntityManager.DestroyEntity(GetSingletonEntity<DagIsStale>());
    }
}