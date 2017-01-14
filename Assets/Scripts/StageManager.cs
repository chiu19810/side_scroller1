using UnityEngine;
using System.Collections;
using System.IO;

public class StageManager : MonoBehaviour
{
    public Camera cameraObject;
    public string startMap = "";

    public float chipSizeX = -1;
    public float chipSizeY = -1;

    private GameObject PlayerInstance;
    private GameObject player;
    private float playerX;
    private float playerY;
    private bool dirLeft;
    private bool dirRight;
    private bool dirTop;
    private bool dirBottom;
    private string[,] map;
    private Vector2 startPos = Vector2.zero;
    private AudioSource audioSource;

    void Start ()
    {
        if (startMap == "")
            Debug.Log("startMapが設定されていません！");
        if (chipSizeX == -1)
            Debug.Log("chipSizeXが設定されていません！");
        if (chipSizeY == -1)
            Debug.Log("chipSizeYが設定されていません！");

        GameObject prefab = (GameObject)Resources.Load("Prefabs/Player");
        PlayerInstance = Instantiate(prefab, new Vector3(-100, -100, 0), Quaternion.identity) as GameObject;
        StageInit(startMap);
    }

    public void StageInit(string path)
    {
        float x = 0;
        float y = -0.64f;
        int numX = 1;
        int numY = 1;

        if ((map = OpenMapFile(path)) == null)
            return;

        // 背景ロード
        MapBackgroundData data = OpenMbgFile(path);
        cameraObject.backgroundColor = data.backcolor;
        Texture2D tex = Resources.Load(data.background.Replace("Assets/Resources/", "").Split('.')[0]) as Texture2D;

        GameObject stage = GameObject.Find("Stage");
        foreach (Transform n in stage.transform)
        {
            GameObject.Destroy(n.gameObject);
        }

        GameObject background = new GameObject("Background");
        background.transform.parent = stage.transform;

        if (tex != null)
        {
            if (data.loopXFlag && data.loopYFlag)
            {
                numX = (int)(chipSizeX * 100 * map.GetLength(1) / tex.width) + 1;
                numY = (int)(chipSizeY * 100 * map.GetLength(0) / tex.height) + 1;
            }
            else if (data.loopXFlag)
            {
                numX = (int)(chipSizeX * 100 * map.GetLength(1) / tex.width) + 1;
            }
            else if (data.loopYFlag)
            {
                numY = (int)(chipSizeY * 100 * map.GetLength(0) / tex.height) + 1;
            }

            GameObject prefab = (GameObject)Resources.Load("Prefabs/ImagePrefab");

            for (int yy = 0; yy < numY; yy++)
            {
                x = 0;
                for (int xx = 0; xx < numX; xx++)
                {
                    GameObject ins = Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity) as GameObject;
                    ins.transform.GetComponent<SpriteRenderer>().sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
                    ins.transform.GetComponent<SpriteRenderer>().sortingOrder = -100;
                    ins.transform.parent = background.transform;

                    x += tex.width / 100;
                }
                y -= tex.height / 100;
            }
        }

        // BGMロード
        OpenBGMFile(path);

        // ステージロード
        player = GameObject.Find("Player(Clone)");
        x = 0;
        y = 0;

        for (int i = map.GetLength(0) - 1; i >= 0; i--)
        {
            x = 0;
            for (int j = 0; j < map.GetLength(1); j++)
            {
                string[] stas = map[i, j].Split('|');
                string prePath = stas[0];
                if (prePath != null && prePath != "")
                {
                    if (prePath.IndexOf("Player") > -1)
                    {
                        startPos = new Vector2(x + player.GetComponent<PlayerController>().getPlayerW / 2, y - player.GetComponent<PlayerController>().getPlayerH);
                        PlayerInstance.transform.position = startPos;
                    }
                    else
                    {
                        GameObject prefab = (GameObject)Resources.Load(prePath);
                        GameObject ins = Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity) as GameObject;
                        ins.transform.parent = stage.transform;

                        if (prePath.IndexOf("AreaChange") > -1)
                            ins.name = stas[1];
                    }
                }

                x += chipSizeX;
            }

            y += chipSizeY;
        }
    }

    private string[,] OpenMapFile(string path)
    {
        path = "Map/" + path;

        TextAsset ta = Resources.Load(path) as TextAsset;
        if (ta == null)
            return null;

        dirLeft = false;
        dirRight = false;
        dirTop = false;
        dirBottom = false;

        if (ta.text.Split('?').Length > 1)
        {
            string[] dir = ta.text.Split('?')[1].Split(':');

            for (int i = 0; i < dir.Length; i++)
            {
                if (dir[i].IndexOf("Left") > -1)
                {
                    dirLeft = true;
                }
                else if (dir[i].IndexOf("Right") > -1)
                {
                    dirRight = true;
                }
                else if (dir[i].IndexOf("Top") > -1)
                {
                    dirTop = true;
                }
                else if (dir[i].IndexOf("Bottom") > -1)
                {
                    dirBottom = true;
                }
            }
        }

        string[] text = ta.text.Split("\n"[0]);
        int sizeY = 0;
        int mapSizeX = -1;
        int mapSizeY = -1;
        mapSizeX = text[0].Split(',').Length;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i].TrimEnd().Replace("\n", "").Replace("\r", "") != "") sizeY++;
        }

        player = GameObject.Find("Player(Clone)");
        mapSizeY = sizeY;
        string[,] map = new string[mapSizeY, mapSizeX];
        for (int i = 0; i < mapSizeY; i++)
        {
            for (int j = 0; j < mapSizeX; j++)
            {
                if (text[i].Split(',')[j].IndexOf("start") > -1)
                {
                    map[i, j] = "Player";
                }
                else if (text[i].Split(',')[j].IndexOf("areachange") > -1)
                    map[i, j] = "Prefabs/AreaChange|" + text[i].Split(',')[j].Split('|')[1].Split(':')[0] + ":" + text[i].Split(',')[j].Split('|')[1].Split(':')[1] + ":" + text[i].Split(',')[j].Split('|')[1].Split(':')[2];
                else if (text[i].Split(',')[j].IndexOf("MapChip") > -1)
                    map[i, j] = "Prefabs/MapChip/" + text[i].Split(',')[j].Split('|')[0];
                else if (text[i].Split(',')[j].IndexOf("MapObject") > -1)
                    map[i, j] = "Prefabs/MapObject/" + text[i].Split(',')[j].Split('|')[0];
                else
                    map[i, j] = "";
            }
        }

        return map;
    }

    private MapBackgroundData OpenMbgFile(string path)
    {
        path = "Map/Backgrounds/" + path;

        TextAsset ta = Resources.Load(path) as TextAsset;
        if (ta == null)
            return new MapBackgroundData();

        MapBackgroundData data = JsonUtility.FromJson<MapBackgroundData>(ta.text);
        return data;
    }

    private void OpenBGMFile(string path)
    {
        path = "Sounds/BGM/" + path;

        AudioClip clip = Resources.Load(path) as AudioClip;
        Destroy(audioSource);
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.Play();
    }

    public GameObject GetPlayer
    {
        get { return PlayerInstance; }
    }

    public string[,] getMap
    {
        get { return map; }
    }

    public Vector2 getStartPos
    {
        get { return startPos; }
    }

    public bool DirLeft
    {
        get { return dirLeft; }
    }

    public bool DirRight
    {
        get { return dirRight; }
    }

    public bool DirTop
    {
        get { return dirTop; }
    }

    public bool DirBottom
    {
        get { return dirBottom; }
    }
}

[System.Serializable]
public class FoldOut
{
    public bool foldout = false;
    public bool objectIsX = false;
    public bool objectIsY = false;
    public float objectX = 0;
    public float objectY = 0;
    public float objectLoopX = 0;
    public float objectLoopY = 0;
    public string obj = "";
}

[System.Serializable]
public class MapBackgroundData
{
    public int mode = 0;
    public string background = "";
    public Color backcolor = new Color(119f / 255f, 211f / 255f, 255f / 255f);
    public bool loopXFlag = false;
    public bool loopYFlag = false;
    public int objectSize = 0;
    public FoldOut[] foldouts;
}