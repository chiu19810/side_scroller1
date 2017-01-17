using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    public Camera cameraObject;
    public string startMap = "";

    public float chipSizeX = -1;
    public float chipSizeY = -1;

    private GameObject PlayerInstance;
    private GameObject player;
    private GameObject stage;
    private float playerX;
    private float playerY;
    private bool dirLeft;
    private bool dirRight;
    private bool dirTop;
    private bool dirBottom;
    private string stagePath = "Map/Stages/";
    private string bgPath = "Map/Backgrounds/";
    private string eventPath = "Map/Events/";
    private string soundPath = "Sounds/BGM/";
    private string[,] map;
    private string nowStageName;
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
        float x = -chipSizeX / 2;
        float y = -chipSizeY / 2;
        int numX = 1;
        int numY = 1;

        if ((map = OpenMapFile(path)) == null)
            return;

        nowStageName = path;

        // 背景ロード
        MapBackgroundData data = OpenMbgFile(path);
        cameraObject.backgroundColor = data.backcolor;
        Texture2D tex = Resources.Load(data.background.Replace("Assets/Resources/", "").Split('.')[0]) as Texture2D;

        Destroy(stage);
        stage = new GameObject("Stage");

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
                x = -chipSizeX / 2;
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
                string[] eves = map[i, j].Split('#');
                string[] stas = eves[0].Split('|');
                string prePath = stas[0];

                if (prePath != null && prePath != "")
                {
                    if (prePath.IndexOf("Player") > -1)
                    {
                        startPos = new Vector2(x, y);
                        PlayerInstance.transform.position = startPos;
                    }
                    else
                    {
                        GameObject prefab = (GameObject)Resources.Load(prePath);
                        if (prefab != null)
                        {
                            GameObject ins = Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity) as GameObject;
                            ins.transform.parent = stage.transform;

                            if (prePath.IndexOf("AreaChange") > -1)
                                ins.name = stas[1];
                        }

                        if (eves.Length > 1)
                        {
                            int eventID = int.Parse(eves[1].Split('|')[1].Split(':')[1]);
                            prefab = (GameObject)Resources.Load(eves[1].Split('|')[0]);
                            if (prefab != null)
                            {
                                GameObject ins = Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity) as GameObject;
                                ins.transform.parent = stage.transform;

                                EventManager em = ins.GetComponent<EventManager>();
                                em.eventID = eventID;
                            }
                        }
                    }
                }

                x += chipSizeX;
            }

            y += chipSizeY;
        }
    }

    private string[,] OpenMapFile(string path)
    {
        path = stagePath + path;

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
                    dirLeft = true;
                else if (dir[i].IndexOf("Right") > -1)
                    dirRight = true;
                else if (dir[i].IndexOf("Top") > -1)
                    dirTop = true;
                else if (dir[i].IndexOf("Bottom") > -1)
                    dirBottom = true;
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
                    map[i, j] = "Player";
                else if (text[i].Split(',')[j].IndexOf("areachange") > -1)
                    map[i, j] = "Prefabs/AreaChange|" + text[i].Split(',')[j].Split('|')[1].Split(':')[0] + ":" + text[i].Split(',')[j].Split('|')[1];
                else if (text[i].Split(',')[j].Split('|')[0].IndexOf("event") > -1)
                {
                    if (text[i].Split(',')[j].Split('|')[1].Split(':')[3] == "0")
                        map[i, j] = "Prefabs/EventPrefab|" + text[i].Split(',')[j].Split('|')[1].Split(':')[0] + ":" + text[i].Split(',')[j].Split('|')[1];
                    else if (text[i].Split(',')[j].Split('|')[1].Split(':')[3] == "1")
                        map[i, j] = "Prefabs/EventTopPrefab|" + text[i].Split(',')[j].Split('|')[1].Split(':')[0] + ":" + text[i].Split(',')[j].Split('|')[1];
                    else if (text[i].Split(',')[j].Split('|')[1].Split(':')[3] == "2")
                        map[i, j] = "Prefabs/EventBottomPrefab|" + text[i].Split(',')[j].Split('|')[1].Split(':')[0] + ":" + text[i].Split(',')[j].Split('|')[1];
                    else if (text[i].Split(',')[j].Split('|')[1].Split(':')[3] == "3")
                        map[i, j] = "Prefabs/EventLeftPrefab|" + text[i].Split(',')[j].Split('|')[1].Split(':')[0] + ":" + text[i].Split(',')[j].Split('|')[1];
                    else if (text[i].Split(',')[j].Split('|')[1].Split(':')[3] == "4")
                        map[i, j] = "Prefabs/EventRightPrefab|" + text[i].Split(',')[j].Split('|')[1].Split(':')[0] + ":" + text[i].Split(',')[j].Split('|')[1];
                }
                else if (text[i].Split(',')[j].IndexOf("MapChip") > -1)
                    map[i, j] = "Prefabs/MapChip/" + text[i].Split(',')[j].Split('|')[0];
                else if (text[i].Split(',')[j].IndexOf("MapObject") > -1)
                    map[i, j] = "Prefabs/MapObject/" + text[i].Split(',')[j].Split('|')[0];
                else
                    map[i, j] = "";

                if (text[i].Split(',')[j].Split('#').Length > 1)
                {
                    if (text[i].Split(',')[j].Split('#')[1].IndexOf("event") > -1)
                    {
                        if (text[i].Split(',')[j].Split('#')[1].Split('|')[1].Split(':')[3] == "0")
                            map[i, j] = map[i, j].Split('#')[0] + "#Prefabs/EventPrefab|" + text[i].Split(',')[j].Split('#')[1].Split('|')[1];
                        else if (text[i].Split(',')[j].Split('#')[1].Split('|')[1].Split(':')[3] == "1")
                            map[i, j] = map[i, j].Split('#')[0] + "#Prefabs/EventTopPrefab|" + text[i].Split(',')[j].Split('#')[1].Split('|')[1];
                        else if (text[i].Split(',')[j].Split('#')[1].Split('|')[1].Split(':')[3] == "2")
                            map[i, j] = map[i, j].Split('#')[0] + "#Prefabs/EventBottomPrefab|" + text[i].Split(',')[j].Split('#')[1].Split('|')[1];
                        else if (text[i].Split(',')[j].Split('#')[1].Split('|')[1].Split(':')[3] == "3")
                            map[i, j] = map[i, j].Split('#')[0] + "#Prefabs/EventLeftPrefab|" + text[i].Split(',')[j].Split('#')[1].Split('|')[1];
                        else if (text[i].Split(',')[j].Split('#')[1].Split('|')[1].Split(':')[3] == "4")
                            map[i, j] = map[i, j].Split('#')[0] + "#Prefabs/EventRightPrefab|" + text[i].Split(',')[j].Split('#')[1].Split('|')[1];
                    }
                }
            }
        }

        return map;
    }

    private MapBackgroundData OpenMbgFile(string path)
    {
        path = bgPath + path;

        TextAsset ta = Resources.Load(path) as TextAsset;
        if (ta == null)
            return new MapBackgroundData();

        MapBackgroundData data = JsonUtility.FromJson<MapBackgroundData>(ta.text);
        return data;
    }

    public MapEventData OpenEventFile(string path)
    {
        path = eventPath + path;

        TextAsset ta = Resources.Load(path) as TextAsset;
        if (ta == null)
            return new MapEventData();

        MapEventData data = JsonUtility.FromJson<MapEventData>(ta.text);
        return data;
    }


    private void OpenBGMFile(string path)
    {
        path = soundPath + path;

        AudioClip clip = Resources.Load(path) as AudioClip;
        Destroy(audioSource);
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = 0.6f;
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

    public string getNowStageName
    {
        get { return nowStageName; }
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
    public int objectSize = 1;
    public FoldOut[] foldouts;
}

[System.Serializable]
public class EventFold
{
    public bool select = false;
    public List<string> command = new List<string>();
    public int select_command = 0;
}

[System.Serializable]
public class MapEventData
{
    public int eventSize = 1;
    public EventFold[] eventFold;
}