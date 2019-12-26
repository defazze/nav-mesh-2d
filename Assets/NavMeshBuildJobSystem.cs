
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[UpdateAfter(typeof(StepPhysicsWorld))]
[UpdateAfter(typeof(EndFramePhysicsSystem))]
unsafe public class NavMeshBuildJobSystem : JobComponentSystem
{
    private BuildPhysicsWorld _physicsWorldSystem;
    private float3 _cellSize;
    private int _x;
    private int _y;
    private float3 _transform;

    [BurstCompile]
    private struct NavMeshCalculator : IJobForEach_B<CellPosition>
    {
        [ReadOnly] public PhysicsWorld physicsWorld;
        public float3 cellSize;
        public float3 transform;
        public int x;
        public int y;

        [NativeDisableParallelForRestriction]
        private DynamicBuffer<CellPosition> _buffer;

        public void Execute(DynamicBuffer<CellPosition> buffer)
        {
            _buffer = buffer;
            _buffer.Clear();
            Calculate(0, x - 1, 0, y - 1);
        }

        private void Calculate(int xMin, int xMax, int yMin, int yMax)
        {
            var xCount = xMax - xMin + 1;
            var yCount = yMax - yMin + 1;
            var xExt = xCount / 2;
            var yExt = yCount / 2;

            var collisionWorld = physicsWorld.CollisionWorld;

            BoxGeometry boxGeometry = new BoxGeometry();
            boxGeometry.Center = Vector3.zero;
            boxGeometry.Orientation = Quaternion.identity;
            boxGeometry.Size = new float3(xCount * cellSize.x, yCount * cellSize.y, .01f);
            boxGeometry.BevelRadius = 0;

            var collider = Unity.Physics.BoxCollider.Create(boxGeometry, CollisionFilter.Default);

            var colliderInput = new ColliderDistanceInput
            {
                Collider = (Unity.Physics.Collider*)collider.GetUnsafePtr(),
                Transform = new RigidTransform(Quaternion.identity, new float3((xMin + xExt) * cellSize.x, (yMin + yExt) * cellSize.y, 0) + transform),
                MaxDistance = -.05f
            };

            bool res = collisionWorld.CalculateDistance(colliderInput);

            collider.Dispose();

            if (!res)
            {
                return;
            }

            if (xCount == 1 && yCount == 1)
            {
                _buffer.Add(new CellPosition { value = new int2(xMax, yMax) });
                return;
            }

            if (xCount == 1)
            {
                Calculate(xMin, xMax, yMin, yMax - yExt);
                Calculate(xMin, xMax, yMin + yExt, yMax);
            }
            else if (yCount == 1)
            {
                Calculate(xMin, xMax - xExt, yMin, yMax);
                Calculate(xMin + xExt, xMax, yMin, yMax);
            }
            else
            {
                Calculate(xMin, xMax - xExt, yMin, yMax - yExt);
                Calculate(xMin + xExt, xMax, yMin, yMax - yExt);
                Calculate(xMin, xMax - xExt, yMin + yExt, yMax);
                Calculate(xMin + xExt, xMax, yMin + yExt, yMax);
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
        var calculatorJob = new NavMeshCalculator
        {
            physicsWorld = _physicsWorldSystem.PhysicsWorld,
            cellSize = _cellSize,
            transform = _transform,
            x = _x,
            y = _y
        };

        _physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
        var jobHandle = calculatorJob.Schedule(this, JobHandle.CombineDependencies(inputDeps, _physicsWorldSystem.FinalJobHandle));

        return jobHandle;
    }
}
