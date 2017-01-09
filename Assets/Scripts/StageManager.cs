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
    private float playerX;
    private float playerY;
    private string[,] map;

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

        map = OpenMapFile(path);
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
                        PlayerInstance.transform.position = new Vector2(x + chipSizeX / 2, y + chipSizeY / 2);
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
        path = "Assets/Resources/Map/" + path + ".map";

        if (!string.IsNullOrEmpty(path))
        {
            string text = "";
            int sizeX = 0;
            int sizeY = 0;
            int mapSizeX = -1;
            int mapSizeY = -1;

            StreamReader sr = new StreamReader(path, System.Text.Encoding.Default);

            while (sr.Peek() >= 0)
            {
                string sb = sr.ReadLine();
                if (sb != "")
                {
                    text += sb + "!";
                    mapSizeX = sizeX;
                    sizeY++;
                }

                string[] mapLine = sb.Split(',');
                sizeX = mapLine.Length;
            }
            sr.Close();

            mapSizeY = sizeY;
            string[,] map = new string[mapSizeY, mapSizeX];
            for (int i = 0; i < mapSizeY; i++)
            {
                for (int j = 0; j < mapSizeX; j++)
                {
                    if (text.Split('!')[i].Split(',')[j].IndexOf("start") > -1)
                        map[i, j] = "Player";
                    else if (text.Split('!')[i].Split(',')[j].IndexOf("areachange") > -1)
                        map[i, j] = "Prefabs/AreaChange|" + text.Split('!')[i].Split(',')[j].Split('|')[1].Split(':')[0] + ":" + text.Split('!')[i].Split(',')[j].Split('|')[1].Split(':')[1] + ":" + text.Split('!')[i].Split(',')[j].Split('|')[1].Split(':')[2];
                    else if (text.Split('!')[i].Split(',')[j].IndexOf("MapChip") > -1)
                        map[i, j] = "Prefabs/MapChip/" + text.Split('!')[i].Split(',')[j].Split('|')[0];
                    else if (text.Split('!')[i].Split(',')[j].IndexOf("MapObject") > -1)
                        map[i, j] = "Prefabs/MapObject/" + text.Split('!')[i].Split(',')[j].Split('|')[0];
                    else
                        map[i, j] = "";
                }
            }

            return map;
        }

        return null;
    }

    private MapBackgroundData OpenMbgFile(string path)
    {
        path = "Assets/Resources/Map/Backgrounds/" + path + ".mbg";

        if (System.IO.File.Exists(path))
        {
            StreamReader sr = new StreamReader(path, System.Text.Encoding.Default);
            MapBackgroundData data = JsonUtility.FromJson<MapBackgroundData>(sr.ReadToEnd());

            return data;
        }

        return new MapBackgroundData();
    }

    public GameObject GetPlayer
    {
        get { return PlayerInstance; }
    }

    public string[,] getMap
    {
        get { return map; }
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