
using Unity.Collections;
using Unity.Entities;

public class SyncSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        var e = GetSingletonEntity<NavMesh>();
        var buffer = EntityManager.GetBuffer<CellPosition>(e);
    }
}
