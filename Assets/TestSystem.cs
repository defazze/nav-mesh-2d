
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TestSystem : ComponentSystem
{
    private float3 _cellSize;
    private int _x;
    private int _y;
    private float3 _transform;

    protected override void OnCreate()
    {
        _cellSize = GameManager.Instance.cellSize;
        _x = GameManager.Instance.cellCount.x;
        _y = GameManager.Instance.cellCount.y;
        _transform = new float3(-1 * (_x / 2) * _cellSize.x, -1 * (_y / 2) * _cellSize.y, 0);
    }

    protected override void OnUpdate()
    {
        var e = GetSingletonEntity<NavMesh>();
        var buffer = EntityManager.GetBuffer<CellPosition>(e);

        var arr = buffer.ToNativeArray(Allocator.Temp);

        for (int i = 0; i < arr.Length; i++)
        {
            var cellCoords = arr[i].value;

            var start = new float3(cellCoords.x * _cellSize.x, cellCoords.y * _cellSize.y, 0) + _transform;
            var end = new float3((cellCoords.x + 1) * _cellSize.x, (cellCoords.y + 1) * _cellSize.y, 0) + _transform;

            Debug.DrawLine(start, end, Color.yellow);

        }
    }
}
