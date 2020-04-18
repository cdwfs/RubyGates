using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class GateDagSortSystem : SystemBase
{
    private EntityQuery _nodeQuery;

    protected override void OnCreate()
    {
        RequireSingletonForUpdate<DagIsStale>();

        // Buttons don't have GateInput. The final node likely won't have an output. Both should be included in the sort.
        _nodeQuery = GetEntityQuery(new EntityQueryDesc
        {
            Any = new[] {ComponentType.ReadOnly<NodeOutput>(), ComponentType.ReadOnly<NodeInput>()}
        });

    }

    struct SortableNode : IDisposable
    {
        public Entity Entity;
        public NativeList<int> Inputs;
        public NativeList<int> Outputs;

        public void Dispose()
        {
            Inputs.Dispose();
            Outputs.Dispose();
        }
    }

    protected override void OnUpdate()
    {
        Dependency.Complete(); // TODO: is this redundant?

        // Retrieve an array of all the node entities
        var nodeEntities = _nodeQuery.ToEntityArray(Allocator.TempJob);

        // Generate an entity-to-index lookup table, so we can track inter-entity references as
        // list indices while sorting.
        // TODO(cort): NativeHashMap is currently optimized for large datasets with a parallel writer, and is not
        // great for small sets + single reader/writer.
        var entityToIndex = new Dictionary<Entity, int>(nodeEntities.Length);
        for (var i = 0; i < nodeEntities.Length; ++i)
        {
            entityToIndex[nodeEntities[i]] = i;
        }

        var nodeInputs = GetBufferFromEntity<NodeInput>(true);
        var nodesToSort = new List<SortableNode>(nodeEntities.Length);
        var nodeUnsortedInputCounts = new NativeArray<int>(nodeEntities.Length, Allocator.Temp);
        var readyToSortIndices = new NativeList<int>(nodeEntities.Length, Allocator.Temp);
        var nodeDepths = new NativeArray<int>(nodeEntities.Length, Allocator.Temp);
        for (var i = 0; i < nodeEntities.Length; ++i)
        {
            var nodeEntity = nodeEntities[i];
            var sg = new SortableNode
            {
                Entity = nodeEntities[i],
                Inputs = new NativeList<int>(0, Allocator.Temp), // overwritten below if the node has an input list
                // output count could technically go up to nodeEntities.Length, but we'll start capacity at 8 for now to avoid O(N^2) mem usage
                Outputs = new NativeList<int>(8, Allocator.Temp),
            };
            if (nodeInputs.Exists(nodeEntity))
            {
                var inputBuffer = nodeInputs[nodeEntity];
                sg.Inputs.Capacity = inputBuffer.Length;
                for (var j = 0; j < inputBuffer.Length; j++)
                {
                    var input = inputBuffer[j];
                    int inputIndex = entityToIndex[input.InputEntity];
                    sg.Inputs.Add(inputIndex);
                }
            }
            nodesToSort.Add(sg);

            nodeUnsortedInputCounts[i] = nodesToSort[i].Inputs.Length;
            if (nodesToSort[i].Inputs.Length == 0)
            {
                readyToSortIndices.Add(i);
            }

            nodeDepths[i] = -1;
        }

        nodeEntities.Dispose();
        // Loop again to populate output lists
        for (var i = 0; i < nodesToSort.Count; ++i)
        {
            var numInputs = nodesToSort[i].Inputs.Length;
            for (var j = 0; j < numInputs; ++j)
            {
                var inputIndex = nodesToSort[i].Inputs[j];
                nodesToSort[inputIndex].Outputs.Add(i);
            }
        }

        // And now the topological sort.
        // Basically using Kahn's algorithm (https://en.wikipedia.org/wiki/Topological_sort#Kahn's_algorithm)
        // adapted to compute the depth of each output element (https://cs.stackexchange.com/questions/2524/getting-parallel-items-in-dependency-resolution)
        var numToSort = nodesToSort.Count;
        while (readyToSortIndices.Length > 0)
        {
            // Grab the next item from the ready list
            var indexToSort = readyToSortIndices[0];
            readyToSortIndices.RemoveAtSwapBack(0);
            Debug.Assert(nodeUnsortedInputCounts[indexToSort] == 0);
            // Decrement all its outputs
            foreach (var outputIndex in nodesToSort[indexToSort].Outputs)
            {
                Debug.Assert(nodesToSort[outputIndex].Inputs.AsArray().Contains(indexToSort));
                Debug.Assert(nodeUnsortedInputCounts[outputIndex] > 0);
                nodeUnsortedInputCounts[outputIndex] -= 1;
                if (nodeUnsortedInputCounts[outputIndex] == 0)
                {
                    readyToSortIndices.Add(outputIndex);
                }
            }

            // Sort this output. Instead of appending to a sorted output list, take the max of
            // all the depths of our inputs, and add 1.
            Debug.Assert(nodeDepths[indexToSort] == -1);
            var maxInputDepth = -1;
            foreach (var inputIndex in nodesToSort[indexToSort].Inputs)
            {
                Debug.Assert(nodeDepths[inputIndex] >= 0);
                maxInputDepth = math.max(maxInputDepth, nodeDepths[inputIndex]);
            }

            nodeDepths[indexToSort] = maxInputDepth + 1;
            EntityManager.SetSharedComponentData(nodesToSort[indexToSort].Entity,
                new DagDepth {Value = maxInputDepth + 1});
            numToSort -= 1;
        }

        // TODO(cort): Find & report actual nodes that form a cycle
        Debug.Assert(numToSort == 0,
            "Cycle detected while sorting node graph. Cyclic dependencies are not supported.");

        foreach (var node in nodesToSort)
            node.Dispose();
        nodeUnsortedInputCounts.Dispose();
        readyToSortIndices.Dispose();
        nodeDepths.Dispose();

        EntityManager.DestroyEntity(GetSingletonEntity<DagIsStale>());
    }
}