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
            float lef = 0;
            float rig = 0;
            float top = 0;
            float bot = 0;

            if (stage.DirLeft)
            {
                lef = stage.chipSizeX;
            }

            if (stage.DirRight)
            {
                rig = stage.chipSizeX;
            }

            if (stage.DirTop)
            {
                top = stage.chipSizeX;
            }

            if (stage.DirBottom)
            {
                bot = stage.chipSizeX;
            }

            if (x - cameraWidth / 2 + stage.chipSizeX / 2 - lef < 0)
                x = cameraWidth / 2 - stage.chipSizeX / 2 + lef;
            else if (x + cameraWidth / 2 + stage.chipSizeX / 2 + rig > stageSizeW)
                x = stageSizeW - cameraWidth / 2 - stage.chipSizeX / 2 - rig;

            if (y - cameraHeight / 2 + stage.chipSizeY / 2 - bot < 0)
                y = cameraHeight / 2 - stage.chipSizeY / 2 + bot;
            else if (y + cameraHeight / 2 + stage.chipSizeY / 2 + bot > stageSizeH)
                y = stageSizeH - cameraHeight / 2 - stage.chipSizeY / 2 - bot;

            if (cameraWidth > stageSizeW && cameraHeight > stageSizeH - bot)
            {
                cameraObject.transform.position = new Vector3(stageSizeW / 2, (stageSizeH - bot / 2) / 2, cameraObject.transform.position.z);
            }
            else if (cameraWidth > stageSizeW)
            {
                cameraObject.transform.position = new Vector3(stageSizeW / 2, y, cameraObject.transform.position.z);
            }
            else if (cameraHeight > stageSizeH - bot)
            {
                cameraObject.transform.position = new Vector3(x, (stageSizeH - bot / 2) / 2, cameraObject.transform.position.z);
            }
            else
            {
                cameraObject.transform.position = new Vector3(x, y, cameraObject.transform.position.z);
            }
        }
	}
}
