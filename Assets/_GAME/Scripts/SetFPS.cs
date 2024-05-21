using UnityEngine;

public class SetFPS : MonoBehaviour
{
    public int FPS = 300;
    private void Awake()
    {
        
    }

    private void Start()
    {
        Application.targetFrameRate = FPS;
    }
}
