
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[BurstCompile]
[DisableAutoCreation]
public class NavMeshBuildSystem : ComponentSystem
{
    private float3 _cellSize;
    private int _x;
    private int _y;
    private float3 _transform;

    protected override void OnCreate()
    {
        var e = EntityManager.CreateEntity(typeof(NavMesh));
        EntityManager.AddBuffer<CellPosition>(e);

        _cellSize = GameManager.Instance.cellSize;
        _x = GameManager.Instance.cellCount.x;
        _y = GameManager.Instance.cellCount.y;
        _transform = new float3(-1 * (_x / 2) * _cellSize.x, -1 * (_y / 2) * _cellSize.y, 0);
    }

    protected override void OnUpdate()
    {
        var e = GetSingletonEntity<NavMesh>();
        var buffer = EntityManager.GetBuffer<CellPosition>(e);
        buffer.Clear();

        var result = new NativeList<int2>(Allocator.Temp);
        CalculateAabbs(0, _x - 1, -0, _y - 1, ref result);
    }

    private void CalculateAabbs(int xMin, int xMax, int yMin, int yMax, ref NativeList<int2> result)
    {
        var xCount = xMax - xMin + 1;
        var yCount = yMax - yMin + 1;
        var xExt = xCount / 2;
        var yExt = yCount / 2;

        var physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
        var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;

        var hits = new NativeList<int>(Allocator.Temp);

        var overlapInput = new OverlapAabbInput
        {
            Aabb = new Aabb
            {
                Min = new float3(xMin, yMin, -1) * _cellSize + _transform,
                Max = new float3(xMax + 1, yMax + 1, 1f) * _cellSize + _transform
            },
            Filter = CollisionFilter.Default
        };
        bool res = collisionWorld.OverlapAabb(overlapInput, ref hits);

        if (!res)
        {
            return;
        }

        if (xCount == 1 && yCount == 1)
        {
            var e = GetSingletonEntity<NavMesh>();
            var buffer = EntityManager.GetBuffer<CellPosition>(e);
            buffer.Add(new CellPosition { value = new int2(xMax, yMax) });
            //result.Add(new int2(xMax, yMax));
            return;
        }

        if (xCount == 1)
        {
            CalculateAabbs(xMin, xMax, yMin, yMax - yExt, ref result);
            CalculateAabbs(xMin, xMax, yMin + yExt, yMax, ref result);
        }
        else if (yCount == 1)
        {
            CalculateAabbs(xMin, xMax - xExt, yMin, yMax, ref result);
            CalculateAabbs(xMin + xExt, xMax, yMin, yMax, ref result);
        }
        else
        {
            CalculateAabbs(xMin, xMax - xExt, yMin, yMax - yExt, ref result);
            CalculateAabbs(xMin + xExt, xMax, yMin, yMax - yExt, ref result);
            CalculateAabbs(xMin, xMax - xExt, yMin + yExt, yMax, ref result);
            CalculateAabbs(xMin + xExt, xMax, yMin + yExt, yMax, ref result);
        }
    }
}

