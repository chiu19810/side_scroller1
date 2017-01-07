using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public GameObject stageManager;
    public GameObject cameraObject;

    private StageManager stage;
    private GameObject player;
    private string[,] map;
    private int mapX;
    private int mapY;
    private float cameraWidth;
    private float cameraHeight;

	void Start ()
    {
        stage = stageManager.GetComponent<StageManager>();
    }

    void Update ()
    {
	    if (stage.getMap != null)
        {
            map = stage.getMap;
            mapX = map.GetLength(1);
            mapY = map.GetLength(0);
            cameraWidth = Screen.width * Camera.main.orthographicSize / (Screen.height / 2);
            cameraHeight = Screen.height * Camera.main.orthographicSize / (Screen.height / 2);
            player = GameObject.Find("Player(Clone)");

            float x = player.transform.position.x;
            float y = player.transform.position.y;
            float stageSizeW = stage.chipSizeX * mapX;
            float stageSizeH = stage.chipSizeY * mapY;

            if (x - cameraWidth / 2 < 0)
            {
                x = cameraWidth / 2;
            }
            else if (x + cameraWidth / 2 > stageSizeW)
            {
                x = stageSizeW - cameraWidth / 2;
            }

            

            cameraObject.transform.position = new Vector3(x, y, cameraObject.transform.position.z);
        }
	}
}
