// This system implements a custom command-buffer-style feature for enqueueing material change
// requests in parallel and playing them back on the main thread. Unlike an
// EntityCommandBuffer, it is *not* deterministic.

using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;

public struct MaterialChange
{
    public Entity entity;
    public int materialIndex;
}

[UpdateAfter(typeof(WireSystem))]
public class ChangeMaterialSystem : SystemBase
{
    private NativeQueue<MaterialChange> _changeQueue;
    public NativeQueue<MaterialChange>.ParallelWriter ChangeQueueParallelWriter => _changeQueue.AsParallelWriter();

    private JobHandle _producerHandle;
    public void AddJobHandleForProducer(JobHandle handle)
    {
        _producerHandle = JobHandle.CombineDependencies(_producerHandle, handle);
    }

    protected override void OnCreate() {
        _changeQueue = new NativeQueue<MaterialChange>(Allocator.Persistent);
    }

    protected override void OnDestroy() {
        _producerHandle.Complete();
        _changeQueue.Dispose();
    }

    protected override void OnUpdate()
    {
        _producerHandle.Complete();
        _producerHandle = new JobHandle();

        while (_changeQueue.TryDequeue(out MaterialChange change))
        {
            var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(change.entity);
            if (EntityManager.HasComponent<WireMaterials>(change.entity)) {
                var wireMaterials = EntityManager.GetSharedComponentData<WireMaterials>(change.entity);
                renderMesh.material = (change.materialIndex == 0)
                    ? wireMaterials.OffMaterial
                    : wireMaterials.OnMaterial;
            }
            EntityManager.SetSharedComponentData(change.entity, renderMesh);
        }
    }
}