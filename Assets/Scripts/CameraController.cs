using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject stageManager;
    public GameObject cameraObject;

    private float cameraWidth;
    private float cameraHeight;
    private StageManager stage;
    private GameObject player;

    void Start ()
    {
        stage = stageManager.GetComponent<StageManager>();
    }

    void Update ()
    {
        if (stage.getMap != null)
        {
            cameraWidth = Screen.width * Camera.main.orthographicSize / (Screen.height / 2);
            cameraHeight = Screen.height * Camera.main.orthographicSize / (Screen.height / 2);
            player = stage.GetPlayer;

            Rect cameraView = stage.Data.map.viewRect;
            float x = player.transform.position.x;
            float y = player.transform.position.y;

            x = Mathf.Clamp(x, cameraView.xMin * stage.chipSizeX + cameraWidth / 2, cameraView.xMax * stage.chipSizeX - cameraWidth / 2);
            y = Mathf.Clamp(y, cameraView.yMin * stage.chipSizeY + cameraHeight / 2, cameraView.yMax * stage.chipSizeY - cameraHeight / 2);

            cameraObject.transform.position = new Vector3(x - stage.chipSizeX / 2, y - stage.chipSizeY / 2, cameraObject.transform.position.z);
        }
    }
}
