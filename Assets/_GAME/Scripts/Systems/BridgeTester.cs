using System.Collections;
using UnityEngine;

public class BridgeTester : MonoBehaviour
{
    public new MeshCollider collider;
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(.1f);
        var data = new MapGenOutput
        {
            Height = 0,
            Width = 0,
            HoleLayer = new int[] { 0 },
            Layer = new int[] { 0 },
            GroundMesh = collider.sharedMesh
        };

        ToEntities.SendMapData(data);
    }
}
