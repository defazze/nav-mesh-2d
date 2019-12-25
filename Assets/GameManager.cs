using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class GameManager : MonoBehaviour
{
    public int2 cellCount;
    public float3 cellSize;

    public Mesh mesh;
    public Material Material;
    public static GameManager Instance;
    public GameManager()
    {
        Instance = this;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(cellCount.x * cellSize.x, cellCount.y * cellSize.y, 1));
    }
}
