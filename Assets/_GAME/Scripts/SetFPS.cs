using System.Collections;
using UnityEngine;

public class SetFPS : MonoBehaviour
{
    public int FPS = 300;
    private void Awake()
    {
        
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(.1f);
        Application.targetFrameRate = FPS;
    }
}
