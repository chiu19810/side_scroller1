using UnityEngine;
using System.Collections;

public class FPSManager : MonoBehaviour
{
    private static float fps;
    private float prevTime;
    private int frameCount;

	void Start ()
    {
        fps = 0;
        prevTime = 0;
        frameCount = 0;
	}
	
	void Update ()
    {
        frameCount++;
        float time = Time.realtimeSinceStartup - prevTime;

        if (time >= 0.5f)
        {
            fps = frameCount / time;

            frameCount = 0;
            prevTime = Time.realtimeSinceStartup;
        }
	}

    static public float getFPS()
    {
        return fps;
    }
}
