
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[UpdateAfter(typeof(StepPhysicsWorld))]
[UpdateAfter(typeof(EndFramePhysicsSystem))]
unsafe public class RaycastJobSystem : JobComponentSystem
{
    private BuildPhysicsWorld _physicsWorldSystem;
    private float3 _cellSize;
    private int _x;
    private int _y;
    private float3 _transform;

    [BurstCompile]
    private struct RaycastJob : IJobParallelFor
    {
        [ReadOnly] public PhysicsWorld physicsWorld;
        public float3 cellSize;
        public float3 transform;
        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int2>.ParallelWriter queue;
        public int x;
        public int y;

        public void Execute(int index)
        {
            var position = new int2(index % x, index / x);
            Calculate(position);
        }

        private void Calculate(int2 cellPosition)
        {
            var position = new float3((cellPosition.x + 0.5f) * cellSize.x, (cellPosition.y + 0.5f) * cellSize.y, 0) + transform;
            var start = position - new float3(0, 0, -0.1f);
            var end = position - new float3(0, 0, 0.1f);

            var collisionWorld = physicsWorld.CollisionWorld;
            var raycastInput = new RaycastInput
            {
                Start = start,
                End = end,
                Filter = CollisionFilter.Default
            };

            if (collisionWorld.CastRay(raycastInput))
            {
                queue.Enqueue(cellPosition);
            }

        }
    }

    private struct UnionJob : IJobForEach_B<CellPosition>
    {
        [NativeDisableContainerSafetyRestriction] public NativeQueue<int2> queue;
        public void Execute(DynamicBuffer<CellPosition> buffer)
        {
            buffer.Clear();
            while (queue.TryDequeue(out int2 position))
            {
                buffer.Add(new CellPosition { value = position });
            }
        }
    }

    protected override void OnCreate()
    {
        var e = EntityManager.CreateEntity(typeof(NavMesh));
        EntityManager.AddBuffer<CellPosition>(e);

        _physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();

        _cellSize = GameManager.Instance.cellSize;
        _x = GameManager.Instance.cellCount.x;
        _y = GameManager.Instance.cellCount.y;
        _transform = new float3(-1 * (_x / 2) * _cellSize.x, -1 * (_y / 2) * _cellSize.y, 0);
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var queue = new NativeQueue<int2>(Allocator.TempJob);
        var writer = queue.AsParallelWriter();

        _physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();


        var raycastJob = new RaycastJob
        {
            physicsWorld = _physicsWorldSystem.PhysicsWorld,
            cellSize = _cellSize,
            transform = _transform,
            queue = writer,
            x = _x,
            y = _y
        };

        var handle = raycastJob.Schedule(_x * _y, 64, JobHandle.CombineDependencies(_physicsWorldSystem.FinalJobHandle, inputDeps));

        var unionJob = new UnionJob
        {
            queue = queue
        };

        var jobHandle = unionJob.Schedule(this, handle);
        queue.Dispose();
        return jobHandle;
    }
}
