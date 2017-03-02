using UnityEngine;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    private float playerX;
    private float playerY;
    private string[,] map;
    private string stagePath = "Map/Stages/";
    private string soundPath = "Sounds/BGM/";
    private string nowStageName;
    private Vector2 startPos = Vector2.zero;
    private GameObject PlayerInstance;
    private GameObject stages;
    private MapSaveData data = new MapSaveData();
    private VariableManager var = new VariableManager();

    private List<string> flgVarNames = new List<string>();
    private List<string> intVarNames = new List<string>();
    private List<string> strVarNames = new List<string>();

    // ローカル変数
    private FlgVarData[] flgVar = new FlgVarData[10];
    private IntVarData[] intVar = new IntVarData[10];
    private StrVarData[] strVar = new StrVarData[10];

    public Camera cameraObject;
    public string startMap;
    public float chipSizeX; // 0.64
    public float chipSizeY; // 0.64

    void Start()
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/Player");
        PlayerInstance = Instantiate(prefab, new Vector3(-100, -100, 0), Quaternion.identity) as GameObject;
        StageInit(startMap);

        for (int i = 0; i < 10; i++)
        {
            flgVar[i] = new FlgVarData();
            intVar[i] = new IntVarData();
            strVar[i] = new StrVarData();

            flgVar[i].name = "" + i;
            flgVar[i].var = false;

            intVar[i].name = "" + i;
            intVar[i].var = 0;

            strVar[i].name = "" + i;
            strVar[i].var = "";
        }

        VarLoad();
        for (int i = 0; i < 10; i++)
        {
            flgVarNames.Add("ローカルフラグ変数 " + flgVar[i].name);
            intVarNames.Add("ローカル整数変数 " + intVar[i].name);
            strVarNames.Add("ローカル文字列変数 " + strVar[i].name);
        }
        // フラグ
        for (int i = 0; i < var.var_flg.Count; i++)
        {
            flgVarNames.Add("システムフラグ変数 " + var.var_flg[i].name);
        }
        // 整数
        for (int i = 0; i < var.var_int.Count; i++)
        {
            intVarNames.Add("システム整数変数 " + var.var_int[i].name);
        }
        // 文字列
        for (int i = 0; i < var.var_str.Count; i++)
        {
            strVarNames.Add("システム文字列変数 " + var.var_str[i].name);
        }
    }

    public void StageInit(string path)
    {
        float x = -chipSizeX / 2;
        float y = -chipSizeY / 2;
        int numX = 1;
        int numY = 1;

        if ((data = OpenMapFile(path)) == null)
            return;

        nowStageName = path;

        MessageWindowManager mwm = GameObject.Find("SystemManager").GetComponent<MessageWindowManager>();
        mwm.getText.text = "";
        mwm.getMessageBox.SetActive(false);

        map = new string[data.map.mapSizeY, data.map.mapSizeX];

        int i = 0;
        for (int yy = 0; yy < data.map.mapSizeY; yy++)
        {
            for (int xx = 0; xx < data.map.mapSizeX; xx++)
            {
                map[yy, xx] = data.map.map[i];
                i++;
            }
        }

        // 背景ロード
        cameraObject.backgroundColor = data.bg.backcolor;
        Texture2D tex = Resources.Load(data.bg.background.Replace("Assets/Resources/", "").Split('.')[0]) as Texture2D;

        Destroy(stages);
        stages = new GameObject("Stage");

        GameObject backgrounds = new GameObject("Background");
        backgrounds.transform.parent = stages.transform;

        if (tex != null)
        {
            if (data.bg.loopXFlag && data.bg.loopYFlag)
            {
                numX = (int)(chipSizeX * 100 * map.GetLength(1) / tex.width) + 1;
                numY = (int)(chipSizeY * 100 * map.GetLength(0) / tex.height) + 1;
            }
            else if (data.bg.loopXFlag)
            {
                numX = (int)(chipSizeX * 100 * map.GetLength(1) / tex.width) + 1;
            }
            else if (data.bg.loopYFlag)
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
                    ins.transform.parent = backgrounds.transform;

                    x += tex.width / 100;
                }
                y -= tex.height / 100;
            }
        }

        // ステージロード
        x = 0;
        y = 0;

        for (i = data.map.mapSizeY - 1; i >= 0; i--)
        {
            x = 0;
            for (int j = 0; j < data.map.mapSizeX; j++)
            {
                string[] stas = map[i, j].Split('|');
                string prePath = stas[0];

                if (prePath != null && prePath != "")
                {
                    if (prePath.IndexOf("start") > -1)
                    {
                        startPos = new Vector2(x, y);
                        PlayerInstance.transform.position = startPos;
                    }
                    else
                    {
                        GameObject prefab = (GameObject)Resources.Load(prePath.Split('.')[0].Replace("Assets/Resources/", ""));
                        if (prefab != null)
                        {
                            GameObject ins = Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity) as GameObject;
                            ins.transform.parent = stages.transform;
                        }
                    }
                }

                x += chipSizeX;
            }

            y += chipSizeY;
        }

        // イベントロード
        GameObject events = new GameObject("Event");

        for (i = 0; i < data.ev.eventChip.Length; i++)
        {
            MapEventChip ev = data.ev.eventChip[i];

            GameObject prefab = (GameObject)Resources.Load("Prefabs/EventPrefab");
            if (prefab != null)
            {
                GameObject ins = Instantiate(prefab, new Vector3(ev.x * chipSizeX, data.map.mapSizeY * chipSizeY - ev.y * chipSizeY - chipSizeY, 0), Quaternion.identity) as GameObject;
                ins.transform.parent = events.transform;
                BoxCollider2D col = ins.transform.GetComponent<BoxCollider2D>();
                col.offset = new Vector2(ev.rect.x * chipSizeX, -ev.rect.y * chipSizeY);
                col.size = new Vector2(ev.rect.width * chipSizeX, ev.rect.height * chipSizeY);
                EventManager em = ins.transform.GetComponent<EventManager>();
                em.eventData = ev;
                em.mode = ev.mode;
            }
        }
    }

    private MapSaveData OpenMapFile(string path)
    {
        return JsonUtility.FromJson<MapSaveData>((Resources.Load(stagePath + path) as TextAsset).text);
    }

    public void VarLoad()
    {
        var = JsonUtility.FromJson<VariableManager>((Resources.Load("var") as TextAsset).text);
    }

    public GameObject GetPlayer
    {
        get { return PlayerInstance; }
    }

    public string[,] getMap
    {
        get { return map; }
    }

    public MapSaveData Data
    {
        get { return data; }
    }

    public Vector2 getStartPos
    {
        get { return startPos; }
    }

    public string getNowStageName
    {
        get { return nowStageName; }
    }

    public VariableManager Var
    {
        get { return var; }
        set { var = value; }
    }

    public FlgVarData[] FlgVar
    {
        get { return flgVar; }
        set { flgVar = value; }
    }

    public IntVarData[] IntVar
    {
        get { return intVar; }
        set { intVar = value; }
    }

    public StrVarData[] StrVar
    {
        get { return strVar; }
        set { strVar = value; }
    }

    public List<string> FlgVarNames
    {
        get { return flgVarNames; }
        set { flgVarNames = value; }
    }

    public List<string> IntVarNames
    {
        get { return intVarNames; }
        set { intVarNames = value; }
    }

    public List<string> StrVarNames
    {
        get { return strVarNames; }
        set { strVarNames = value; }
    }

    public string ExtRemover(string str)
    {
        string[] strs = str.Split('.');
        if (strs.Length == 0)
            return str;
        string result = "";
        for (int i = 0; i < strs.Length - 1; i++)
            result += strs[i];
        return result;
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
public class EventCommand
{
    public string viewCommand;
    public string jsonCommand;
    public string type;

    public EventCommand(string json, string com, string type)
    {
        jsonCommand = json;
        viewCommand = com;
        this.type = type;
    }
}

[System.Serializable]
public class EventMessageData
{
    public string messageWindow_text;
}

[System.Serializable]
public class EventImageData
{
    public string imageName;
    public int selectIndex;
    public string selectName;
    public int layerIndex;
    public bool aspectFlg;
    public float x;
    public float y;
    public float w;
    public float h;
}

[System.Serializable]
public class EventVarData
{
    public int selectFlgIndex;
    public int selectIntIndex;
    public int selectStrIndex;
    public string selectFlgName;
    public string selectIntName;
    public string selectStrName;
    public int varMode;
    public int selectOprIndex;
    public string selectOprName;
    public int selectIntOprIndex;
    public int selectStrOprIndex;
    public string selectIntOprName;
    public string selectStrOprName;
    public int varInputMode;
    public int varInputMode2;
    public int selectSetFlgIndex;
    public int selectSetIntIndex;
    public int selectSetIntIndex2;
    public int selectSetStrIndex;
    public int selectSetStrIndex2;
    public string selectSetFlgName;
    public string selectSetIntName;
    public string selectSetIntName2;
    public string selectSetStrName;
    public string selectSetStrName2;
    public int varSetInt;
    public int varSetInt2;
    public bool varSetFlg;
    public string varSetStr;
    public string varSetStr2;
}

[System.Serializable]
public class EventMoveData
{
    public int selectMoveStageIndex;
    public string selectMoveStageName;
    public bool moveStageSameFlag;
    public Vector2 movePos = new Vector2(-1, -1);
}

[System.Serializable]
public class EventIfData
{
    public bool interFlg;
    public bool elseFlg;
    public int contNum;
    public int nowContNum;
    public EventIfContent[] content = new EventIfContent[3];

    public EventIfData()
    {
        for (int i = 0; i < content.Length; i++)
        {
            content[i] = new EventIfContent();
        }
    }
}

[System.Serializable]
public class EventIfContent
{
    public bool isEnable;
    public int mode = 0;
    public int selectVarIndex1;
    public string selectVarName1;
    public int selectOprIndex;
    public string selectOprName;
    public int varInputMode;
    public int selectVarIndex2;
    public string selectVarName2;
    public bool inputFlg;
    public int inputInt;
    public string inputStr;
}

[System.Serializable]
public class EventLoopData
{
    public int mode = 0;
    public int selectVarIndex1;
    public string selectVarName1;
    public int selectOprIndex;
    public string selectOprName;
    public int varInputMode;
    public int selectVarIndex2;
    public string selectVarName2;
    public bool inputFlg;
    public int inputInt;
    public string inputStr;
}

[System.Serializable]
public class EventSoundData
{
    public string soundName;
    public string soundPath;
    public int selectSoundIndex;
    public bool soundLoopFlag;
    public bool isPlayFlag;
    public bool isPlayOffFlag;
    public float soundFastStartTime;
    public float soundStartTime;
    public float soundEndTime;
    public float soundPitch;
    public float soundVolume;
    public float soundPlayTime;
    public float soundOldTime;
}

[System.Serializable]
public class EventEffectData
{

}

[System.Serializable]
public class EventOtherData
{
    public string adminName;
}

[System.Serializable]
public class EventFold
{
    public bool select = false;
    public List<EventCommand> command = new List<EventCommand>();
    public int selectStart = 0;
    public int selectEnd = 0;

    public EventFold()
    {
        command.Add(new EventCommand("", "■", ""));
    }
}

[System.Serializable]
public class MapEventChip
{
    public string name;
    public float x;
    public float y;
    public Rect rect;
    public int mode;
    public EventFold _event;
}

[System.Serializable]
public class MapData
{
    public string[] map;
    public int mapSizeX;
    public int mapSizeY;
    public Rect viewRect;
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
public class MapEventData
{
    public MapEventChip[] eventChip;
}

[System.Serializable]
public class MapSaveData
{
    public MapData map;
    public MapEventData ev;
    public MapBackgroundData bg;
}