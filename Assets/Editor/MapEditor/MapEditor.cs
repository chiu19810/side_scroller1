using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public delegate void FuncOpener();
public delegate void FuncSelectBoxOpener(int id);
public class MapEditor : EditorWindow
{
    // バージョン情報
    private const string VERSION = "MapEditor v1.0";
    private const string VERSION_TEXT = "（一応）汎用マップエディタ・。・ｖ\n© 2017 untida";

    // 基本
    private const float WINDOW_W = 950;
    private const float WINDOW_H = 650;
    private const float LEFT_W = 220;
    private const float LABEL_W = 50;
    private const int VAR_FLG_SIZE = 10;
    private const int VAR_INT_SIZE = 10;
    private const int VAR_STR_SIZE = 10;
    private int ctrlCount;
    private int ctrlCheckCount;
    private int doubleCount;
    private int oldDoubleIndex;
    private int mouseX;
    private int mouseY;
    private int activeFileIndex;
    private string varPath = "Assets/Resources/var.txt";
    private string chipSearchPath = "Assets/Resources/Prefabs/MapChip/";
    private string objectSearchPath = "Assets/Resources/Prefabs/MapObject/";
    private string defaultDirectory = "Assets/Resources/Map/Stages/";
    private string defaultMapDirectory = "Assets/Resources/Map/Maps/";
    private string defaultBackgroundDirectory = "Assets/Resources/Map/Backgrounds/";
    private string defaultEventDirectory = "Assets/Resources/Map/Events/";
    private string defaultSoundDirectory = "Assets/Resources/Sounds/";
    private string defaultImageDirectory = "Assets/Resources/Textures/";
    private string status;
    private bool ctrlFlag;
    private bool playButtonFlag;
    private Drawer d = new Drawer();
    private SelectBox sb = new SelectBox();
    private List<OpenMapFile> openMapFiles = new List<OpenMapFile>();

    public VariableManager var = new VariableManager();

    // マップ
    private string fileMapName = "新規マップ.txt";
    private List<GameObject> mapChipList = new List<GameObject>();
    private List<GameObject> mapObjectList = new List<GameObject>();
//    private List<string[,]> mapClipBoard = new List<string[,]>();

    // イベント
    private string fileEvName = "新規イベント.txt";
    private List<EventCommand> eventClipBoard = new List<EventCommand>();

    // 背景
    private string fileBgName = "新規背景.txt";

    // メニューバー
    private bool menuOpenFlag = false;
    private bool fileOpenFlag = false;
    private bool editOpenFlag = false;
    private bool helpOpenFlag = false;

    // ウィンドウ
    public MapEditorEventWindow evWindow;
    public MapEditorNewFileWindow newFileWindow;
    public MapEditorVarSettingWindow varWindow;

    public class OpenMapFile
    {
        // 基本
        public float gridSize = 50;
        public int mapSizeX = 10;
        public int mapSizeY = 10;
        public int mode;
        public int viewX;
        public int viewY;
        public int viewW;
        public int viewH;
        public string fileName;
        public string selectedLeftImagePath;
        public string selectedRightImagePath;
        public string openPath;
        public bool saveFlag;
        public bool checkSaveFlag;
        public bool prevFlag;
        public bool nextFlag;
        public Vector2 scrollPos = Vector2.zero;
        public Rect[,] gridRect;

        // マップ
        public string[,] map;
        public string[,] oldMap = null;
        public List<string[,]> mapPrevList = new List<string[,]>();
        public List<string[,]> mapNextList = new List<string[,]>();
        public List<bool> mapPrevSaveFlagList = new List<bool>();
        public List<bool> mapNextSaveFlagList = new List<bool>();
        public List<MapSizeIndex> mapPrevIndexList = new List<MapSizeIndex>();
        public List<MapSizeIndex> mapNextIndexList = new List<MapSizeIndex>();
        public Vector2 chipSelectBoxScrollPos = Vector2.zero;
        public Vector2 objectSelectBoxScrollPos = Vector2.zero;

        // イベント
        public int eventDragIndex;
        public int selectEventIndex;
        public bool eventDragFlag;
        public List<MapEventChip> eventChips = new List<MapEventChip>();
        public Vector2 selectVec;
        public Vector2 doubleOldVec;

        // 背景
        public MapBackgroundData bg = new MapBackgroundData();
        public MapBackgroundData oldBg = new MapBackgroundData();
        public Vector2 scrollPos2 = Vector2.zero;

        // ローカル変数
        public FlgVarData[] flgVar = new FlgVarData[10];
        public IntVarData[] intVar = new IntVarData[10];
        public StrVarData[] strVar = new StrVarData[10];

        public OpenMapFile()
        {
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
        }
    }

    [MenuItem("Window/MapEditor")]
	static void ShowTestMainWindow()
    {
        MapEditor window = (MapEditor) GetWindow (typeof (MapEditor));
        window.minSize = new Vector2(WINDOW_W, WINDOW_H);
        window.init();
    }

    void Update()
    {
        if (MapOpenFlag && openMapFiles != null && openMapFiles[activeFileIndex] != null)
        {
            if (openMapFiles[activeFileIndex].mapSizeX == 0 || openMapFiles[activeFileIndex].mapSizeY == 0)
                FileExit();
            if (openMapFiles[activeFileIndex].saveFlag && !(openMapFiles[activeFileIndex].fileName.IndexOf(" (*)") > -1))
                openMapFiles[activeFileIndex].fileName += " (*)";
            else if (!openMapFiles[activeFileIndex].saveFlag)
                openMapFiles[activeFileIndex].fileName = openMapFiles[activeFileIndex].fileName.Replace(" (*)", "");

            if (openMapFiles[activeFileIndex].mode == 1)
            {
                if (doubleCount > 0)
                    doubleCount--;
                else if (doubleCount < 0)
                    doubleCount = 0;
            }
            else if (openMapFiles[activeFileIndex].mode == 2)
            {
                bool flag = false;

                if (openMapFiles[activeFileIndex].oldBg.objectSize != openMapFiles[activeFileIndex].bg.objectSize)
                {
                    Repaint();
                    return;
                }

                for (int i = 0; i < openMapFiles[activeFileIndex].oldBg.foldouts.Length; i++)
                {
                    if (openMapFiles[activeFileIndex].oldBg.foldouts[i] != null &&
                        openMapFiles[activeFileIndex].bg.foldouts[i] != null &&
                        (openMapFiles[activeFileIndex].oldBg.foldouts[i].foldout == openMapFiles[activeFileIndex].bg.foldouts[i].foldout ||
                        openMapFiles[activeFileIndex].oldBg.foldouts[i].obj == openMapFiles[activeFileIndex].bg.foldouts[i].obj ||
                        openMapFiles[activeFileIndex].oldBg.foldouts[i].objectIsX == openMapFiles[activeFileIndex].bg.foldouts[i].objectIsX ||
                        openMapFiles[activeFileIndex].oldBg.foldouts[i].objectIsY == openMapFiles[activeFileIndex].bg.foldouts[i].objectIsY ||
                        openMapFiles[activeFileIndex].oldBg.foldouts[i].objectLoopX == openMapFiles[activeFileIndex].bg.foldouts[i].objectLoopX ||
                        openMapFiles[activeFileIndex].oldBg.foldouts[i].objectLoopY == openMapFiles[activeFileIndex].bg.foldouts[i].objectLoopY ||
                        openMapFiles[activeFileIndex].oldBg.foldouts[i].objectX == openMapFiles[activeFileIndex].bg.foldouts[i].objectX ||
                        openMapFiles[activeFileIndex].oldBg.foldouts[i].objectY == openMapFiles[activeFileIndex].bg.foldouts[i].objectY))
                        flag = true;
                }

                if (openMapFiles[activeFileIndex].oldBg.mode != openMapFiles[activeFileIndex].bg.mode ||
                    openMapFiles[activeFileIndex].oldBg.background != openMapFiles[activeFileIndex].bg.background ||
                    openMapFiles[activeFileIndex].oldBg.loopXFlag != openMapFiles[activeFileIndex].bg.loopXFlag ||
                    openMapFiles[activeFileIndex].oldBg.loopYFlag != openMapFiles[activeFileIndex].bg.loopYFlag ||
                    openMapFiles[activeFileIndex].oldBg.backcolor != openMapFiles[activeFileIndex].bg.backcolor ||
                    flag)
                {
                    openMapFiles[activeFileIndex].saveFlag = true;
                    Repaint();
                }
            }
        }
    }
    void OnGUI()
    {
/*            PlaymodeStateObserver.OnPressedPlayButton += () =>
        {
            if (!playButtonFlag && openMapFiles != null && MapOpenFlag && openMapFiles[activeFileIndex].saveFlag)
            {
                openMapFiles[activeFileIndex].saveFlag = false;
                if (!EditorUtility.DisplayDialog("MapEditor 警告", "再生すると変更が破棄されます。\n保存しますか？（保存しなかった場合変更は破棄されます。）", " はい ", " いいえ "))
                {
                    playButtonFlag = true;
                    return;
                }
                else
                {
                    playButtonFlag = true;
                    OutputFile();
                    return;
                }
            }
        };*/

        Event e = Event.current;
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginHorizontal(GUILayout.Width(180));
        DrawMenuButton();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        // 左
        Rect rect = new Rect(2, 60, LEFT_W - 1, Screen.height - 114);
        EditorGUI.DrawRect(rect, new Color(0.7f, 0.7f, 0.7f));
        d.DrawLine(rect, new Color(0.3f, 0.3f, 0.3f));
        EditorGUILayout.BeginVertical(GUILayout.Width(LEFT_W));
        GUILayout.Space(40);
        if (MapOpenFlag)
        {
            if (openMapFiles[activeFileIndex].mode == 0)
            {
                // マップ
                SelectChipBox();
                DrawSelectedImage();
            }
            else if (openMapFiles[activeFileIndex].mode == 1)
            {
                // イベント
                EventGUI();
            }
            else if (openMapFiles[activeFileIndex].mode == 2)
            {
                // 背景
                BgGUI();
            }
        }
        EditorGUILayout.EndVertical();
        if (MapOpenFlag)
        {
            if (openMapFiles[activeFileIndex].mode == 0)
                GUILayout.Space(-8);
            else if (openMapFiles[activeFileIndex].mode == 1)
                GUILayout.Space(-5);
        }
        // 右
        EditorGUILayout.BeginVertical(GUILayout.Width(Screen.width - LEFT_W - 10));
        MainGUI(e);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(25);
        DrawOpenFileTab();
        if (MapOpenFlag)
            StatusGUI();
        MenuGUI();
        EditorGUILayout.EndVertical();
        if (e.type == EventType.KeyDown || e.type == EventType.MouseMove || e.type == EventType.MouseDown)
        {
            if (MapOpenFlag)
            {
                if (openMapFiles[activeFileIndex].mode == 2 && e.keyCode == KeyCode.Delete &&
                    openMapFiles[activeFileIndex].eventChips.Count > 0 && openMapFiles[activeFileIndex].selectEventIndex != -1)
                {
                    openMapFiles[activeFileIndex].eventChips.RemoveAt(openMapFiles[activeFileIndex].selectEventIndex);
                    openMapFiles[activeFileIndex].selectEventIndex = -1;
                    Repaint();
                }
            }

            if (e.type == EventType.MouseDown)
                GUI.FocusControl("");
            Repaint();
        }
    }
    void OnDestroy()
    {
        if (MapOpenFlag && openMapFiles[activeFileIndex].saveFlag)
        {
            if (EditorUtility.DisplayDialog("MapEditor 警告", "変更が保存されていません。\n保存しますか？", " はい ", " いいえ "))
            {
                OutputFile();
            }
        }
    }
    public class MapSizeIndex
    {
        private int mapSizeX;
        private int mapSizeY;

        public MapSizeIndex(int x, int y)
        {
            mapSizeX = x;
            mapSizeY = y;
        }

        public int MapSizeX
        {
            get { return mapSizeX; }
            set { mapSizeX = value; }
        }

        public int MapSizeY
        {
            get { return mapSizeY; }
            set { mapSizeY = value; }
        }
    }
    public void MapSizeUpdate()
    {
        openMapFiles[activeFileIndex].viewX = 0;
        openMapFiles[activeFileIndex].viewY = 0;
        openMapFiles[activeFileIndex].viewW = openMapFiles[activeFileIndex].mapSizeX;
        openMapFiles[activeFileIndex].viewH = openMapFiles[activeFileIndex].mapSizeY;
        string[,] oldMap = (string[,])openMapFiles[activeFileIndex].map.Clone();
        string[,] newMap = new string[openMapFiles[activeFileIndex].mapSizeY, openMapFiles[activeFileIndex].mapSizeX];

        bool flag = newMap.Length > oldMap.Length;

        for (int i = 0; i < (flag ? newMap.GetLength(0) : oldMap.GetLength(0)); i++)
        {
            for (int j = 0; j < (flag ? newMap.GetLength(1) : oldMap.GetLength(1)); j++)
            {
                if (flag)
                    newMap[i, j] = "";

                if ((flag ? oldMap.GetLength(1) > j && oldMap.GetLength(0) > i : newMap.GetLength(1) > j && newMap.GetLength(0) > i))
                    newMap[i, j] = oldMap[i, j];
            }
        }
        openMapFiles[activeFileIndex].map = (string[,])newMap.Clone();

        openMapFiles[activeFileIndex].mapPrevIndexList.Add(new MapSizeIndex(oldMap.GetLength(1), oldMap.GetLength(0)));
        openMapFiles[activeFileIndex].mapNextIndexList.Clear();
        openMapFiles[activeFileIndex].mapPrevList.Add((string[,])oldMap.Clone());
        openMapFiles[activeFileIndex].mapNextList.Clear();
        openMapFiles[activeFileIndex].mapPrevSaveFlagList.Add(openMapFiles[activeFileIndex].saveFlag);
        openMapFiles[activeFileIndex].mapNextSaveFlagList.Clear();
        openMapFiles[activeFileIndex].saveFlag = true;
        oldMap = null;

        GridSizeUpdater();
    }
    public void NewFileInit(Vector2 vec)
    {
        OpenMapFile file = new OpenMapFile();

        // 基本
        file.mapSizeX = (int) vec.x;
        file.mapSizeY = (int) vec.y;
        file.gridSize = 50;
        file.mode = 0;
        file.saveFlag = false;
        file.checkSaveFlag = false;
        file.prevFlag = false;
        file.nextFlag = false;
        file.fileName = "新規ファイル.txt";

        // マップ
        file.scrollPos.y = 50 * file.mapSizeY;
        file.viewX = 0;
        file.viewY = 0;
        file.viewW = file.mapSizeX;
        file.viewH = file.mapSizeY;
        file.mapPrevList = new List<string[,]>();
        file.mapNextList = new List<string[,]>();
        file.mapPrevSaveFlagList = new List<bool>();
        file.mapNextSaveFlagList = new List<bool>();
        file.mapPrevIndexList = new List<MapSizeIndex>();
        file.mapNextIndexList = new List<MapSizeIndex>();
        file.selectedRightImagePath = "eraser";

        file.map = new string[file.mapSizeY, file.mapSizeX];
        for (int i = 0; i < file.mapSizeY; i++)
        {
            for (int j = 0; j < file.mapSizeX; j++)
            {
                file.map[i, j] = "";
            }
        }

        // イベント
        file.selectVec = new Vector2(-1, -1);
        file.selectEventIndex = -1;
        file.eventChips.Clear();

        // 背景
        file.bg.objectSize = 0;
        file.bg.mode = 0;
        file.bg.loopXFlag = false;
        file.bg.loopYFlag = false;
        file.bg.background = null;
        file.bg.backcolor = new Color(119f / 255f, 211f / 255f, 255f / 255f);
        file.bg.foldouts = null;

        openMapFiles.Add(file);
        activeFileIndex = openMapFiles.Count - 1;

        file.gridRect = CreateGrid(file.mapSizeY, file.mapSizeX);

        Repaint();
    }
    public void VarInit()
    {
        if (!File.Exists(varPath))
        {
            for (int i = 0; i < VAR_FLG_SIZE; i++)
            {
                FlgVarData vd = new FlgVarData();
                vd.name = "" + i;
                vd.var = false;
                var.var_flg.Add(vd);
            }

            for (int i = 0; i < VAR_INT_SIZE; i++)
            {
                IntVarData vd = new IntVarData();
                vd.name = "" + i;
                vd.var = 0;
                var.var_int.Add(vd);
            }

            for (int i = 0; i < VAR_STR_SIZE; i++)
            {
                StrVarData vd = new StrVarData();
                vd.name = "" + i;
                vd.var = "";
                var.var_str.Add(vd);
            }

            VarSave();
        }
        else
        {
            VarLoad();
        }
    }
    public void VarSave()
    {
        if (File.Exists(varPath))
        {
            FileStream st = new FileStream(varPath, FileMode.Open);
            st.SetLength(0);
            st.Close();
        }
        
        FileInfo fileInfo = new FileInfo(varPath);
        StreamWriter sw = fileInfo.AppendText();
        sw.WriteLine(JsonUtility.ToJson(var));
        sw.Flush();
        sw.Close();
    }
    public void VarLoad()
    {
        if (File.Exists(varPath))
        {
            StreamReader sr = new StreamReader(varPath, System.Text.Encoding.UTF8);
            var = JsonUtility.FromJson<VariableManager>(sr.ReadToEnd());
            sr.Close();
        }
    }
    private void init()
    {
        // 基本
        wantsMouseMove = true;
        playButtonFlag = false;
        ctrlFlag = false;
        if (openMapFiles != null)
            openMapFiles.Clear();

        // マップ
        mapChipList.Clear();
        mapObjectList.Clear();

        // チップファイル
        string[] filePaths = Directory.GetFiles(chipSearchPath, "*.prefab");
        foreach (string filePath in filePaths)
        {
            string path = filePath.Replace("\\", "/").Replace(Application.dataPath, "");
            GameObject obj = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
            if (obj != null)
            {
                mapChipList.Add(obj);
            }
        }

        // オブジェクトファイル
        filePaths = Directory.GetFiles(objectSearchPath, "*.prefab");
        foreach (string filePath in filePaths)
        {
            string path = filePath.Replace("\\", "/").Replace(Application.dataPath, "");
            GameObject obj = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
            if (obj != null)
            {
                mapObjectList.Add(obj);
            }
        }

        fileMapName = "";

        // サウンド
        if (GameObject.Find("EditorSystem") != null)
            DestroyImmediate(GameObject.Find("EditorSystem"));

        // 変数
        VarInit();
    }
    private void NewFileOpen()
    {
        newFileWindow = MapEditorNewFileWindow.WillAppear(this, 0);
    }
    private void OpenMapSizeUpdateWindow()
    {
        newFileWindow = MapEditorNewFileWindow.WillAppear(this, 1);
    }
    private void FileExit()
    {
        if (!MapOpenFlag)
            return;

        if (openMapFiles[activeFileIndex].saveFlag)
        {
            if (!EditorUtility.DisplayDialog("MapEditor 警告", "変更が保存されていませんが、ファイルを閉じますか？", " はい ", " いいえ "))
                return;
        }

        openMapFiles.RemoveAt(activeFileIndex);
        if (openMapFiles.Count == 0)
            init();
        else
            activeFileIndex--;

        if (activeFileIndex < 0)
            activeFileIndex = 0;

        Repaint();
    }
    private void GridSizeUpdater()
    {
        openMapFiles[activeFileIndex].gridRect = CreateGrid(openMapFiles[activeFileIndex].mapSizeY, openMapFiles[activeFileIndex].mapSizeX);
        Repaint();
    }
    private void MainGUI(Event e)
    {
        mouseX = -1;
        mouseY = -1;
        status = "";

        DrawTabButton();
        GUILayout.Space(27);

        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(Screen.width - LEFT_W - 5));

        if (MapOpenFlag)
        {
            string oldView = MapOpenFlag ? openMapFiles[activeFileIndex].viewX + ":" + openMapFiles[activeFileIndex].viewY + ":" + openMapFiles[activeFileIndex].viewW + ":" + openMapFiles[activeFileIndex].viewH : "";
            Rect workArea = GUILayoutUtility.GetRect(10, 10000, 10, 10000);
            openMapFiles[activeFileIndex].scrollPos = GUI.BeginScrollView(workArea, openMapFiles[activeFileIndex].scrollPos, new Rect(0, 0, openMapFiles[activeFileIndex].mapSizeX * openMapFiles[activeFileIndex].gridSize + 5, openMapFiles[activeFileIndex].mapSizeY * openMapFiles[activeFileIndex].gridSize + 5), false, false);

            Vector2 pos = e.mousePosition;
            int xx;

            if (e.type == EventType.layout)
                pos = new Vector2(-1, -1);

            if (MapOpenFlag && (openMapFiles[activeFileIndex].gridRect == null || openMapFiles[activeFileIndex].map == null))
            {
                init();
                Repaint();
            }

            // イベント
            if (openMapFiles[activeFileIndex].mode == 1)
            {
                doubleCount = doubleCount < 1 ? 0 : doubleCount;
            }

            // 背景
            float backSizeX = Screen.width - LEFT_W - 5;
            float backSizeY = Screen.height;

            if (backSizeX < openMapFiles[activeFileIndex].gridSize * openMapFiles[activeFileIndex].mapSizeX + 5)
            {
                backSizeX = openMapFiles[activeFileIndex].gridSize * openMapFiles[activeFileIndex].mapSizeX + 5;
            }

            if (backSizeY < openMapFiles[activeFileIndex].gridSize * openMapFiles[activeFileIndex].mapSizeY + 5)
            {
                backSizeY = openMapFiles[activeFileIndex].gridSize * openMapFiles[activeFileIndex].mapSizeY + 5;
            }

            if (MapOpenFlag)
            {
                EditorGUI.DrawRect(new Rect(0, 0, backSizeX, backSizeY), openMapFiles[activeFileIndex].bg.backcolor);

                if (openMapFiles[activeFileIndex].bg.background != "" && openMapFiles[activeFileIndex].bg.background != null)
                {
                    Texture2D tex = AssetDatabase.LoadAssetAtPath(openMapFiles[activeFileIndex].bg.background, typeof(Texture2D)) as Texture2D;

                    float stageW = openMapFiles[activeFileIndex].gridSize * openMapFiles[activeFileIndex].mapSizeX;
                    float stageH = openMapFiles[activeFileIndex].gridSize * openMapFiles[activeFileIndex].mapSizeY;
                    float w = tex.width;
                    float h = tex.height;
                    float x = 0;
                    float y = stageH - h * (openMapFiles[activeFileIndex].gridSize / 64);
                    int numX = 1;
                    int numY = 1;

                    if (openMapFiles[activeFileIndex].bg.loopXFlag && openMapFiles[activeFileIndex].bg.loopYFlag)
                    {
                        numX = (int)(stageW / (w * (openMapFiles[activeFileIndex].gridSize / 64))) + 1;
                        numY = (int)(stageH / (h * (openMapFiles[activeFileIndex].gridSize / 64))) + 1;
                    }
                    else if (openMapFiles[activeFileIndex].bg.loopXFlag)
                    {
                        numX = (int)(stageW / (w * (openMapFiles[activeFileIndex].gridSize / 64))) + 1;
                    }
                    else if (openMapFiles[activeFileIndex].bg.loopYFlag)
                    {
                        numY = (int)(stageH / (h * (openMapFiles[activeFileIndex].gridSize / 64))) + 1;
                    }

                    for (int yy = 0; yy < numY; yy++)
                    {
                        x = 0;
                        for (xx = 0; xx < numX; xx++)
                        {
                            Texture2D tex2 = (Texture2D)AssetDatabase.LoadAssetAtPath(openMapFiles[activeFileIndex].bg.background, typeof(Texture2D));
                            GUI.DrawTexture(new Rect(x, y, w * (openMapFiles[activeFileIndex].gridSize / 64), h * (openMapFiles[activeFileIndex].gridSize / 64)), tex2);

                            x += w * (openMapFiles[activeFileIndex].gridSize / 64);
                        }
                        y -= h * (openMapFiles[activeFileIndex].gridSize / 64);
                    }
                }

                if (openMapFiles[activeFileIndex].bg.foldouts != null && openMapFiles[activeFileIndex].bg.mode == 1)
                {
                    for (int i = 0; i < openMapFiles[activeFileIndex].bg.objectSize; i++)
                    {
                        if (openMapFiles[activeFileIndex].bg.foldouts[i] != null)
                        {
                            if (openMapFiles[activeFileIndex].bg.foldouts[i].obj != "" && openMapFiles[activeFileIndex].bg.foldouts[i].obj != null)
                            {
                                Texture2D tex = AssetDatabase.LoadAssetAtPath(openMapFiles[activeFileIndex].bg.foldouts[i].obj, typeof(Texture2D)) as Texture2D;

                                float stageW = openMapFiles[activeFileIndex].gridSize * openMapFiles[activeFileIndex].mapSizeX;
                                float stageH = openMapFiles[activeFileIndex].gridSize * openMapFiles[activeFileIndex].mapSizeY;
                                float w = tex.width;
                                float h = tex.height;
                                float x = openMapFiles[activeFileIndex].bg.foldouts[i].objectX * (openMapFiles[activeFileIndex].gridSize / 64);
                                float y = stageH - h * (openMapFiles[activeFileIndex].gridSize / 64) - openMapFiles[activeFileIndex].bg.foldouts[i].objectY * (openMapFiles[activeFileIndex].gridSize / 64);
                                int numX = 1;
                                int numY = 1;

                                if (openMapFiles[activeFileIndex].bg.foldouts[i].objectIsX && openMapFiles[activeFileIndex].bg.foldouts[i].objectIsY)
                                {
                                    numX = (int)(stageW / (w * (openMapFiles[activeFileIndex].gridSize / 64) + openMapFiles[activeFileIndex].bg.foldouts[i].objectLoopX * (openMapFiles[activeFileIndex].gridSize / 64))) + 1;
                                    numY = (int)(stageH / (h * (openMapFiles[activeFileIndex].gridSize / 64) + openMapFiles[activeFileIndex].bg.foldouts[i].objectLoopY * (openMapFiles[activeFileIndex].gridSize / 64))) + 1;
                                }
                                else if (openMapFiles[activeFileIndex].bg.foldouts[i].objectIsX)
                                {
                                    numX = (int)(stageW / (w * (openMapFiles[activeFileIndex].gridSize / 64) + openMapFiles[activeFileIndex].bg.foldouts[i].objectLoopX * (openMapFiles[activeFileIndex].gridSize / 64))) + 1;
                                }
                                else if (openMapFiles[activeFileIndex].bg.foldouts[i].objectIsY)
                                {
                                    numY = (int)(stageH / (h * (openMapFiles[activeFileIndex].gridSize / 64) + openMapFiles[activeFileIndex].bg.foldouts[i].objectLoopY * (openMapFiles[activeFileIndex].gridSize / 64))) + 1;
                                }

                                for (int yy = 0; yy < numY; yy++)
                                {
                                    x = openMapFiles[activeFileIndex].bg.foldouts[i].objectX * (openMapFiles[activeFileIndex].gridSize / 64);
                                    for (xx = 0; xx < numX; xx++)
                                    {
                                        Texture2D tex2 = (Texture2D)AssetDatabase.LoadAssetAtPath(openMapFiles[activeFileIndex].bg.foldouts[i].obj, typeof(Texture2D));
                                        GUI.DrawTexture(new Rect(x, y, w * (openMapFiles[activeFileIndex].gridSize / 64), h * (openMapFiles[activeFileIndex].gridSize / 64)), tex2);

                                        x += w * (openMapFiles[activeFileIndex].gridSize / 64) + openMapFiles[activeFileIndex].bg.foldouts[i].objectLoopX * (openMapFiles[activeFileIndex].gridSize / 64);
                                    }
                                    y -= h * (openMapFiles[activeFileIndex].gridSize / 64) + openMapFiles[activeFileIndex].bg.foldouts[i].objectLoopY * (openMapFiles[activeFileIndex].gridSize / 64);
                                }
                            }
                        }
                    }
                }

                for (xx = 0; xx < openMapFiles[activeFileIndex].mapSizeX; xx++)
                {
                    if (pos.x > openMapFiles[activeFileIndex].mapSizeX * openMapFiles[activeFileIndex].gridSize)
                    {
                        xx = openMapFiles[activeFileIndex].mapSizeX - 1;
                        break;
                    }
                    else if (pos.x < 0)
                    {
                        xx = 0;
                        break;
                    }

                    Rect r = openMapFiles[activeFileIndex].gridRect[0, xx];
                    if (r.x <= pos.x && pos.x <= r.x + r.width)
                        break;
                }

                for (int yy = 0; yy < openMapFiles[activeFileIndex].mapSizeY; yy++)
                {
                    if (pos.y > openMapFiles[activeFileIndex].mapSizeY * openMapFiles[activeFileIndex].gridSize)
                    {
                        yy = openMapFiles[activeFileIndex].mapSizeY - 1;
                        break;
                    }
                    else if (pos.y < 0)
                    {
                        yy = 0;
                        break;
                    }

                    if (openMapFiles[activeFileIndex].gridRect[yy, xx].Contains(pos))
                    {
                        mouseX = xx;
                        mouseY = yy;
                    }
                }

                int oldCtrlCount = ctrlCount;

                if (e.type == EventType.MouseDown)
                {
                    GUI.FocusControl("");
                }

                if (e.type == EventType.KeyDown)
                {
                    if (e.keyCode == KeyCode.LeftControl || e.keyCode == KeyCode.RightControl)
                    {
                        ctrlFlag = true;
                        ctrlCount++;
                    }

                    // イベント削除
                    if (e.keyCode == KeyCode.Delete)
                    {
                        if (openMapFiles[activeFileIndex].eventChips.Count > 0 && openMapFiles[activeFileIndex].selectEventIndex != -1)
                        {
                            openMapFiles[activeFileIndex].eventChips.RemoveAt(openMapFiles[activeFileIndex].selectEventIndex);
                            openMapFiles[activeFileIndex].selectEventIndex = -1;
                            Repaint();
                        }
                    }
                }
                else if (e.type == EventType.KeyUp)
                {
                    ctrlCount = 0;
                    ctrlCheckCount = 0;
                    ctrlFlag = false;
                }

                if (ctrlCount == oldCtrlCount)
                    ctrlCheckCount++;

                if (ctrlCheckCount > 20)
                {
                    ctrlCount = 0;
                    ctrlCheckCount = 0;
                    ctrlFlag = false;
                }

                if (e.type == EventType.ScrollWheel)
                {
                    // ホイールで拡大/縮小
                    if (pos.x > 0 && pos.x < Screen.width && pos.y > 0 && pos.y < 520 && ctrlFlag)
                    {
                        if (e.delta[1] == 3)
                            openMapFiles[activeFileIndex].gridSize = openMapFiles[activeFileIndex].gridSize + 5;
                        else if (e.delta[1] == -3)
                            openMapFiles[activeFileIndex].gridSize = openMapFiles[activeFileIndex].gridSize - 5;

                        if (openMapFiles[activeFileIndex].gridSize > 100)
                            openMapFiles[activeFileIndex].gridSize = 100;
                        else if (openMapFiles[activeFileIndex].gridSize < 5)
                            openMapFiles[activeFileIndex].gridSize = 5;

                        GridSizeUpdater();
                        Repaint();
                    }
                }
                else if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
                {
                    if (openMapFiles[activeFileIndex].mode == 0)
                    {
                        // マップ
                        if (mouseX != -1 && mouseY != -1)
                        {
                            // 左クリック/右クリック
                            if ((e.button == 0 || e.button == 1) && !menuOpenFlag)
                            {
                                string path = "";

                                if (e.button == 0)
                                    path = openMapFiles[activeFileIndex].selectedLeftImagePath;
                                else if (e.button == 1)
                                    path = openMapFiles[activeFileIndex].selectedRightImagePath;

                                if (path != null)
                                {
                                    bool flag = false;
                                    string[,] _oldmap = (string[,])openMapFiles[activeFileIndex].map.Clone();

                                    if (path.IndexOf("eraser") > -1)
                                    {
                                        if (openMapFiles[activeFileIndex].map[mouseY, mouseX] != "")
                                        {
                                            flag = true;
                                            openMapFiles[activeFileIndex].map[mouseY, mouseX] = "";
                                        }
                                    }
                                    else if (path.IndexOf("start") > -1)
                                    {
                                        for (int yyy = 0; yyy < openMapFiles[activeFileIndex].mapSizeY; yyy++)
                                        {
                                            for (int xxx = 0; xxx < openMapFiles[activeFileIndex].mapSizeX; xxx++)
                                            {
                                                if (openMapFiles[activeFileIndex].map[yyy, xxx].IndexOf("start") > -1)
                                                    openMapFiles[activeFileIndex].map[yyy, xxx] = "";
                                            }
                                        }

                                        if (openMapFiles[activeFileIndex].map[mouseY, mouseX] != path)
                                        {
                                            flag = true;
                                            openMapFiles[activeFileIndex].map[mouseY, mouseX] = path;
                                        }
                                    }
                                    else if (!(path.IndexOf("none") > -1))
                                    {
                                        if (openMapFiles[activeFileIndex].map[mouseY, mouseX] != path)
                                        {
                                            flag = true;
                                            openMapFiles[activeFileIndex].map[mouseY, mouseX] = path;
                                        }
                                    }

                                    if (flag)
                                    {
                                        if (!openMapFiles[activeFileIndex].checkSaveFlag)
                                        {
                                            openMapFiles[activeFileIndex].oldMap = _oldmap;
                                            openMapFiles[activeFileIndex].mapPrevSaveFlagList.Add(openMapFiles[activeFileIndex].saveFlag);
                                            openMapFiles[activeFileIndex].mapNextSaveFlagList.Clear();
                                            openMapFiles[activeFileIndex].saveFlag = true;
                                            openMapFiles[activeFileIndex].checkSaveFlag = true;
                                        }
                                    }
                                }

                                Repaint();
                            }
                            else if (e.button == 2)
                            {
                                // 中クリック
                                if (openMapFiles[activeFileIndex].map[mouseY, mouseX] != "")
                                {
                                    if (openMapFiles[activeFileIndex].map[mouseY, mouseX].IndexOf("start") > -1)
                                        openMapFiles[activeFileIndex].selectedLeftImagePath = openMapFiles[activeFileIndex].map[mouseY, mouseX];
                                    else
                                        openMapFiles[activeFileIndex].selectedLeftImagePath = openMapFiles[activeFileIndex].map[mouseY, mouseX];

                                    Repaint();
                                }
                            }
                        }
                    }
                    else if (openMapFiles[activeFileIndex].mode == 1)
                    {
                        // イベント
                        if (e.type == EventType.MouseDown && e.button == 0 && !menuOpenFlag)
                        {
                            if (Mathf.Floor(pos.x / openMapFiles[activeFileIndex].gridSize) * openMapFiles[activeFileIndex].gridSize < openMapFiles[activeFileIndex].mapSizeX * openMapFiles[activeFileIndex].gridSize &&
                                Mathf.Floor(pos.y / openMapFiles[activeFileIndex].gridSize) * openMapFiles[activeFileIndex].gridSize < openMapFiles[activeFileIndex].mapSizeY * openMapFiles[activeFileIndex].gridSize)
                            {
                                openMapFiles[activeFileIndex].selectVec = new Vector2(Mathf.Floor(pos.x / openMapFiles[activeFileIndex].gridSize), Mathf.Floor(pos.y / openMapFiles[activeFileIndex].gridSize));

                                bool flag = false;

                                for (int i = 0; i < openMapFiles[activeFileIndex].eventChips.Count; i++)
                                {
                                    if (openMapFiles[activeFileIndex].eventChips[i].x == openMapFiles[activeFileIndex].selectVec.x && openMapFiles[activeFileIndex].eventChips[i].y == openMapFiles[activeFileIndex].selectVec.y)
                                    {
                                        openMapFiles[activeFileIndex].selectEventIndex = i;
                                        flag = true;
                                        break;
                                    }
                                }

                                if (!flag)
                                    openMapFiles[activeFileIndex].selectEventIndex = -1;

                                // ダブルクリック
                                if (doubleCount > 0 && openMapFiles[activeFileIndex].selectVec == openMapFiles[activeFileIndex].doubleOldVec)
                                {
                                    if (flag)
                                    {
                                        evWindow = MapEditorEventWindow.WillAppear(this);
                                        evWindow.selectID = openMapFiles[activeFileIndex].selectEventIndex;
                                    }
                                    else
                                    {
                                        MapEventChip evChip = new MapEventChip();
                                        evChip.mode = 0;
                                        evChip.x = Mathf.Floor(pos.x / openMapFiles[activeFileIndex].gridSize);
                                        evChip.y = Mathf.Floor(pos.y / openMapFiles[activeFileIndex].gridSize);
                                        evChip.rect = new Rect(0, 0, 1, 1);
                                        evChip._event = new EventFold();
                                        openMapFiles[activeFileIndex].eventChips.Add(evChip);
                                        openMapFiles[activeFileIndex].selectEventIndex = openMapFiles[activeFileIndex].eventChips.Count - 1;
                                    }
                                }

                                doubleCount = 50;
                                openMapFiles[activeFileIndex].doubleOldVec = openMapFiles[activeFileIndex].selectVec;

                                Repaint();
                            }
                            else
                                openMapFiles[activeFileIndex].selectVec = new Vector2(-1, -1);
                        }
                        else if (e.type == EventType.MouseDrag && e.button == 0 && !menuOpenFlag)
                        {
                            if (!openMapFiles[activeFileIndex].eventDragFlag)
                            {
                                for (int i = 0; i < openMapFiles[activeFileIndex].eventChips.Count; i++)
                                {
                                    if (openMapFiles[activeFileIndex].eventChips[i].x == openMapFiles[activeFileIndex].selectVec.x && openMapFiles[activeFileIndex].eventChips[i].y == openMapFiles[activeFileIndex].selectVec.y)
                                    {
                                        openMapFiles[activeFileIndex].eventDragIndex = i;
                                        openMapFiles[activeFileIndex].eventDragFlag = true;
                                    }
                                }
                            }
                            else
                            {
                                float x = Mathf.Floor(pos.x / openMapFiles[activeFileIndex].gridSize) * openMapFiles[activeFileIndex].gridSize;
                                float y = Mathf.Floor(pos.y / openMapFiles[activeFileIndex].gridSize) * openMapFiles[activeFileIndex].gridSize;

                                if (x < openMapFiles[activeFileIndex].mapSizeX * openMapFiles[activeFileIndex].gridSize && y < openMapFiles[activeFileIndex].mapSizeY * openMapFiles[activeFileIndex].gridSize && x >= 0 && y >= 0)
                                {
                                    openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].eventDragIndex].x = Mathf.Floor(pos.x / openMapFiles[activeFileIndex].gridSize);
                                    openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].eventDragIndex].y = Mathf.Floor(pos.y / openMapFiles[activeFileIndex].gridSize);
                                    openMapFiles[activeFileIndex].selectVec = new Vector2(openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].eventDragIndex].x, openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].eventDragIndex].y);
                                }
                            }

                            Repaint();
                        }
                    }
                }

                // Undo追加
                if (e.type == EventType.MouseUp && openMapFiles[activeFileIndex].oldMap != null)
                {
                    openMapFiles[activeFileIndex].checkSaveFlag = false;
                    openMapFiles[activeFileIndex].mapPrevIndexList.Add(new MapSizeIndex(openMapFiles[activeFileIndex].oldMap.GetLength(1), openMapFiles[activeFileIndex].oldMap.GetLength(0)));
                    openMapFiles[activeFileIndex].mapNextIndexList.Clear();
                    openMapFiles[activeFileIndex].mapPrevList.Add((string[,])openMapFiles[activeFileIndex].oldMap.Clone());
                    openMapFiles[activeFileIndex].mapNextList.Clear();
                    openMapFiles[activeFileIndex].oldMap = null;
                    Repaint();
                }

                if (mouseX != -1 && mouseY != -1)
                {
                    string[] stas = openMapFiles[activeFileIndex].map[mouseY, mouseX].Split('|');

                    if (!(stas[0].IndexOf("start") > -1))
                    {
                        if (stas.Length > 1)
                            status = stas[0].Split('/')[stas[0].Split('/').Length - 1] + " / " + stas[1];
                    }
                    else
                        status = "プレイヤーのスタート地点";
                }
                else
                {
                    if (mouseX == -1)
                        mouseX = 0;

                    if (mouseY == -1)
                        mouseY = 0;
                }

                // イベント
                if (e.type == EventType.MouseUp)
                    openMapFiles[activeFileIndex].eventDragFlag = false;

                // マップチップを描画する
                for (int yy = 0; yy < openMapFiles[activeFileIndex].mapSizeY; yy++)
                {
                    for (xx = 0; xx < openMapFiles[activeFileIndex].mapSizeX; xx++)
                    {
                        if (openMapFiles[activeFileIndex].map[yy, xx] != null && openMapFiles[activeFileIndex].map[yy, xx].Length > 0)
                        {
                            string[] eves = openMapFiles[activeFileIndex].map[yy, xx].Split('#');
                            string[] stas = eves[0].Split('|');
                            string path = stas[0];

                            if (!(stas[0].IndexOf("start") > -1))
                                path = stas[0];

                            Texture2D tex;

                            try
                            {
                                if (path.IndexOf("none") > -1 || path.IndexOf("start") > -1)
                                    tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
                                else
                                    tex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath((AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject).GetComponent<SpriteRenderer>().sprite), typeof(Texture2D));

                                if (openMapFiles[activeFileIndex].mode == 2)
                                {
                                    Color oldcolor = GUI.color;
                                    GUI.color = new Color(oldcolor.r, oldcolor.g, oldcolor.b, oldcolor.a - 0.8f);
                                    GUI.DrawTexture(openMapFiles[activeFileIndex].gridRect[yy, xx], tex);
                                    GUI.color = oldcolor;
                                }
                                else
                                    GUI.DrawTexture(openMapFiles[activeFileIndex].gridRect[yy, xx], tex);
                            }
                            catch (System.Exception exception)
                            {
                                Debug.Log(exception);
                            }
                        }
                    }
                }

                // グリッド線を描画する
                for (int yy = 0; yy < openMapFiles[activeFileIndex].mapSizeY; yy++)
                {
                    for (xx = 0; xx < openMapFiles[activeFileIndex].mapSizeX; xx++)
                    {
                        d.DrawLine(openMapFiles[activeFileIndex].gridRect[yy, xx], new Color(1f, 1f, 1f, 0.5f));
                    }
                }

                // 表示範囲を描画
                d.DrawLine(new Rect(openMapFiles[activeFileIndex].viewX * openMapFiles[activeFileIndex].gridSize, openMapFiles[activeFileIndex].viewY * openMapFiles[activeFileIndex].gridSize, openMapFiles[activeFileIndex].viewW * openMapFiles[activeFileIndex].gridSize, openMapFiles[activeFileIndex].viewH * openMapFiles[activeFileIndex].gridSize), new Color(0, 0, 1f, 0.8f));

                if (openMapFiles[activeFileIndex].mode == 1)
                    EditorGUI.DrawRect(new Rect(0, 0, backSizeX, backSizeY), new Color(0, 0, 0, 0.7f));

                // イベントチップの描画
                for (int i = 0; i < openMapFiles[activeFileIndex].eventChips.Count; i++)
                {
                    Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Editor/MapEditor/event_chip.png", typeof(Texture2D));

                    if (openMapFiles[activeFileIndex].mode != 1)
                    {
                        Color oldcolor = GUI.color;
                        GUI.color = new Color(oldcolor.r, oldcolor.g, oldcolor.b, oldcolor.a - 0.8f);
                        GUI.DrawTexture(new Rect(openMapFiles[activeFileIndex].eventChips[i].x * openMapFiles[activeFileIndex].gridSize, openMapFiles[activeFileIndex].eventChips[i].y * openMapFiles[activeFileIndex].gridSize, openMapFiles[activeFileIndex].gridSize, openMapFiles[activeFileIndex].gridSize), tex);
                        GUI.color = oldcolor;
                    }
                    else
                        GUI.DrawTexture(new Rect(openMapFiles[activeFileIndex].eventChips[i].x * openMapFiles[activeFileIndex].gridSize, openMapFiles[activeFileIndex].eventChips[i].y * openMapFiles[activeFileIndex].gridSize, openMapFiles[activeFileIndex].gridSize, openMapFiles[activeFileIndex].gridSize), tex);

                    d.DrawLine(new Rect(openMapFiles[activeFileIndex].eventChips[i].x * openMapFiles[activeFileIndex].gridSize + (openMapFiles[activeFileIndex].eventChips[i].rect.x * openMapFiles[activeFileIndex].gridSize) + (openMapFiles[activeFileIndex].gridSize / 2) - ((openMapFiles[activeFileIndex].eventChips[i].rect.width * openMapFiles[activeFileIndex].gridSize) / 2), openMapFiles[activeFileIndex].eventChips[i].y * openMapFiles[activeFileIndex].gridSize + (openMapFiles[activeFileIndex].eventChips[i].rect.y * openMapFiles[activeFileIndex].gridSize) + (openMapFiles[activeFileIndex].gridSize / 2) - ((openMapFiles[activeFileIndex].eventChips[i].rect.height * openMapFiles[activeFileIndex].gridSize) / 2), openMapFiles[activeFileIndex].eventChips[i].rect.width * openMapFiles[activeFileIndex].gridSize, openMapFiles[activeFileIndex].eventChips[i].rect.height * openMapFiles[activeFileIndex].gridSize), openMapFiles[activeFileIndex].mode != 1 ? new Color(1, 0, 0, 0.2f) : Color.red);
                }

                // 選択グリッドの描画
                if (openMapFiles[activeFileIndex].mode == 1 && openMapFiles[activeFileIndex].selectVec.x != -1 && openMapFiles[activeFileIndex].selectVec.y != -1)
                    d.DrawLine(new Rect(openMapFiles[activeFileIndex].selectVec.x * openMapFiles[activeFileIndex].gridSize, openMapFiles[activeFileIndex].selectVec.y * openMapFiles[activeFileIndex].gridSize, openMapFiles[activeFileIndex].gridSize, openMapFiles[activeFileIndex].gridSize), Color.white);
            }

            if (oldView != openMapFiles[activeFileIndex].viewX + ":" + openMapFiles[activeFileIndex].viewY + ":" + openMapFiles[activeFileIndex].viewW + ":" + openMapFiles[activeFileIndex].viewH)
                openMapFiles[activeFileIndex].saveFlag = true;

            GUI.EndScrollView();
        }
        else
        {
            Rect workArea = GUILayoutUtility.GetRect(10, 10000, 10, 10000);
            GUI.BeginScrollView(workArea, Vector2.zero, new Rect(), false, false);
            GUI.EndScrollView();
        }

        EditorGUILayout.EndVertical();
    }
    private void MenuGUI()
    {
        // メニュー表示
        if (newFileWindow == null)
        {
            if (fileOpenFlag)
                MenuDrawer(3, 25, 200, new string[] { "新しいマップファイル", "マップファイルを開く", "-", (MapOpenFlag ? (openMapFiles[activeFileIndex].openPath != null && openMapFiles[activeFileIndex].openPath != "") ? "" : "@" : "@") + "保存", (MapOpenFlag ? "" : "@") + "名前をつけて保存", "-", (MapOpenFlag ? "" : "@") + "Export", (MapOpenFlag ? "" : "@") + "Import", "-", "変数設定", "-", (MapOpenFlag ? "" : "@") + "ファイルを閉じる", "終了" }, 12, new FuncOpener(NewFileOpen), new FuncOpener(OpenFile), new FuncOpener(Save), new FuncOpener(OutputFile), new FuncOpener(Export), new FuncOpener(Import), new FuncOpener(VarSetting), new FuncOpener(FileExit), new FuncOpener(Exit));

            if (editOpenFlag)
                MenuDrawer(3, 25, 200, new string[] { (MapOpenFlag ? (openMapFiles[activeFileIndex].mode == 0 ? (openMapFiles[activeFileIndex].prevFlag ? "" : "@") : "@") : "@") + "元に戻す", (MapOpenFlag ? (openMapFiles[activeFileIndex].mode == 0 ? (openMapFiles[activeFileIndex].nextFlag ? "" : "@") : "@") : "@") + "やり直し", "-", (MapOpenFlag ? "" : "@") + "マップサイズの変更" }, 12, new FuncOpener(Undo), new FuncOpener(Redo), new FuncOpener(OpenMapSizeUpdateWindow));

            if (helpOpenFlag)
                MenuDrawer(3, 25, 200, new string[] { "お絵かき", "バージョン情報" }, 12, new FuncOpener(() => { Painter.WillAppear(this); }), new FuncOpener(ShowVersion));
        }

        if (!menuOpenFlag)
        {
            fileOpenFlag = false;
            editOpenFlag = false;
            helpOpenFlag = false;
        }
    }
    private void DrawOpenFileTab()
    {
        if (MapOpenFlag)
        {
            float x = LEFT_W + 8;
            float y = -20;
            float w = (x + openMapFiles.Count * (150 + 10)) > Screen.width - 10 ? (Screen.width - x - 5) / openMapFiles.Count - 10 : 150;
            float h = 20;

            Rect workArea = GUILayoutUtility.GetRect(10, 10000, 10, 30);

            for (int i = 0; i < openMapFiles.Count; i++)
            {
                d.drawTabButton(new Rect(workArea.x + x, workArea.y + y, w, h), openMapFiles[i].fileName, true, activeFileIndex == i ? Color.white : Color.black, activeFileIndex == i ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.9f, 0.9f, 0.9f), activeFileIndex == i ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.2f, 0.2f, 0.2f), new FuncOpener(() => { activeFileIndex = i; Repaint(); }));
                x += w + 10;
            }
        }
    }
    private void StatusGUI()
    {
        EditorGUI.DrawRect(new Rect(0, Screen.height - 45, Screen.width, 30), new Color(0.9411764705882353f, 0.9411764705882353f, 0.9411764705882353f));
        Handles.color = Color.black;
        Handles.DrawLine(new Vector2(0, Screen.height - 47), new Vector2(Screen.width, Screen.height - 47));
        Handles.color = new Color(0.8901960784313725f, 0.8901960784313725f, 0.8901960784313725f);
        Handles.DrawLine(new Vector2(0, Screen.height - 46), new Vector2(Screen.width, Screen.height - 46));
        Handles.color = Color.white;
        Handles.DrawLine(new Vector2(0, Screen.height - 45), new Vector2(Screen.width, Screen.height - 45));

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("W=" + openMapFiles[activeFileIndex].mapSizeX + " H=" + openMapFiles[activeFileIndex].mapSizeY + " / " + (openMapFiles[activeFileIndex].gridSize * 2) + "% / X=" + mouseX + " Y=" + mouseY + " / " + status, GUILayout.Width(400));
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal(GUILayout.Width(300));
        GUILayout.Label("カメラ表示範囲 : ", GUILayout.Width(90));
        GUILayout.Label("X");
        openMapFiles[activeFileIndex].viewX = EditorGUILayout.IntField(openMapFiles[activeFileIndex].viewX, GUILayout.Width(30));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Y");
        openMapFiles[activeFileIndex].viewY = EditorGUILayout.IntField(openMapFiles[activeFileIndex].viewY, GUILayout.Width(30));
        GUILayout.FlexibleSpace();
        GUILayout.Label("W");
        openMapFiles[activeFileIndex].viewW = EditorGUILayout.IntField(openMapFiles[activeFileIndex].viewW, GUILayout.Width(30));
        GUILayout.FlexibleSpace();
        GUILayout.Label("H");
        openMapFiles[activeFileIndex].viewH = EditorGUILayout.IntField(openMapFiles[activeFileIndex].viewH, GUILayout.Width(30));
        GUILayout.FlexibleSpace();
        if (openMapFiles[activeFileIndex].viewX + openMapFiles[activeFileIndex].viewW > openMapFiles[activeFileIndex].mapSizeX)
        {
            if (openMapFiles[activeFileIndex].viewX > openMapFiles[activeFileIndex].mapSizeX - 1)
            {
                openMapFiles[activeFileIndex].viewX = openMapFiles[activeFileIndex].mapSizeX - 1;
                openMapFiles[activeFileIndex].viewW = 1;
            }
            else
            {
                openMapFiles[activeFileIndex].viewW = openMapFiles[activeFileIndex].mapSizeX - openMapFiles[activeFileIndex].viewX;
            }
        }
        if (openMapFiles[activeFileIndex].viewY + openMapFiles[activeFileIndex].viewH > openMapFiles[activeFileIndex].mapSizeY)
        {
            if (openMapFiles[activeFileIndex].viewY > openMapFiles[activeFileIndex].mapSizeY - 1)
            {
                openMapFiles[activeFileIndex].viewY = openMapFiles[activeFileIndex].mapSizeY - 1;
                openMapFiles[activeFileIndex].viewH = 1;
            }
            else
            {
                openMapFiles[activeFileIndex].viewH = openMapFiles[activeFileIndex].mapSizeY - openMapFiles[activeFileIndex].viewY;
            }
        }
        if (openMapFiles[activeFileIndex].viewW > openMapFiles[activeFileIndex].mapSizeX)
            openMapFiles[activeFileIndex].viewW = openMapFiles[activeFileIndex].mapSizeX;
        if (openMapFiles[activeFileIndex].viewH > openMapFiles[activeFileIndex].mapSizeY)
            openMapFiles[activeFileIndex].viewH = openMapFiles[activeFileIndex].mapSizeY;

        if (GUILayout.Button("リセット"))
        {
            openMapFiles[activeFileIndex].viewX = 0;
            openMapFiles[activeFileIndex].viewY = 0;
            openMapFiles[activeFileIndex].viewW = openMapFiles[activeFileIndex].mapSizeX;
            openMapFiles[activeFileIndex].viewH = openMapFiles[activeFileIndex].mapSizeY;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        if (openMapFiles[activeFileIndex].mapPrevList.Count > 0)
            openMapFiles[activeFileIndex].prevFlag = true;
        else
            openMapFiles[activeFileIndex].prevFlag = false;
        if (openMapFiles[activeFileIndex].mapNextList.Count > 0)
            openMapFiles[activeFileIndex].nextFlag = true;
        else
            openMapFiles[activeFileIndex].nextFlag = false;

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(1);
    }
    private void EventGUI()
    {
        GUILayout.Label("イベント一覧 : ");
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LEFT_W - 3), GUILayout.Height(350));
        List<string> names = new List<string>();
        for (int i = 0; i < openMapFiles[activeFileIndex].eventChips.Count; i++)
            names.Add(openMapFiles[activeFileIndex].eventChips[i].name != null ? openMapFiles[activeFileIndex].eventChips[i].name : i.ToString());
        if (!menuOpenFlag)
        {
            openMapFiles[activeFileIndex].selectEventIndex = sb.Show(new Rect(0, 0, LEFT_W - 30, openMapFiles[activeFileIndex].eventChips.Count * 20), openMapFiles[activeFileIndex].selectEventIndex, names.ToArray(), new FuncOpener(Repaint), new FuncSelectBoxOpener((int id) =>
            {
                if (id != -1)
                    openMapFiles[activeFileIndex].selectVec = new Vector2(openMapFiles[activeFileIndex].eventChips[id].x, openMapFiles[activeFileIndex].eventChips[id].y);

                Repaint();
            }
            ), new FuncSelectBoxOpener((int id) =>
            {
                evWindow = MapEditorEventWindow.WillAppear(this);
                evWindow.selectID = id;
            }));
        }
        else
            GUILayout.Space(340);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        if (openMapFiles[activeFileIndex].eventChips != null && openMapFiles[activeFileIndex].selectEventIndex >= 0 && openMapFiles[activeFileIndex].eventChips.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("名前 : ");
            openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].name = EditorGUILayout.TextField(openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].name != null ? openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].name : openMapFiles[activeFileIndex].selectEventIndex.ToString());
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("起動条件 : ");
            openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].mode = EditorGUILayout.Popup(openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].mode, new string[] { "自動実行", "プレイヤー接触", "決定キーで実行" });
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            GUILayout.Label("判定範囲 : ");
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X");
            openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.x = EditorGUILayout.FloatField(openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.x);
            GUILayout.Label("Y");
            openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.y = EditorGUILayout.FloatField(openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.y);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("W");
            openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.width = EditorGUILayout.FloatField(openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.width);
            GUILayout.Label("H");
            openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.height = EditorGUILayout.FloatField(openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.height);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("左"))
            {
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.x = -0.5f;
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.y = 0;
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.width = 0.2f;
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.height = 0.6f;
            }
            if (GUILayout.Button("右"))
            {
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.x = 0.5f;
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.y = 0;
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.width = 0.2f;
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.height = 0.6f;
            }
            if (GUILayout.Button("上"))
            {
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.x = 0f;
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.y = -0.5f;
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.width = 0.6f;
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.height = 0.2f;
            }
            if (GUILayout.Button("下"))
            {
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.x = 0f;
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.y = 0.5f;
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.width = 0.6f;
                openMapFiles[activeFileIndex].eventChips[openMapFiles[activeFileIndex].selectEventIndex].rect.height = 0.2f;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }
    private void BgGUI()
    {
        openMapFiles[activeFileIndex].oldBg.mode = openMapFiles[activeFileIndex].bg.mode;
        openMapFiles[activeFileIndex].oldBg.objectSize = openMapFiles[activeFileIndex].bg.objectSize;
        openMapFiles[activeFileIndex].oldBg.background = openMapFiles[activeFileIndex].bg.background;
        openMapFiles[activeFileIndex].oldBg.backcolor = openMapFiles[activeFileIndex].bg.backcolor;
        openMapFiles[activeFileIndex].oldBg.loopXFlag = openMapFiles[activeFileIndex].bg.loopXFlag;
        openMapFiles[activeFileIndex].oldBg.loopYFlag = openMapFiles[activeFileIndex].bg.loopYFlag;
        openMapFiles[activeFileIndex].oldBg.foldouts = new FoldOut[openMapFiles[activeFileIndex].bg.objectSize];

        for (int i = 0; i < openMapFiles[activeFileIndex].bg.objectSize; i++)
        {
            if (openMapFiles[activeFileIndex].oldBg.foldouts[i] == null)
                openMapFiles[activeFileIndex].oldBg.foldouts[i] = new FoldOut();

            if (openMapFiles[activeFileIndex].bg.foldouts[i] == null)
                continue;

            openMapFiles[activeFileIndex].oldBg.foldouts[i].foldout = openMapFiles[activeFileIndex].bg.foldouts[i].foldout;
            openMapFiles[activeFileIndex].oldBg.foldouts[i].obj = openMapFiles[activeFileIndex].bg.foldouts[i].obj;
            openMapFiles[activeFileIndex].oldBg.foldouts[i].objectIsX = openMapFiles[activeFileIndex].bg.foldouts[i].objectIsX;
            openMapFiles[activeFileIndex].oldBg.foldouts[i].objectIsY = openMapFiles[activeFileIndex].bg.foldouts[i].objectIsY;
            openMapFiles[activeFileIndex].oldBg.foldouts[i].objectLoopX = openMapFiles[activeFileIndex].bg.foldouts[i].objectLoopX;
            openMapFiles[activeFileIndex].oldBg.foldouts[i].objectLoopY = openMapFiles[activeFileIndex].bg.foldouts[i].objectLoopY;
            openMapFiles[activeFileIndex].oldBg.foldouts[i].objectX = openMapFiles[activeFileIndex].bg.foldouts[i].objectX;
            openMapFiles[activeFileIndex].oldBg.foldouts[i].objectY = openMapFiles[activeFileIndex].bg.foldouts[i].objectY;
        }

        EditorGUILayout.BeginVertical();
        openMapFiles[activeFileIndex].scrollPos2 = EditorGUILayout.BeginScrollView(openMapFiles[activeFileIndex].scrollPos2);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("モード : ", GUILayout.Width(LABEL_W));
        openMapFiles[activeFileIndex].bg.mode = EditorGUILayout.Popup(openMapFiles[activeFileIndex].bg.mode, new string[] { "背景ループ", "オブジェクト別ループ" });
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        openMapFiles[activeFileIndex].bg.background = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField("背景 : ", AssetDatabase.LoadAssetAtPath(openMapFiles[activeFileIndex].bg.background, typeof(Texture2D)), typeof(Texture2D), false));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        openMapFiles[activeFileIndex].bg.backcolor = EditorGUILayout.ColorField("背景色 : ", openMapFiles[activeFileIndex].bg.backcolor);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("ループモード : ", GUILayout.Width(LABEL_W));
        GUILayout.Label("X座標");
        openMapFiles[activeFileIndex].bg.loopXFlag = EditorGUILayout.Toggle(openMapFiles[activeFileIndex].bg.loopXFlag);
        GUILayout.Label("Y座標");
        openMapFiles[activeFileIndex].bg.loopYFlag = EditorGUILayout.Toggle(openMapFiles[activeFileIndex].bg.loopYFlag);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        if (openMapFiles[activeFileIndex].bg.mode == 1)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("オブジェクト数 : ", GUILayout.Width(LABEL_W));
            openMapFiles[activeFileIndex].bg.objectSize = EditorGUILayout.IntField(openMapFiles[activeFileIndex].bg.objectSize);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (openMapFiles[activeFileIndex].bg.objectSize != openMapFiles[activeFileIndex].oldBg.objectSize)
            {
                if (openMapFiles[activeFileIndex].bg.objectSize >= 100)
                    openMapFiles[activeFileIndex].bg.objectSize = 100;

                FoldOut[] oldFold = openMapFiles[activeFileIndex].bg.foldouts;
                openMapFiles[activeFileIndex].bg.foldouts = new FoldOut[openMapFiles[activeFileIndex].bg.objectSize];

                for (int i = 0; i < openMapFiles[activeFileIndex].bg.objectSize; i++)
                {
                    if (oldFold != null)
                    {
                        if (i < oldFold.Length)
                        {
                            if (oldFold[i] != null)
                            {
                                openMapFiles[activeFileIndex].bg.foldouts[i] = oldFold[i];
                                continue;
                            }
                        }
                    }

                    openMapFiles[activeFileIndex].bg.foldouts[i] = new FoldOut();
                }
            }

            if (openMapFiles[activeFileIndex].bg.foldouts != null)
            {
                for (int i = 0; i < openMapFiles[activeFileIndex].bg.objectSize; i++)
                {
                    if (openMapFiles[activeFileIndex].bg.foldouts[i] != null)
                    {
                        if (openMapFiles[activeFileIndex].bg.foldouts[i].foldout = EditorGUILayout.Foldout(openMapFiles[activeFileIndex].bg.foldouts[i].foldout, "Object [" + i + "]"))
                        {
                            EditorGUILayout.BeginHorizontal();
                            openMapFiles[activeFileIndex].bg.foldouts[i].obj = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField("オブジェクト : ", AssetDatabase.LoadAssetAtPath(openMapFiles[activeFileIndex].bg.foldouts[i].obj, typeof(Texture2D)), typeof(Texture2D), false));
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.Space();

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label("ループモード : ", GUILayout.Width(LABEL_W));
                            GUILayout.Label("X座標");
                            openMapFiles[activeFileIndex].bg.foldouts[i].objectIsX = EditorGUILayout.Toggle(openMapFiles[activeFileIndex].bg.foldouts[i].objectIsX);
                            GUILayout.Label("Y座標");
                            openMapFiles[activeFileIndex].bg.foldouts[i].objectIsY = EditorGUILayout.Toggle(openMapFiles[activeFileIndex].bg.foldouts[i].objectIsY);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.Space();

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label("X座標 : ", GUILayout.Width(LABEL_W));
                            openMapFiles[activeFileIndex].bg.foldouts[i].objectX = EditorGUILayout.FloatField(openMapFiles[activeFileIndex].bg.foldouts[i].objectX);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.Space();

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label("Y座標 : ", GUILayout.Width(LABEL_W));
                            openMapFiles[activeFileIndex].bg.foldouts[i].objectY = EditorGUILayout.FloatField(openMapFiles[activeFileIndex].bg.foldouts[i].objectY);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.Space();

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label("ループの間隔 : ", GUILayout.Width(LABEL_W));
                            GUILayout.Label("X座標");
                            openMapFiles[activeFileIndex].bg.foldouts[i].objectLoopX = EditorGUILayout.FloatField(openMapFiles[activeFileIndex].bg.foldouts[i].objectLoopX);
                            GUILayout.Label("Y座標");
                            openMapFiles[activeFileIndex].bg.foldouts[i].objectLoopY = EditorGUILayout.FloatField(openMapFiles[activeFileIndex].bg.foldouts[i].objectLoopY);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.Space();
                        }
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }
    private void SelectChipBox()
    {
        float x = 0.0f;
        float y = 0.0f;
        float x2 = 0.0f;
        float y2 = 0.0f;
        float w = 30.0f;
        float h = 30.0f;
        float winMaxW = LEFT_W - 30;
        float maxW = winMaxW - 20;

        GUILayout.Label("マップチップ : ");
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LEFT_W - 3));
        Rect workArea = GUILayoutUtility.GetRect(10, 10000, 10, 200);
        openMapFiles[activeFileIndex].chipSelectBoxScrollPos = GUI.BeginScrollView(workArea, openMapFiles[activeFileIndex].chipSelectBoxScrollPos, new Rect(0, 0, winMaxW, h * (mapChipList.Count / (maxW / h))), false, true);

        string path = "Assets/Editor/MapEditor/start.png";
        Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        GUILayout.BeginArea(new Rect(x, y, w, h));
        if (GUILayout.Button(tex, GUILayout.MaxWidth(w), GUILayout.MaxHeight(h), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
        {
            openMapFiles[activeFileIndex].selectedLeftImagePath = path;
        }
        GUILayout.EndArea();
        x += w + 2;

        foreach (GameObject d in mapChipList)
        {
            if (d == null)
                continue;

            if (x > maxW)
            {
                x = 0.0f;
                y += h + 2;
            }

            string texPath = AssetDatabase.GetAssetPath(d.GetComponent<SpriteRenderer>().sprite);
            tex = (Texture2D)AssetDatabase.LoadAssetAtPath(texPath, typeof(Texture2D));

            GUILayout.BeginArea(new Rect(x, y, w, h));
            EditorGUI.BeginDisabledGroup(menuOpenFlag);
            if (GUILayout.Button(tex, GUILayout.MaxWidth(w), GUILayout.MaxHeight(h), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
            {
                openMapFiles[activeFileIndex].selectedLeftImagePath = AssetDatabase.GetAssetPath(d) + "|MapChip";
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndArea();
            x += w + 2;
        }

        GUI.EndScrollView();
        EditorGUILayout.EndVertical();

        GUILayout.Label("オブジェクト : ");
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LEFT_W - 3));
        workArea = GUILayoutUtility.GetRect(10, 10000, 10, 200);
        openMapFiles[activeFileIndex].objectSelectBoxScrollPos = GUI.BeginScrollView(workArea, openMapFiles[activeFileIndex].objectSelectBoxScrollPos, new Rect(0, 0, winMaxW, h * (mapObjectList.Count / (maxW / h))), false, true);

        foreach (GameObject d in mapObjectList)
        {
            if (x2 > maxW)
            {
                x2 = 0.0f;
                y2 += h + 4;
            }

            string texPath = AssetDatabase.GetAssetPath(d.GetComponent<SpriteRenderer>().sprite);
            tex = (Texture2D)AssetDatabase.LoadAssetAtPath(texPath, typeof(Texture2D));

            GUILayout.BeginArea(new Rect(x2, y2, w, h));
            EditorGUI.BeginDisabledGroup(menuOpenFlag);
            if (GUILayout.Button(tex, GUILayout.MaxWidth(w), GUILayout.MaxHeight(h), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
            {
                openMapFiles[activeFileIndex].selectedLeftImagePath = AssetDatabase.GetAssetPath(d) + "|MapObject";
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndArea();
            x2 += w + 4;
        }

        GUI.EndScrollView();
        EditorGUILayout.EndVertical();
    }
    private void DrawSelectedImage()
    {
        string selectedImagePath = openMapFiles[activeFileIndex].selectedLeftImagePath;

		if (selectedImagePath != null && selectedImagePath != "")
        {
            string[] stus = selectedImagePath.Split('|');
			GUILayout.Label("選択チップ : " + (stus[0].IndexOf("start.png") > -1 ? "プレイヤーのスタート位置" : stus[0].Split('/')[stus[0].Split('/').Length - 1]));
            EditorGUILayout.BeginHorizontal();
            Texture2D tex;

            if (stus[0] == "")
                stus[0] = "Assets/Editor/MapEditor/none.png";

            if (stus[0].IndexOf("none") > -1 || stus[0].IndexOf("start") > -1)
                tex = (Texture2D)AssetDatabase.LoadAssetAtPath(stus[0], typeof(Texture2D));
            else
                tex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath((AssetDatabase.LoadAssetAtPath(stus[0], typeof(GameObject)) as GameObject).GetComponent<SpriteRenderer>().sprite), typeof(Texture2D));

            GUILayout.Box(tex);

            EditorGUILayout.EndHorizontal();
		}
        else
        {
            Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Editor/MapEditor/none.png", typeof(Texture2D));
            GUILayout.Label("選択チップ : ");
            GUILayout.Box(tex);
        }
    }
    private void VarSetting()
    {
        varWindow = MapEditorVarSettingWindow.WillAppear(this);
    }
    private void DrawMenuButton()
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);
        GUIStyleState state = new GUIStyleState();
        state.textColor = new Color(0.2f, 0.2f, 0.2f);
        style.normal = state;
        Texture2D menuTex = AssetDatabase.LoadAssetAtPath("Assets/Editor/MapEditor/menu_bg.png", typeof(Texture2D)) as Texture2D;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, 23), menuTex);

        fileOpenFlag = GUILayout.Toggle(fileOpenFlag, "ファイル", style);
        if (fileOpenFlag)
        {
            menuOpenFlag = true;
            editOpenFlag = false;
            helpOpenFlag = false;
        }

        editOpenFlag = GUILayout.Toggle(editOpenFlag, "編集", style);
        if (editOpenFlag)
        {
            menuOpenFlag = true;
            fileOpenFlag = false;
            helpOpenFlag = false;
        }

        helpOpenFlag = GUILayout.Toggle(helpOpenFlag, "その他", style);
        if (helpOpenFlag)
        {
            menuOpenFlag = true;
            fileOpenFlag = false;
            editOpenFlag = false;
        }

        if (!fileOpenFlag && !editOpenFlag && !helpOpenFlag || newFileWindow != null)
            menuOpenFlag = false;
    }
    private void MenuDrawer(float x, float y, float w, string[] items, int fontsize, params FuncOpener[] func)
    {
        int count = 0;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == "-")
                count++;
        }

        Rect rect = new Rect(x, y, w, items.Length * (fontsize + 12) - count * 13 + 8);
        EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), new Color(0, 0, 0, 0.1f));
        EditorGUI.DrawRect(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), new Color(0, 0, 0, 0.1f));
        EditorGUI.DrawRect(new Rect(rect.x + 3, rect.y + 3, rect.width, rect.height), new Color(0, 0, 0, 0.1f));
        EditorGUI.DrawRect(rect, new Color(0.9411764705882353f, 0.9411764705882353f, 0.9411764705882353f));

        Vector2 pos = Event.current.mousePosition;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        GUIStyleState state = new GUIStyleState();
        state.textColor = new Color(0.2f, 0.2f, 0.2f);
        style.fontSize = fontsize;
        style.normal = state;

        Handles.color = new Color(0.8784313725490196f, 0.8784313725490196f, 0.8784313725490196f);
        Handles.DrawLine(new Vector2(rect.x + 24, rect.y), new Vector2(rect.x + 24, rect.y + rect.height));
        Handles.color = Color.white;
        Handles.DrawLine(new Vector2(rect.x + 25, rect.y), new Vector2(rect.x + 25, rect.y + rect.height));

        if (!(rect.x < pos.x &&
            rect.x + rect.width > pos.x &&
            rect.y < pos.y &&
            rect.y + rect.height > pos.y))
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown)
                menuOpenFlag = false;
        }

        int menuCount = 0;
        float menuHR = 0;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == "-")
                menuHR += 10;

            bool hidden = false;

            if (items[i].IndexOf("@") > -1)
            {
                hidden = true;
                state.textColor = new Color(0.6f, 0.6f, 0.6f);
                style.normal = state;
            }
            else
            {
                state.textColor = new Color(0.2f, 0.2f, 0.2f);
                style.normal = state;
            }

            Rect buttonRect = new Rect(x + 2, y + menuCount * (fontsize + 12) + 5 + menuHR, w - 4, fontsize + 10);

            if (items[i] == "-")
            {
                menuCount--;
                Handles.color = new Color(0.8784313725490196f, 0.8784313725490196f, 0.8784313725490196f);
                Handles.DrawLine(new Vector2(buttonRect.x + 25, buttonRect.y - 5), new Vector2(buttonRect.x + buttonRect.width, buttonRect.y - 5));
                Handles.color = Color.white;
                Handles.DrawLine(new Vector2(buttonRect.x + 25, buttonRect.y - 4), new Vector2(buttonRect.x + buttonRect.width, buttonRect.y - 4));
            }
            else
            {
                if (buttonRect.x < pos.x &&
                    buttonRect.x + buttonRect.width > pos.x &&
                    buttonRect.y < pos.y &&
                    buttonRect.y + buttonRect.height > pos.y)
                {
                    EditorGUI.DrawRect(buttonRect, new Color(0.78f, 0.83f, 0.92f, 0.2f));
                    Handles.color = new Color(0.6862745098039216f, 0.8156862745098039f, 0.9686274509803922f);
                    Handles.DrawLine(new Vector2(buttonRect.x + 2, buttonRect.y), new Vector2(buttonRect.x - 2 + buttonRect.width, buttonRect.y));
                    Handles.DrawLine(new Vector2(buttonRect.x + 2, buttonRect.y + buttonRect.height), new Vector2(buttonRect.x - 2 + buttonRect.width, buttonRect.y + buttonRect.height));
                    Handles.DrawLine(new Vector2(buttonRect.x, buttonRect.y + 2), new Vector2(buttonRect.x, buttonRect.y + buttonRect.height - 2));
                    Handles.DrawLine(new Vector2(buttonRect.x + buttonRect.width, buttonRect.y + 2), new Vector2(buttonRect.x + buttonRect.width, buttonRect.y + buttonRect.height - 2));
                    Handles.DrawLine(new Vector2(buttonRect.x + 2, buttonRect.y), new Vector2(buttonRect.x, buttonRect.y + 2));
                    Handles.DrawLine(new Vector2(buttonRect.x + 3, buttonRect.y), new Vector2(buttonRect.x, buttonRect.y + 3));
                    Handles.DrawLine(new Vector2(buttonRect.x + 4, buttonRect.y), new Vector2(buttonRect.x, buttonRect.y + 4));
                    Handles.DrawLine(new Vector2(buttonRect.x + 2, buttonRect.y + buttonRect.height), new Vector2(buttonRect.x, buttonRect.y - 2 + buttonRect.height));
                    Handles.DrawLine(new Vector2(buttonRect.x + 3, buttonRect.y + buttonRect.height), new Vector2(buttonRect.x, buttonRect.y - 3 + buttonRect.height));
                    Handles.DrawLine(new Vector2(buttonRect.x + 4, buttonRect.y + buttonRect.height), new Vector2(buttonRect.x, buttonRect.y - 4 + buttonRect.height));
                    Handles.DrawLine(new Vector2(buttonRect.x - 2 + buttonRect.width, buttonRect.y), new Vector2(buttonRect.x + buttonRect.width, buttonRect.y + 2));
                    Handles.DrawLine(new Vector2(buttonRect.x - 3 + buttonRect.width, buttonRect.y), new Vector2(buttonRect.x + buttonRect.width, buttonRect.y + 3));
                    Handles.DrawLine(new Vector2(buttonRect.x - 4 + buttonRect.width, buttonRect.y), new Vector2(buttonRect.x + buttonRect.width, buttonRect.y + 4));
                    Handles.DrawLine(new Vector2(buttonRect.x - 2 + buttonRect.width, buttonRect.y + buttonRect.height), new Vector2(buttonRect.x + buttonRect.width, buttonRect.y - 2 + buttonRect.height));
                    Handles.DrawLine(new Vector2(buttonRect.x - 3 + buttonRect.width, buttonRect.y + buttonRect.height), new Vector2(buttonRect.x + buttonRect.width, buttonRect.y - 3 + buttonRect.height));
                    Handles.DrawLine(new Vector2(buttonRect.x - 4 + buttonRect.width, buttonRect.y + buttonRect.height), new Vector2(buttonRect.x + buttonRect.width, buttonRect.y - 4 + buttonRect.height));
                }

                Rect btRect = new Rect(buttonRect.x, buttonRect.y + 2, buttonRect.width, buttonRect.height);

                GUI.Label(new Rect(btRect.x + 30, btRect.y, btRect.width - 30, btRect.height), items[i].Replace("@", ""), style);

                if (GUI.Button(btRect, "", style))
                {
                    if (!(items[i].IndexOf("@") > -1))
                        menuOpenFlag = false;

                    if (!hidden)
                    {
                        if (func != null && func.Length > menuCount && func[menuCount] != null)
                            func[menuCount]();
                    }
                }
            }

            menuCount++;
        }
    }
    private void DrawTabButton()
    {
        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();

        EditorGUI.DrawRect(new Rect(0, 20, Screen.width, 35), new Color(0.6352941176470588f, 0.6352941176470588f, 0.6352941176470588f));
        Handles.color = new Color(0.3f, 0.3f, 0.3f);
        Handles.DrawLine(new Vector2(0, 20 + 35), new Vector2(Screen.width, 20 + 35));

        d.drawTabButton(new Rect(5, 28, 100, 20), "マップ", !menuOpenFlag && MapOpenFlag, !MapOpenFlag ? new Color(0, 0, 0, 0.5f) : openMapFiles[activeFileIndex].mode == 0 ? Color.white : Color.black, !MapOpenFlag ? new Color(0.9f, 0.9f, 0.9f, 0.5f) : openMapFiles[activeFileIndex].mode == 0 ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.9f, 0.9f, 0.9f), !MapOpenFlag ? new Color(0.2f, 0.2f, 0.2f, 0.5f) : openMapFiles[activeFileIndex].mode == 0 ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.2f, 0.2f, 0.2f), new FuncOpener(() => { openMapFiles[activeFileIndex].mode = 0; Repaint(); }));
        d.drawTabButton(new Rect(100 + 5 * 2, 28, 100, 20), "イベント", !menuOpenFlag && MapOpenFlag, !MapOpenFlag ? new Color(0, 0, 0, 0.5f) : openMapFiles[activeFileIndex].mode == 1 ? Color.white : Color.black, !MapOpenFlag ? new Color(0.9f, 0.9f, 0.9f, 0.5f) : openMapFiles[activeFileIndex].mode == 1 ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.9f, 0.9f, 0.9f), !MapOpenFlag ? new Color(0.2f, 0.2f, 0.2f, 0.5f) : openMapFiles[activeFileIndex].mode == 1 ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.2f, 0.2f, 0.2f), new FuncOpener(() => { openMapFiles[activeFileIndex].mode = 1; Repaint(); }));
        d.drawTabButton(new Rect(100 * 2 + 5 * 3, 28, 100, 20), "背景", !menuOpenFlag && MapOpenFlag, !MapOpenFlag ? new Color(0, 0, 0, 0.5f) : openMapFiles[activeFileIndex].mode == 2 ? Color.white : Color.black, !MapOpenFlag ? new Color(0.9f, 0.9f, 0.9f, 0.5f) : openMapFiles[activeFileIndex].mode == 2 ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.9f, 0.9f, 0.9f), !MapOpenFlag ? new Color(0.2f, 0.2f, 0.2f, 0.5f) : openMapFiles[activeFileIndex].mode == 2 ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.2f, 0.2f, 0.2f), new FuncOpener(() => { openMapFiles[activeFileIndex].mode = 2; Repaint(); }));

        System.Action<Rect, string, bool, FuncOpener> drawToolButton = (Rect rect, string path, bool flag, FuncOpener func) =>
        {
            GUILayout.BeginArea(rect);
            EditorGUI.BeginDisabledGroup(flag);
            Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
            if (GUILayout.Button(tex, GUILayout.MaxWidth(rect.width - 5), GUILayout.MaxHeight(rect.height - 5), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
            {
                func();
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndArea();
        };

        float x = 330;
        float y = 25;
        float w = 25;
        float h = 25;
        float m = 5;

        drawToolButton(new Rect(x, y, w + m, h + m), "Assets/Editor/MapEditor/zoom_in.png", !MapOpenFlag || openMapFiles[activeFileIndex].gridSize > 99, new FuncOpener(ZoomIn));
        drawToolButton(new Rect(x + w + m, y, w + m, h + m), "Assets/Editor/MapEditor/zoom_out.png", !MapOpenFlag || openMapFiles[activeFileIndex].gridSize < 6, new FuncOpener(ZoomOut));
        drawToolButton(new Rect(x + (w + m) * 2, y, w + m, h + m), "Assets/Editor/MapEditor/undo.png", !MapOpenFlag || (!openMapFiles[activeFileIndex].prevFlag || openMapFiles[activeFileIndex].mode != 0), new FuncOpener(Undo));
        drawToolButton(new Rect(x + (w + m) * 3, y, w + m, h + m), "Assets/Editor/MapEditor/redo.png", !MapOpenFlag || (!openMapFiles[activeFileIndex].nextFlag || openMapFiles[activeFileIndex].mode != 0), new FuncOpener(Redo));

        GUILayout.FlexibleSpace();

        if (MapOpenFlag)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            GUIStyleState state = new GUIStyleState();
            state.textColor = Color.white;
            style.normal = state;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 14;
            GUILayout.Label(openMapFiles[activeFileIndex].fileName, style);
            GUILayout.Space(40);

            Rect rec = new Rect(Screen.width - 25, 29, 17, 17);
            GUI.DrawTexture(rec, (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Editor/MapEditor/close.png", typeof(Texture2D)));
            if (GUI.Button(rec, "", GUI.skin.label))
            {
                FileExit();
            }
        }

        EditorGUILayout.EndHorizontal();

        if (MapOpenFlag)
            GUILayout.Space(-20);
    }
    private void Undo()
    {
        openMapFiles[activeFileIndex].mapNextIndexList.Add(new MapSizeIndex(openMapFiles[activeFileIndex].map.GetLength(1), openMapFiles[activeFileIndex].map.GetLength(0)));

        openMapFiles[activeFileIndex].mapNextList.Add((string[,])openMapFiles[activeFileIndex].map.Clone());
        openMapFiles[activeFileIndex].mapSizeY = openMapFiles[activeFileIndex].mapPrevIndexList[openMapFiles[activeFileIndex].mapPrevIndexList.Count - 1].MapSizeY;
        openMapFiles[activeFileIndex].mapSizeX = openMapFiles[activeFileIndex].mapPrevIndexList[openMapFiles[activeFileIndex].mapPrevIndexList.Count - 1].MapSizeX;
        openMapFiles[activeFileIndex].viewX = 0;
        openMapFiles[activeFileIndex].viewY = 0;
        openMapFiles[activeFileIndex].viewW = openMapFiles[activeFileIndex].mapSizeX;
        openMapFiles[activeFileIndex].viewH = openMapFiles[activeFileIndex].mapSizeY;
        string[,] oldMap = (string[,])openMapFiles[activeFileIndex].mapPrevList[openMapFiles[activeFileIndex].mapPrevList.Count - 1].Clone();
        string[,] newMap = new string[openMapFiles[activeFileIndex].mapSizeY, openMapFiles[activeFileIndex].mapSizeX];
        bool flag = newMap.Length > oldMap.Length;

        for (int i = 0; i < (flag ? newMap.GetLength(0) : oldMap.GetLength(0)); i++)
        {
            for (int j = 0; j < (flag ? newMap.GetLength(1) : oldMap.GetLength(1)); j++)
            {
                if (flag)
                    newMap[i, j] = "";

                if ((flag ? oldMap.GetLength(1) > j && oldMap.GetLength(0) > i : newMap.GetLength(1) > j && newMap.GetLength(0) > i))
                    newMap[i, j] = oldMap[i, j];
            }
        }
        openMapFiles[activeFileIndex].map = (string[,])newMap.Clone();

        GridSizeUpdater();

        openMapFiles[activeFileIndex].mapPrevList.RemoveAt(openMapFiles[activeFileIndex].mapPrevList.Count - 1);

        openMapFiles[activeFileIndex].mapNextSaveFlagList.Add(openMapFiles[activeFileIndex].saveFlag);
        openMapFiles[activeFileIndex].saveFlag = openMapFiles[activeFileIndex].mapPrevSaveFlagList[openMapFiles[activeFileIndex].mapPrevSaveFlagList.Count - 1];
        openMapFiles[activeFileIndex].mapPrevSaveFlagList.RemoveAt(openMapFiles[activeFileIndex].mapPrevSaveFlagList.Count - 1);

        openMapFiles[activeFileIndex].mapPrevIndexList.RemoveAt(openMapFiles[activeFileIndex].mapPrevIndexList.Count - 1);

        if (openMapFiles[activeFileIndex].mapPrevList.Count > 0)
            openMapFiles[activeFileIndex].prevFlag = true;
        else
            openMapFiles[activeFileIndex].prevFlag = false;
        if (openMapFiles[activeFileIndex].mapNextList.Count > 0)
            openMapFiles[activeFileIndex].nextFlag = true;
        else
            openMapFiles[activeFileIndex].nextFlag = false;

        Repaint();
    }
    private void Redo()
    {
        openMapFiles[activeFileIndex].mapPrevIndexList.Add(new MapSizeIndex(openMapFiles[activeFileIndex].map.GetLength(1), openMapFiles[activeFileIndex].map.GetLength(0)));

        openMapFiles[activeFileIndex].mapPrevList.Add((string[,])openMapFiles[activeFileIndex].map.Clone());
        openMapFiles[activeFileIndex].mapSizeY = openMapFiles[activeFileIndex].mapNextIndexList[openMapFiles[activeFileIndex].mapNextIndexList.Count - 1].MapSizeY;
        openMapFiles[activeFileIndex].mapSizeX = openMapFiles[activeFileIndex].mapNextIndexList[openMapFiles[activeFileIndex].mapNextIndexList.Count - 1].MapSizeX;
        openMapFiles[activeFileIndex].viewX = 0;
        openMapFiles[activeFileIndex].viewY = 0;
        openMapFiles[activeFileIndex].viewW = openMapFiles[activeFileIndex].mapSizeX;
        openMapFiles[activeFileIndex].viewH = openMapFiles[activeFileIndex].mapSizeY;
        string[,] oldMap = (string[,])openMapFiles[activeFileIndex].mapNextList[openMapFiles[activeFileIndex].mapNextList.Count - 1].Clone();
        string[,] newMap = new string[openMapFiles[activeFileIndex].mapSizeY, openMapFiles[activeFileIndex].mapSizeX];

        bool flag = newMap.Length > oldMap.Length;

        for (int i = 0; i < (flag ? newMap.GetLength(0) : oldMap.GetLength(0)); i++)
        {
            for (int j = 0; j < (flag ? newMap.GetLength(1) : oldMap.GetLength(1)); j++)
            {
                if (flag)
                    newMap[i, j] = "";

                if ((flag ? oldMap.GetLength(1) > j && oldMap.GetLength(0) > i : newMap.GetLength(1) > j && newMap.GetLength(0) > i))
                    newMap[i, j] = oldMap[i, j];
            }
        }
        openMapFiles[activeFileIndex].map = (string[,])newMap.Clone();

        GridSizeUpdater();

        openMapFiles[activeFileIndex].mapNextList.RemoveAt(openMapFiles[activeFileIndex].mapNextList.Count - 1);

        openMapFiles[activeFileIndex].mapPrevSaveFlagList.Add(openMapFiles[activeFileIndex].saveFlag);
        openMapFiles[activeFileIndex].saveFlag = openMapFiles[activeFileIndex].mapNextSaveFlagList[openMapFiles[activeFileIndex].mapNextSaveFlagList.Count - 1];
        openMapFiles[activeFileIndex].mapNextSaveFlagList.RemoveAt(openMapFiles[activeFileIndex].mapNextSaveFlagList.Count - 1);

        openMapFiles[activeFileIndex].mapNextIndexList.RemoveAt(openMapFiles[activeFileIndex].mapNextIndexList.Count - 1);

        if (openMapFiles[activeFileIndex].mapPrevList.Count > 0)
            openMapFiles[activeFileIndex].prevFlag = true;
        else
            openMapFiles[activeFileIndex].prevFlag = false;
        if (openMapFiles[activeFileIndex].mapNextList.Count > 0)
            openMapFiles[activeFileIndex].nextFlag = true;
        else
            openMapFiles[activeFileIndex].nextFlag = false;

        Repaint();
    }
    private void ZoomIn()
    {
        openMapFiles[activeFileIndex].gridSize += 5;
        if (openMapFiles[activeFileIndex].gridSize > 100)
            openMapFiles[activeFileIndex].gridSize = 100;

        GridSizeUpdater();
        Repaint();
    }
    private void ZoomOut()
    {
        openMapFiles[activeFileIndex].gridSize -= 5;
        if (openMapFiles[activeFileIndex].gridSize < 5)
        {
            openMapFiles[activeFileIndex].gridSize = 5;
        }

        GridSizeUpdater();
        Repaint();
    }
    private Rect[,] CreateGrid(int divY, int divX)
    {
        int sizeW = divX;
        int sizeH = divY;

        float x = 0.0f;
        float y = 0.0f;
        float w = openMapFiles[activeFileIndex].gridSize;
        float h = openMapFiles[activeFileIndex].gridSize;

        Rect[,] resultRects = new Rect[sizeH, sizeW];

        for (int yy = 0; yy < sizeH; yy++)
        {
            x = 0.0f;
            for (int xx = 0; xx < sizeW; xx++)
            {
                Rect r = new Rect(new Vector2(x, y), new Vector2(w, h));
                resultRects[yy, xx] = r;
                x += w;
            }
            y += h;
        }

        return resultRects;
    }
    private void OutputFile()
    {
        // マップ
        bool flag = false;

        for (int y = 0; y < openMapFiles[activeFileIndex].mapSizeY; y++)
        {
            for (int x = 0; x < openMapFiles[activeFileIndex].mapSizeX; x++)
            {
                if (openMapFiles[activeFileIndex].map[y, x].IndexOf("start") > -1)
                {
                    flag = true;
                }
            }
        }

        if (!flag)
        {
            EditorUtility.DisplayDialog("MapEditor エラー", "スタート位置が設定されていません！", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel("select file", defaultDirectory, openMapFiles[activeFileIndex].fileName.Replace(" (*)", ""), "txt");

        if (!string.IsNullOrEmpty(path))
        {
            openMapFiles[activeFileIndex].fileName = path.Split('/')[path.Split('/').Length - 1];
            openMapFiles[activeFileIndex].openPath = path;

            if (path == "" || path == null)
                return;

            if (File.Exists(path))
            {
                FileStream st = new FileStream(path, FileMode.Open);
                st.SetLength(0);
                st.Close();
            }

            MapSaveData data = new MapSaveData();

            // マップ
            data.map = new MapData();
            data.map.mapSizeX = openMapFiles[activeFileIndex].mapSizeX;
            data.map.mapSizeY = openMapFiles[activeFileIndex].mapSizeY;
            data.map.map = new string[openMapFiles[activeFileIndex].map.Length];

            int i = 0;
            for (int y = 0; y < openMapFiles[activeFileIndex].mapSizeY; y++)
            {
                for (int x = 0; x < openMapFiles[activeFileIndex].mapSizeX; x++)
                {
                    data.map.map[i] = openMapFiles[activeFileIndex].map[y, x];
                    i++;
                }
            }

            data.map.viewRect = new Rect(openMapFiles[activeFileIndex].viewX, openMapFiles[activeFileIndex].viewY, openMapFiles[activeFileIndex].viewW, openMapFiles[activeFileIndex].viewH);

            // イベント
            data.ev = new MapEventData();
            data.ev.eventChip = openMapFiles[activeFileIndex].eventChips.ToArray();

            // 背景
            data.bg = new MapBackgroundData();
            data.bg.mode = openMapFiles[activeFileIndex].bg.mode;
            data.bg.background = openMapFiles[activeFileIndex].bg.background;
            data.bg.backcolor = openMapFiles[activeFileIndex].bg.backcolor;
            data.bg.loopXFlag = openMapFiles[activeFileIndex].bg.loopXFlag;
            data.bg.loopYFlag = openMapFiles[activeFileIndex].bg.loopYFlag;
            data.bg.objectSize = openMapFiles[activeFileIndex].bg.objectSize;
            data.bg.foldouts = new FoldOut[openMapFiles[activeFileIndex].bg.objectSize];

            for (i = 0; i < openMapFiles[activeFileIndex].bg.objectSize; i++)
            {
                data.bg.foldouts[i] = openMapFiles[activeFileIndex].bg.foldouts[i];
            }

            FileInfo fileInfo = new FileInfo(path);
            StreamWriter sw = fileInfo.AppendText();
            sw.WriteLine(JsonUtility.ToJson(data));
            sw.Flush();
            sw.Close();

            try
            {
                File.Delete(path + ".meta");
            }
            catch (System.Exception exception)
            {
                Debug.Log(exception);
            }

            openMapFiles[activeFileIndex].saveFlag = false;

            EditorUtility.DisplayDialog("MapEditor", "保存が完了しました。\n" + path, "OK");
        }
    }
    private void OpenFile()
    {
        string path = EditorUtility.OpenFilePanel("select file", defaultDirectory, "txt");

        if (!string.IsNullOrEmpty(path))
        {
            int i = 0;

            if (MapOpenFlag)
            {
                bool flag = false;
                for (i = 0; i < openMapFiles.Count; i++)
                {
                    if (openMapFiles[i] != null && openMapFiles[i].openPath != null && openMapFiles[i].openPath != "" && openMapFiles[i].openPath == path)
                    {
                        flag = true;
                        break;
                    }
                }

                if (flag)
                {
                    if (openMapFiles[i].saveFlag)
                    {
                        if (!EditorUtility.DisplayDialog("MapEditor 警告", "すでに同じファイルが開かれており、変更が保存されていませんが\nファイルを開きなおしますか？\n" + path, " はい ", " いいえ "))
                        {
                            return;
                        }

                        openMapFiles.RemoveAt(i);
                    }
                    else
                    {
                        activeFileIndex = i;
                        return;
                    }
                }
            }

            StreamReader sr = new StreamReader(path, System.Text.Encoding.UTF8);
            MapSaveData data = JsonUtility.FromJson<MapSaveData>(sr.ReadToEnd());
            sr.Close();

            NewFileInit(new Vector2(data.map.mapSizeX, data.map.mapSizeY));

            openMapFiles[activeFileIndex].fileName = path.Split('/')[path.Split('/').Length - 1];
            openMapFiles[activeFileIndex].openPath = path;

            // マップ
            Rect r = data.map.viewRect;

            string[,] oldMap = (string[,])openMapFiles[activeFileIndex].map.Clone();

            openMapFiles[activeFileIndex].mapSizeX = data.map.mapSizeX;
            openMapFiles[activeFileIndex].mapSizeY = data.map.mapSizeY;
            openMapFiles[activeFileIndex].map = new string[openMapFiles[activeFileIndex].mapSizeY, openMapFiles[activeFileIndex].mapSizeX];

            i = 0;
            for (int y = 0; y < openMapFiles[activeFileIndex].mapSizeY; y++)
            {
                for (int x = 0; x < openMapFiles[activeFileIndex].mapSizeX; x++)
                {
                    openMapFiles[activeFileIndex].map[y, x] = data.map.map[i];
                    i++;
                }
            }

            openMapFiles[activeFileIndex].viewX = (int)r.x;
            openMapFiles[activeFileIndex].viewY = (int)r.y;
            openMapFiles[activeFileIndex].viewW = (int)r.width;
            openMapFiles[activeFileIndex].viewH = (int)r.height;

            openMapFiles[activeFileIndex].mapPrevIndexList.Add(new MapSizeIndex(oldMap.GetLength(1), oldMap.GetLength(0)));
            openMapFiles[activeFileIndex].mapNextIndexList.Clear();
            openMapFiles[activeFileIndex].mapPrevList.Add((string[,])oldMap.Clone());
            openMapFiles[activeFileIndex].mapNextList.Clear();
            openMapFiles[activeFileIndex].mapPrevSaveFlagList.Add(openMapFiles[activeFileIndex].saveFlag);
            openMapFiles[activeFileIndex].mapNextSaveFlagList.Clear();

            openMapFiles[activeFileIndex].gridRect = CreateGrid(openMapFiles[activeFileIndex].mapSizeY, openMapFiles[activeFileIndex].mapSizeX);

            // イベント
            openMapFiles[activeFileIndex].eventChips = new List<MapEventChip>();

            for (i = 0; i < data.ev.eventChip.Length; i++)
            {
                openMapFiles[activeFileIndex].eventChips.Add(data.ev.eventChip[i]);
            }

            // 背景
            openMapFiles[activeFileIndex].bg.mode = data.bg.mode;
            openMapFiles[activeFileIndex].bg.background = data.bg.background;
            openMapFiles[activeFileIndex].bg.backcolor = data.bg.backcolor;
            openMapFiles[activeFileIndex].bg.loopXFlag = data.bg.loopXFlag;
            openMapFiles[activeFileIndex].bg.loopYFlag = data.bg.loopYFlag;
            openMapFiles[activeFileIndex].bg.objectSize = data.bg.objectSize;

            openMapFiles[activeFileIndex].bg.foldouts = new FoldOut[openMapFiles[activeFileIndex].bg.objectSize];

            for (i = 0; i < openMapFiles[activeFileIndex].bg.objectSize; i++)
            {
                openMapFiles[activeFileIndex].bg.foldouts[i] = data.bg.foldouts[i];
            }

            openMapFiles[activeFileIndex].mapPrevList.Clear();
            openMapFiles[activeFileIndex].mapNextList.Clear();
            openMapFiles[activeFileIndex].mapPrevSaveFlagList.Clear();
            openMapFiles[activeFileIndex].mapNextSaveFlagList.Clear();
            openMapFiles[activeFileIndex].mapPrevIndexList.Clear();
            openMapFiles[activeFileIndex].mapNextIndexList.Clear();
            openMapFiles[activeFileIndex].saveFlag = false;
            openMapFiles[activeFileIndex].scrollPos.y = 50 * openMapFiles[activeFileIndex].mapSizeY;
            Focus();
            Repaint();
        }
    }
    private void Save()
    {
        // マップ
        bool flag = false;

        for (int y = 0; y < openMapFiles[activeFileIndex].mapSizeY; y++)
        {
            for (int x = 0; x < openMapFiles[activeFileIndex].mapSizeX; x++)
            {
                if (openMapFiles[activeFileIndex].map[y, x].IndexOf("start") > -1)
                {
                    flag = true;
                }
            }
        }

        if (!flag)
        {
            EditorUtility.DisplayDialog("MapEditor エラー", "スタート位置が設定されていません！", "OK");
            return;
        }

        if (openMapFiles[activeFileIndex].openPath == null || openMapFiles[activeFileIndex].openPath == "")
            return;

        string path = openMapFiles[activeFileIndex].openPath;

        openMapFiles[activeFileIndex].fileName = path.Split('/')[path.Split('/').Length - 1];

        if (path == "" || path == null)
            return;

        if (File.Exists(path))
        {
            FileStream st = new FileStream(path, FileMode.Open);
            st.SetLength(0);
            st.Close();
        }

        MapSaveData data = new MapSaveData();

        // マップ
        data.map = new MapData();
        data.map.mapSizeX = openMapFiles[activeFileIndex].mapSizeX;
        data.map.mapSizeY = openMapFiles[activeFileIndex].mapSizeY;
        data.map.map = new string[openMapFiles[activeFileIndex].map.Length];

        int i = 0;
        for (int y = 0; y < openMapFiles[activeFileIndex].mapSizeY; y++)
        {
            for (int x = 0; x < openMapFiles[activeFileIndex].mapSizeX; x++)
            {
                data.map.map[i] = openMapFiles[activeFileIndex].map[y, x];
                i++;
            }
        }

        data.map.viewRect = new Rect(openMapFiles[activeFileIndex].viewX, openMapFiles[activeFileIndex].viewY, openMapFiles[activeFileIndex].viewW, openMapFiles[activeFileIndex].viewH);

        // イベント
        data.ev = new MapEventData();
        data.ev.eventChip = openMapFiles[activeFileIndex].eventChips.ToArray();

        // 背景
        data.bg = new MapBackgroundData();
        data.bg.mode = openMapFiles[activeFileIndex].bg.mode;
        data.bg.background = openMapFiles[activeFileIndex].bg.background;
        data.bg.backcolor = openMapFiles[activeFileIndex].bg.backcolor;
        data.bg.loopXFlag = openMapFiles[activeFileIndex].bg.loopXFlag;
        data.bg.loopYFlag = openMapFiles[activeFileIndex].bg.loopYFlag;
        data.bg.objectSize = openMapFiles[activeFileIndex].bg.objectSize;
        data.bg.foldouts = new FoldOut[openMapFiles[activeFileIndex].bg.objectSize];

        for (i = 0; i < openMapFiles[activeFileIndex].bg.objectSize; i++)
        {
            data.bg.foldouts[i] = openMapFiles[activeFileIndex].bg.foldouts[i];
        }

        FileInfo fileInfo = new FileInfo(path);
        StreamWriter sw = fileInfo.AppendText();
        sw.WriteLine(JsonUtility.ToJson(data));
        sw.Flush();
        sw.Close();

        try
        {
            File.Delete(path + ".meta");
        }
        catch (System.Exception exception)
        {
            Debug.Log(exception);
        }

        openMapFiles[activeFileIndex].saveFlag = false;
        Repaint();
    }
    private void Exit()
    {
        if (MapOpenFlag && openMapFiles[activeFileIndex].saveFlag)
        {
            if (!EditorUtility.DisplayDialog("MapEditor 警告", "変更が保存されていませんが、エディタを閉じますか？", " はい ", " いいえ "))
                return;
        }

        Close();
    }
    private void Export()
    {
        string path = "";

        if (openMapFiles[activeFileIndex].mode == 0)
        {
            // マップ
            bool flag = false;

            for (int y = 0; y < openMapFiles[activeFileIndex].mapSizeY; y++)
            {
                for (int x = 0; x < openMapFiles[activeFileIndex].mapSizeX; x++)
                {
                    if (openMapFiles[activeFileIndex].map[y, x].IndexOf("start") > -1)
                    {
                        flag = true;
                    }
                }
            }

            if (!flag)
            {
                EditorUtility.DisplayDialog("MapEditor エラー", "スタート位置が設定されていません！", "OK");
                return;
            }

            path = EditorUtility.SaveFilePanel("select file", defaultMapDirectory, fileMapName, "txt");

            if (path == "")
                return;

            if (File.Exists(path))
            {
                FileStream st = new FileStream(path, FileMode.Open);
                st.SetLength(0);
                st.Close();
            }

            MapData data = new MapData();
            data.mapSizeX = openMapFiles[activeFileIndex].mapSizeX;
            data.mapSizeY = openMapFiles[activeFileIndex].mapSizeY;
            data.map = new string[openMapFiles[activeFileIndex].map.Length];

            int i = 0;
            for (int y = 0; y < openMapFiles[activeFileIndex].mapSizeY; y++)
            {
                for (int x = 0; x < openMapFiles[activeFileIndex].mapSizeX; x++)
                {
                    data.map[i] = openMapFiles[activeFileIndex].map[y, x];
                    i++;
                }
            }

            data.viewRect = new Rect(openMapFiles[activeFileIndex].viewX, openMapFiles[activeFileIndex].viewY, openMapFiles[activeFileIndex].viewW, openMapFiles[activeFileIndex].viewH);

            FileInfo fileInfo = new FileInfo(path);
            StreamWriter sw = fileInfo.AppendText();
            sw.WriteLine(JsonUtility.ToJson(data));
            sw.Flush();
            sw.Close();
        }
        else if (openMapFiles[activeFileIndex].mode == 1)
        {
            // イベント
            path = EditorUtility.SaveFilePanel("select file", defaultEventDirectory, fileEvName, "txt");

            if (path == "")
                return;

            if (File.Exists(path))
            {
                FileStream st = new FileStream(path, FileMode.Open);
                st.SetLength(0);
                st.Close();
            }

            MapEventData data = new MapEventData();
            data.eventChip = openMapFiles[activeFileIndex].eventChips.ToArray();

            FileInfo fileInfo = new FileInfo(path);
            StreamWriter sw = fileInfo.AppendText();
            sw.WriteLine(JsonUtility.ToJson(data));
            sw.Flush();
            sw.Close();
        }
        else if (openMapFiles[activeFileIndex].mode == 2)
        {
            // 背景
            path = EditorUtility.SaveFilePanel("select file", defaultBackgroundDirectory, fileBgName, "txt");

            if (path == "")
                return;

            if (File.Exists(path))
            {
                FileStream st = new FileStream(path, FileMode.Open);
                st.SetLength(0);
                st.Close();
            }

            MapBackgroundData data = new MapBackgroundData();
            data.mode = openMapFiles[activeFileIndex].bg.mode;
            data.background = openMapFiles[activeFileIndex].bg.background;
            data.backcolor = openMapFiles[activeFileIndex].bg.backcolor;
            data.loopXFlag = openMapFiles[activeFileIndex].bg.loopXFlag;
            data.loopYFlag = openMapFiles[activeFileIndex].bg.loopYFlag;
            data.objectSize = openMapFiles[activeFileIndex].bg.objectSize;
            data.foldouts = new FoldOut[openMapFiles[activeFileIndex].bg.objectSize];

            for (int i = 0; i < openMapFiles[activeFileIndex].bg.objectSize; i++)
            {
                data.foldouts[i] = openMapFiles[activeFileIndex].bg.foldouts[i];
            }

            FileInfo fileInfo = new FileInfo(path);
            StreamWriter sw = fileInfo.AppendText();
            sw.WriteLine(JsonUtility.ToJson(data));
            sw.Flush();
            sw.Close();
        }

        if (path == "")
        {
            try
            {
                File.Delete(path + ".meta");
            }
            catch (System.Exception exception)
            {
                Debug.Log(exception);
            }

            openMapFiles[activeFileIndex].saveFlag = false;

            EditorUtility.DisplayDialog("MapEditor", "保存が完了しました。\n" + path, "OK");
        }
    }
    private void Import()
    {
        if (openMapFiles[activeFileIndex].mode == 0)
        {
            if (openMapFiles[activeFileIndex].saveFlag)
            {
                if (!EditorUtility.DisplayDialog("MapEditor 警告", "変更が保存されていませんが、マップファイルをインポートしますか？", " はい ", " いいえ "))
                {
                    return;
                }
            }

            // マップ
            string path = EditorUtility.OpenFilePanel("select file", defaultMapDirectory, "txt");

            if (!string.IsNullOrEmpty(path))
            {
                StreamReader sr = new StreamReader(path, System.Text.Encoding.UTF8);
                MapData data = JsonUtility.FromJson<MapData>(sr.ReadToEnd());
                sr.Close();

                Rect r = data.viewRect;

                string[,] oldMap = (string[,])openMapFiles[activeFileIndex].map.Clone();

                openMapFiles[activeFileIndex].mapSizeX = data.mapSizeX;
                openMapFiles[activeFileIndex].mapSizeY = data.mapSizeY;
                openMapFiles[activeFileIndex].map = new string[openMapFiles[activeFileIndex].mapSizeY, openMapFiles[activeFileIndex].mapSizeX];

                int i = 0;
                for (int y = 0; y < openMapFiles[activeFileIndex].mapSizeY; y++)
                {
                    for (int x = 0; x < openMapFiles[activeFileIndex].mapSizeX; x++)
                    {
                        openMapFiles[activeFileIndex].map[y, x] = data.map[i];
                        i++;
                    }
                }

                openMapFiles[activeFileIndex].viewX = (int)r.x;
                openMapFiles[activeFileIndex].viewY = (int)r.y;
                openMapFiles[activeFileIndex].viewW = (int)r.width;
                openMapFiles[activeFileIndex].viewH = (int)r.height;

                openMapFiles[activeFileIndex].mapPrevIndexList.Add(new MapSizeIndex(oldMap.GetLength(1), oldMap.GetLength(0)));
                openMapFiles[activeFileIndex].mapNextIndexList.Clear();
                openMapFiles[activeFileIndex].mapPrevList.Add((string[,])oldMap.Clone());
                openMapFiles[activeFileIndex].mapNextList.Clear();
                openMapFiles[activeFileIndex].mapPrevSaveFlagList.Add(openMapFiles[activeFileIndex].saveFlag);
                openMapFiles[activeFileIndex].mapNextSaveFlagList.Clear();
                openMapFiles[activeFileIndex].saveFlag = true;

                openMapFiles[activeFileIndex].gridRect = CreateGrid(openMapFiles[activeFileIndex].mapSizeY, openMapFiles[activeFileIndex].mapSizeX);
                openMapFiles[activeFileIndex].scrollPos.y = 50 * openMapFiles[activeFileIndex].mapSizeY;
                Focus();
                Repaint();
            }
        }
        else if (openMapFiles[activeFileIndex].mode == 1)
        {
            // イベント
            string path = EditorUtility.OpenFilePanel("select file", defaultEventDirectory, "txt");

            if (!string.IsNullOrEmpty(path))
            {
                StreamReader sr = new StreamReader(path, System.Text.Encoding.UTF8);
                MapEventData data = JsonUtility.FromJson<MapEventData>(sr.ReadToEnd());
                sr.Close();

                openMapFiles[activeFileIndex].eventChips = new List<MapEventChip>();

                for (int i = 0; i < data.eventChip.Length; i++)
                {
                    openMapFiles[activeFileIndex].eventChips.Add(data.eventChip[i]);
                }

                Repaint();
            }
        }
        else if (openMapFiles[activeFileIndex].mode == 2)
        {
            // 背景
            string path = EditorUtility.OpenFilePanel("select file", defaultBackgroundDirectory, "txt");

            if (!string.IsNullOrEmpty(path))
            {
                StreamReader sr = new StreamReader(path, System.Text.Encoding.UTF8);
                MapBackgroundData data = JsonUtility.FromJson<MapBackgroundData>(sr.ReadToEnd());
                sr.Close();

                openMapFiles[activeFileIndex].bg.mode = data.mode;
                openMapFiles[activeFileIndex].bg.background = data.background;
                openMapFiles[activeFileIndex].bg.backcolor = data.backcolor;
                openMapFiles[activeFileIndex].bg.loopXFlag = data.loopXFlag;
                openMapFiles[activeFileIndex].bg.loopYFlag = data.loopYFlag;
                openMapFiles[activeFileIndex].bg.objectSize = data.objectSize;

                openMapFiles[activeFileIndex].bg.foldouts = new FoldOut[openMapFiles[activeFileIndex].bg.objectSize];

                for (int i = 0; i < openMapFiles[activeFileIndex].bg.objectSize; i++)
                {
                    openMapFiles[activeFileIndex].bg.foldouts[i] = data.foldouts[i];
                }

                openMapFiles[activeFileIndex].scrollPos.y = 50 * openMapFiles[activeFileIndex].mapSizeY;
                Focus();
                Repaint();
            }
        }
    }
    private void ShowVersion()
    {
        EditorUtility.DisplayDialog("MapEditor - Version", VERSION + "\n\n" + VERSION_TEXT, "OK");
    }
    public MapSaveData GetMapSaveData
    {
        get
        {
            MapSaveData data = new MapSaveData();

            // マップ
            data.map = new MapData();
            data.map.mapSizeX = openMapFiles[activeFileIndex].mapSizeX;
            data.map.mapSizeY = openMapFiles[activeFileIndex].mapSizeY;
            data.map.map = new string[openMapFiles[activeFileIndex].map.Length];

            int i = 0;
            for (int y = 0; y < openMapFiles[activeFileIndex].mapSizeY; y++)
            {
                for (int x = 0; x < openMapFiles[activeFileIndex].mapSizeX; x++)
                {
                    data.map.map[i] = openMapFiles[activeFileIndex].map[y, x];
                    i++;
                }
            }

            data.map.viewRect = new Rect(openMapFiles[activeFileIndex].viewX, openMapFiles[activeFileIndex].viewY, openMapFiles[activeFileIndex].viewW, openMapFiles[activeFileIndex].viewH);

            // イベント
            data.ev = new MapEventData();
            data.ev.eventChip = openMapFiles[activeFileIndex].eventChips.ToArray();

            // 背景
            data.bg = new MapBackgroundData();
            data.bg.mode = openMapFiles[activeFileIndex].bg.mode;
            data.bg.background = openMapFiles[activeFileIndex].bg.background;
            data.bg.backcolor = openMapFiles[activeFileIndex].bg.backcolor;
            data.bg.loopXFlag = openMapFiles[activeFileIndex].bg.loopXFlag;
            data.bg.loopYFlag = openMapFiles[activeFileIndex].bg.loopYFlag;
            data.bg.objectSize = openMapFiles[activeFileIndex].bg.objectSize;
            data.bg.foldouts = new FoldOut[openMapFiles[activeFileIndex].bg.objectSize];

            for (i = 0; i < openMapFiles[activeFileIndex].bg.objectSize; i++)
            {
                data.bg.foldouts[i] = openMapFiles[activeFileIndex].bg.foldouts[i];
            }

            return data;
        }
    }
    public string GetFileName
    {
        get { return openMapFiles[activeFileIndex].fileName; }
    }
    public bool SaveFlag
    {
        get { return openMapFiles[activeFileIndex].saveFlag; }
    }
    public bool MapOpenFlag
    {
        get { return openMapFiles != null && openMapFiles.Count > 0; }
    }
    public bool PrevFlag
    {
        get { return openMapFiles[activeFileIndex].prevFlag; }
    }
    public bool NextFlag
    {
        get { return openMapFiles[activeFileIndex].nextFlag; }
    }
    public List<OpenMapFile> OpenMapFiles
    {
        get { return openMapFiles; }
        set { openMapFiles = value; }
    }
    public List<MapEventChip> EventChips
    {
        get { return openMapFiles[activeFileIndex].eventChips; }
        set { openMapFiles[activeFileIndex].eventChips = value; }
    }
    public int ActiveFileIndex
    {
        get { return activeFileIndex; }
        set { activeFileIndex = value; }
    }
    public int SelectEventIndex
    {
        get { return openMapFiles[activeFileIndex].selectEventIndex; }
        set { openMapFiles[activeFileIndex].selectEventIndex = value; }
    }
    public Vector2 SelectVec
    {
        get { return openMapFiles[activeFileIndex].selectVec; }
        set { openMapFiles[activeFileIndex].selectVec = value; }
    }
    public string GetVersion
    {
        get { return VERSION; }
    }
    public string GetVersionText
    {
        get { return VERSION_TEXT; }
    }
    public int Mode
    {
        get { return openMapFiles[activeFileIndex].mode; }
        set { openMapFiles[activeFileIndex].mode = value; }
    }
    public int MapSizeX
    {
        get { return MapOpenFlag ? openMapFiles[activeFileIndex].mapSizeX : 10; }
        set { openMapFiles[activeFileIndex].mapSizeX = value; }
    }
    public int MapSizeY
    {
        get { return MapOpenFlag ? openMapFiles[activeFileIndex].mapSizeY : 10; }
        set { openMapFiles[activeFileIndex].mapSizeY = value; }
    }
    public int VarFlgSize
    {
        get { return VAR_FLG_SIZE; }
    }
    public int VarIntSize
    {
        get { return VAR_INT_SIZE; }
    }
    public int VarStrSize
    {
        get { return VAR_STR_SIZE; }
    }
    public string DefaultDirectory
    {
        get { return defaultDirectory; }
    }
    public string DefaultMapDirectory
    {
        get { return defaultMapDirectory; }
    }
    public string DefaultBackgroundDirectory
    {
        get { return defaultBackgroundDirectory; }
    }
    public string DefaultEventDirectory
    {
        get { return defaultEventDirectory; }
    }
    public string DefaultSoundDirectory
    {
        get { return defaultSoundDirectory; }
    }
    public string DefaultImageDirectory
    {
        get { return defaultImageDirectory; }
    }
    public List<EventCommand> EventClipBoard
    {
        get { return eventClipBoard; }
        set { eventClipBoard = value; }
    }
}
public class Painter : EditorWindow
{
    private const float WINDOW_W = 500;
    private const float WINDOW_H = 300;
    private List<Rect> rect = new List<Rect>();

    public static Painter WillAppear(MapEditor _parent)
    {
        GetWindow<Painter>().Close();
        Painter window = CreateInstance<Painter>();
        window.ShowUtility();
        window.minSize = new Vector2(WINDOW_W, WINDOW_H);
        window.init();
        return window;
    }

    public void init()
    {
        wantsMouseMove = true;
    }

    void OnGUI()
    {
        EditorGUI.DrawRect(new Rect(0, 0, Screen.width, Screen.height), Color.white);

        Vector2 pos = Event.current.mousePosition;
        Event e = Event.current;

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            rect.Add(new Rect(pos.x, pos.y, 3, 3));

        foreach (Rect rec in rect)
            EditorGUI.DrawRect(rec, Color.black);

        if (e.type == EventType.MouseMove || e.button == 0)
            Repaint();
    }
}
public class MapEditorNewFileWindow : EditorWindow
{
    private const float WINDOW_W = 300;
    private const float WINDOW_H = 100;
    private int mode;
    private int mapSizeX;
    private int mapSizeY;
    private MapEditor parent;

    public static MapEditorNewFileWindow WillAppear(MapEditor _parent, int mode)
    {
        GetWindow<MapEditorNewFileWindow>().Close();
        MapEditorNewFileWindow window = CreateInstance<MapEditorNewFileWindow>();
        window.ShowUtility();
        window.minSize = new Vector2(WINDOW_W, WINDOW_H);
        window.maxSize = new Vector2(WINDOW_W, WINDOW_H);
        window.SetParent(_parent);
        window.titleContent = new GUIContent(mode == 0 ? "新規ファイル作成" : "マップサイズ変更");
        window.mode = mode;
        window.init();
        return window;
    }

    public void init()
    {
        wantsMouseMove = true;
        mapSizeX = parent.MapSizeX;
        mapSizeY = parent.MapSizeY;
    }

    void Update()
    {
        if (focusedWindow.titleContent.text == "MapEditor")
            Focus();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.Space();
        GUILayout.Label("マップサイズ");

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("幅", GUILayout.Width(100));
        mapSizeX = EditorGUILayout.IntField(mapSizeX);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("高さ", GUILayout.Width(100));
        mapSizeY = EditorGUILayout.IntField(mapSizeY);
        EditorGUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("  OK  "))
        {
            switch (mode)
            {
                case 0:
                    parent.NewFileInit(new Vector2(mapSizeX, mapSizeY));
                    break;
                case 1:
                    parent.MapSizeX = mapSizeX;
                    parent.MapSizeY = mapSizeY;
                    parent.MapSizeUpdate();
                    break;
                default:
                    break;
            }

            parent.Repaint();
            Close();
        }

        if (GUILayout.Button("キャンセル"))
            Close();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        if (Event.current.type == EventType.MouseMove)
            Repaint();
    }

    private void SetParent(MapEditor _parent)
    {
        parent = _parent;
    }
}
public class MapEditorVarSettingWindow : EditorWindow
{
    private const float WINDOW_W = 735;
    private const float WINDOW_H = 500;
    private int flgSize;
    private int intSize;
    private int strSize;
    private Vector2 scrollPos1 = Vector2.zero;
    private Vector2 scrollPos2 = Vector2.zero;
    private Vector2 scrollPos3 = Vector2.zero;
    private List<FlgVarData> flgVar = new List<FlgVarData>();
    private List<IntVarData> intVar = new List<IntVarData>();
    private List<StrVarData> strVar = new List<StrVarData>();
    private MapEditor parent;

    public static MapEditorVarSettingWindow WillAppear(MapEditor _parent)
    {
        GetWindow<MapEditorVarSettingWindow>().Close();
        MapEditorVarSettingWindow window = CreateInstance<MapEditorVarSettingWindow>();
        window.ShowUtility();
        window.minSize = new Vector2(WINDOW_W, WINDOW_H);
        window.maxSize = new Vector2(WINDOW_W, WINDOW_H);
        window.SetParent(_parent);
        window.titleContent = new GUIContent("変数設定");
        window.init();
        return window;
    }

    public void init()
    {
        wantsMouseMove = true;
        Load();
    }

    void Update()
    {
        if (focusedWindow != null && focusedWindow.titleContent.text == "MapEditor")
            Focus();
    }

    void OnGUI()
    {
        Event e = Event.current;

        EditorGUILayout.BeginHorizontal();

        // フラグ
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal(GUILayout.Width(200));
        GUILayout.Label("フラグの数 : ");
        flgSize = EditorGUILayout.IntField(flgSize);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(200), GUILayout.Height(Screen.height - 75));
        scrollPos1 = EditorGUILayout.BeginScrollView(scrollPos1);

        for (int i = 0; i < flgSize; i++)
        {
            if (flgVar.Count <= i)
            {
                FlgVarData vd = new FlgVarData();
                vd.name = "" + i;
                vd.var = false;
                flgVar.Add(vd);
            }

            if (flgVar[i] != null)
            {

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("名前 : ");
                flgVar[i].name = EditorGUILayout.TextField(flgVar[i].name);
                EditorGUILayout.Space();
                GUILayout.Label("値 : ");
                flgVar[i].var = EditorGUILayout.Toggle(flgVar[i].var);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();

        // 整数
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal(GUILayout.Width(220));
        GUILayout.Label("整数変数の数 : ");
        intSize = EditorGUILayout.IntField(intSize);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(220), GUILayout.Height(Screen.height - 75));
        scrollPos2 = EditorGUILayout.BeginScrollView(scrollPos2);

        for (int i = 0; i < intSize; i++)
        {
            if (intVar.Count <= i)
            {
                IntVarData vd = new IntVarData();
                vd.name = "" + i;
                vd.var = 0;
                intVar.Add(vd);
            }

            if (intVar[i] != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("名前 : ");
                intVar[i].name = EditorGUILayout.TextField(intVar[i].name);
                EditorGUILayout.Space();
                GUILayout.Label("値 : ");
                intVar[i].var = EditorGUILayout.IntField(intVar[i].var);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();

        // 文字列
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal(GUILayout.Width(290));
        GUILayout.Label("文字列変数の数 : ");
        strSize = EditorGUILayout.IntField(strSize);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(290), GUILayout.Height(Screen.height - 75));
        scrollPos3 = EditorGUILayout.BeginScrollView(scrollPos3);

        for (int i = 0; i < strSize; i++)
        {
            if (strVar.Count <= i)
            {
                StrVarData vd = new StrVarData();
                vd.name = "" + i;
                vd.var = "";
                strVar.Add(vd);
            }

            if (strVar[i] != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("名前 : ");
                strVar[i].name = EditorGUILayout.TextField(strVar[i].name);
                EditorGUILayout.Space();
                GUILayout.Label("値 : ");
                strVar[i].var = EditorGUILayout.TextField(strVar[i].var);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("OK", GUILayout.Height(40)))
        {
            Save();
            Close();
        }

        if (e.type == EventType.MouseMove)
            Repaint();
        else if (e.type == EventType.MouseDown)
            GUI.FocusControl("");
    }

    private void Load()
    {
        List<FlgVarData> var1 = new List<FlgVarData>();

        for (int i = 0; i < parent.var.var_flg.Count; i++)
        {
            FlgVarData vd = new FlgVarData();
            vd.name = parent.var.var_flg[i].name;
            vd.var = parent.var.var_flg[i].var;
            var1.Add(vd);
        }

        flgVar = var1;

        List<IntVarData> var2 = new List<IntVarData>();

        for (int i = 0; i < parent.var.var_int.Count; i++)
        {
            IntVarData vd = new IntVarData();
            vd.name = parent.var.var_int[i].name;
            vd.var = parent.var.var_int[i].var;
            var2.Add(vd);
        }

        intVar = var2;

        List<StrVarData> var3 = new List<StrVarData>();

        for (int i = 0; i < parent.var.var_str.Count; i++)
        {
            StrVarData vd = new StrVarData();
            vd.name = parent.var.var_str[i].name;
            vd.var = parent.var.var_str[i].var;
            var3.Add(vd);
        }

        strVar = var3;

        flgSize = parent.var.var_flg.Count;
        intSize = parent.var.var_int.Count;
        strSize = parent.var.var_str.Count;
    }

    private void Save()
    {
        if (flgSize < flgVar.Count)
        {
            int size = flgVar.Count;
            for (int i = size - 1; i >= flgSize; i--)
            {
                flgVar.RemoveAt(i);
            }
        }

        if (intSize < intVar.Count)
        {
            int size = intVar.Count;
            for (int i = size - 1; i >= intSize; i--)
            {
                intVar.RemoveAt(i);
            }
        }

        if (strSize < strVar.Count)
        {
            int size = strVar.Count;
            for (int i = size - 1; i >= strSize; i--)
            {
                strVar.RemoveAt(i);
            }
        }

        parent.var.var_flg = new List<FlgVarData>(flgVar);
        parent.var.var_int = new List<IntVarData>(intVar);
        parent.var.var_str = new List<StrVarData>(strVar);

        parent.VarSave();
    }

    private void SetParent(MapEditor _parent)
    {
        parent = _parent;
    }
}
public class MapEditorEventWindow : EditorWindow
{
    private const float WINDOW_W = 650;
    private const float WINDOW_H = 450;
    private const float LEFT_W = 220;
    private const float LABEL_W = 100;
    private int doubleCount;
    private int selectStart;
    private int selectEnd;
    private int clipIndex;
    private Drawer d = new Drawer();
    private SelectBox sb = new SelectBox();
    private SelectBox sb2 = new SelectBox();
    private SelectBox sb3 = new SelectBox();

    public MapEditor parent;
    public int selectID;
    public EventCommandWindow subWindow;

    public static MapEditorEventWindow WillAppear(MapEditor _parent)
    {
        MapEditorEventWindow window = (MapEditorEventWindow)GetWindow(typeof(MapEditorEventWindow), false);
        window.Show();
        window.minSize = new Vector2(WINDOW_W, WINDOW_H);
        window.titleContent = new GUIContent("イベントウィンドウ");
        window.SetParent(_parent);
        window.init();
        return window;
    }

    public void init()
    {
        wantsMouseMove = true;

        // サウンド
        if (GameObject.Find("EditorSystem") != null)
            DestroyImmediate(GameObject.Find("EditorSystem"));

        selectStart = -1;
        selectEnd = -1;
    }

    void Update()
    {
        if ((parent == null) ||
            (parent != null && !parent.MapOpenFlag) ||
            (parent.EventChips != null && parent.EventChips.Count <= selectID)
            )
        {
            Close();
            return;
        }

        doubleCount--;

        if (doubleCount < 0)
            doubleCount = 0;
    }

    void OnGUI()
    {
        if (parent == null)
            return;

        Event e = Event.current;
        Vector2 pos = e.mousePosition;
        EventFold eve = parent.EventChips[selectID]._event;

        if (eve.command[eve.command.Count - 1].viewCommand != "■")
            AddCommand(new EventCommand("", "■", ""));

        System.Func<List<EventCommand>, string[]> toStrArry = (List<EventCommand> list) => { List<string> strs = new List<string>(); for (int i = 0; i < list.Count; i++) strs.Add(list[i].viewCommand); return strs.ToArray(); };

        System.Func<int, int> func1 = (int id) =>
        {
            if (id == -1 || id >= eve.command.Count || eve.command[id].type == null)
                return eve.selectStart;

            int i;
            int ifCount = 1;
            for (i = id; i > 0; i--)
            {
                if (eve.command[i].jsonCommand != "" && (eve.command[i].type.IndexOf("条件") > -1 || eve.command[i].type.IndexOf("分岐") > -1))
                {
                    int index = JsonUtility.FromJson<EventIfData>(eve.command[i].jsonCommand).nowContNum;
                    if (index == 0)
                    {
                        ifCount--;
                        if (ifCount == 0)
                            break;
                    }
                    else if (id != i && index == -1)
                        ifCount++;
                }
            }
            return eve.command[id].type.IndexOf("条件") > -1 ||
            eve.command[id].type.IndexOf("分岐") > -1 ?
            JsonUtility.FromJson<EventIfData>(eve.command[id].jsonCommand).nowContNum == 0 ? id : i : id;
        };

        System.Func<int, int> func2 = (int id) =>
        {
            if (id == -1 || id >= eve.command.Count || eve.command[id].type == null )
                return eve.selectStart;

            int i;
            int ifCount = 1;
            for (i = id; i < eve.command.Count; i++)
            {
                if (eve.command[i].jsonCommand != "" && (eve.command[i].type.IndexOf("条件") > -1 || eve.command[i].type.IndexOf("分岐") > -1))
                {
                    int index = JsonUtility.FromJson<EventIfData>(eve.command[i].jsonCommand).nowContNum;
                    if (index == -1)
                    {
                        ifCount--;
                        if (ifCount == 0)
                            break;
                    }
                    else if (id != i && index == 0)
                        ifCount++;
                }
            }
            return eve.command[id].type.IndexOf("条件") > -1 ||
            eve.command[id].type.IndexOf("分岐") > -1 ? i : id;
        };

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LEFT_W - 3), GUILayout.Height(Screen.height - 38));
        List<string> names = new List<string>();
        for (int i = 0; i < parent.EventChips.Count; i++)
            names.Add(parent.EventChips[i].name != null ? parent.EventChips[i].name : i.ToString());
        parent.SelectEventIndex = sb.Show(new Rect(0, 0, LEFT_W - 30, parent.EventChips.Count * 20), parent.SelectEventIndex, parent.SelectEventIndex, true, names.ToArray(), false, false, new FuncOpener(Repaint), new FuncSelectBoxOpener((int id) =>
        {
            if (id != -1)
                parent.OpenMapFiles[parent.ActiveFileIndex].selectVec = new Vector2(parent.OpenMapFiles[parent.ActiveFileIndex].eventChips[id].x, parent.OpenMapFiles[parent.ActiveFileIndex].eventChips[id].y);
            parent.evWindow = WillAppear(parent);
            parent.evWindow.selectID = id;
            Repaint();
            parent.Repaint();
        }
        ), null);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("コマンドウィンドウを表示", GUILayout.Height(30)))
        {
            subWindow = EventCommandWindow.WillAppear(this);
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Label(parent.EventChips[selectID].name != null ? parent.EventChips[selectID].name : selectID.ToString());

        float x = pos.x - 224 + sb2.GetScrollPos.x;
        float y = pos.y - 63 + sb2.GetScrollPos.y;
        int posIndex = (int)(y / 19);

        if (e.type == EventType.MouseDown)
        {
            selectStart = -1;
            if (pos.x > 224 && pos.x < Screen.width - 23 && pos.y > 61 && pos.y < (Screen.height - 61 - 150))
            {
                selectStart = posIndex >= eve.command.Count ? -1 : posIndex;
            }
            selectEnd = selectStart;
        }
        else if (e.type == EventType.MouseDrag)
        {
            if (pos.x > 224 && pos.x < Screen.width - 23 && pos.y > 61 && pos.y < (Screen.height - 61 - 150) && selectStart != -1)
            {
                selectEnd = posIndex >= eve.command.Count ? selectStart == posIndex ? -1 : eve.command.Count - 1 : posIndex;
            }
        }

        eve.selectStart = selectStart > selectEnd ? selectEnd : selectStart;
        eve.selectEnd = selectStart > selectEnd ? selectStart : selectEnd;
        if (eve.selectStart == -1 && eve.selectEnd != -1)
        {
            eve.selectStart = eve.selectEnd;
            eve.selectEnd = eve.command.Count - 1;
        }
        eve.selectStart = eve.selectStart != -1 && eve.selectStart < eve.command.Count && selectStart < eve.command.Count && selectEnd < eve.command.Count ?
            (eve.command[selectStart].type.IndexOf("条件") > -1 || eve.command[selectStart].type.IndexOf("分岐") > -1) ?
            func1(selectStart) : eve.selectStart : eve.selectStart;
        eve.selectEnd = eve.selectEnd != -1 && eve.selectEnd < eve.command.Count && selectStart < eve.command.Count && selectEnd < eve.command.Count ?
            (eve.command[selectStart].type.IndexOf("条件") > -1 || eve.command[selectStart].type.IndexOf("分岐") > -1) ?
            func2(selectStart) : eve.selectEnd : eve.selectEnd;

        if (selectStart != selectEnd && selectStart != -1 && selectEnd != -1 && selectStart < eve.command.Count && selectEnd < eve.command.Count)
        {
            if (selectStart > selectEnd)
            {
                for (int i = selectStart; i >= selectEnd; i--)
                {
                    if (!(eve.command[i].type.IndexOf("分岐終了") > -1))
                    {
                        if (eve.command[i].type.IndexOf("分岐") > -1)
                        {
                            eve.selectStart = i + 1;
                            selectEnd = i + 1;
                            break;
                        }

                        if (eve.command[i].viewCommand == "■")
                        {
                            eve.selectEnd = i - 1;
                            selectStart = i - 1;
                        }
                    }
                    else
                    {
                        int count = 0;
                        for (int j = i - 1; j >= 0; j--)
                        {
                            if (eve.command[j].type.IndexOf("分岐終了") > -1)
                                count++;
                            else if (eve.command[j].type.IndexOf("条件") > -1)
                            {
                                count--;
                                if (count < 0)
                                {
                                    if (j > selectEnd)
                                    {
                                        for (int k = j; k >= selectEnd; k--)
                                        {
                                            if (!(eve.command[k].type.IndexOf("分岐終了") > -1))
                                            {
                                                if (eve.command[k].type.IndexOf("分岐") > -1)
                                                {
                                                    eve.selectStart = k + 1;
                                                    selectEnd = k + 1;
                                                    break;
                                                }
                                            }
                                        }
                                        break;
                                    }
                                    eve.selectStart = j;
                                    selectEnd = j;
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }
            else
            {
                for (int i = selectStart; i < selectEnd + 1; i++)
                {
                    if (!(eve.command[i].type.IndexOf("条件") > -1))
                    {
                        if (eve.command[i].viewCommand == "■")
                        {
                            eve.selectEnd = i - 1;
                            selectEnd = i - 1;
                            break;
                        }
                    }
                    else
                    {
                        int count = 0;
                        for (int j = i + 1; j < eve.command.Count; j++)
                        {
                            if (eve.command[j].type.IndexOf("分岐終了") > -1)
                            {
                                count--;
                                if (count < 0)
                                {
                                    if (j < selectEnd)
                                    {
                                        for (int k = j; k < selectEnd + 1; k++)
                                        {
                                            if (eve.command[k].viewCommand == "■")
                                            {
                                                eve.selectEnd = k - 1;
                                                selectEnd = k - 1;
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                    eve.selectEnd = j;
                                    selectEnd = j;
                                    break;
                                }
                            }
                            else if (eve.command[j].type.IndexOf("条件") > -1)
                                count++;
                        }
                        break;
                    }
                }
            }
        }

        // コマンドのコピペ
        if (eve.selectStart != -1)
        {
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.C)
                {
                    if (eve.selectEnd != -1 && eve.selectStart < eve.command.Count && eve.selectEnd < eve.command.Count)
                    {
                        if (eve.command[eve.selectStart].viewCommand != "■")
                        {
                            parent.EventClipBoard.Clear();
                            parent.EventClipBoard.AddRange(eve.command.GetRange(eve.selectStart, eve.selectEnd - eve.selectStart + 1));
                            Repaint();
                        }
                    }
                }
                else if (e.keyCode == KeyCode.V)
                {
                    if (parent.EventClipBoard.Count > 0)
                    {
                        InsertRangeCommand(parent.EventClipBoard);
                        Repaint();
                    }
                }
                else if (e.keyCode == KeyCode.X)
                {
                    if (eve.selectEnd != -1 && eve.selectStart < eve.command.Count && eve.selectEnd < eve.command.Count)
                    {
                        if (eve.command.Count > 0 && eve.command[eve.selectStart].viewCommand != "■")
                        {
                            parent.EventClipBoard.Clear();
                            parent.EventClipBoard.AddRange(eve.command.GetRange(eve.selectStart, eve.selectEnd - eve.selectStart + 1));
                            RemoveRangeCommand(eve.selectStart, eve.selectEnd - eve.selectStart + 1);
                            eve.selectEnd = eve.selectStart;
                            selectEnd = eve.selectStart;
                            selectStart = eve.selectStart;
                            Repaint();
                        }
                    }
                }
            }
        }

        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(Screen.width - LEFT_W - 7), GUILayout.Height(Screen.height - 90 - 160));
        eve.selectStart = sb2.Show(new Rect(0, 0, 1000, eve.command.Count * 20),
            eve.selectStart, eve.selectEnd, false, toStrArry(eve.command), true, true, new FuncOpener(Repaint), null,
            new FuncSelectBoxOpener((int id) =>
            {
                if (id == eve.selectStart)
                {
                    subWindow = EventCommandWindow.WillAppear(this);
                    subWindow.initCommand(eve.selectStart, eve.command[id]);
                }
                else
                {
                    if (eve.command[id].type.IndexOf("分岐") > -1)
                    {
                        subWindow = EventCommandWindow.WillAppear(this);
                        subWindow.initCommand(func1(eve.selectStart), eve.command[id]);
                    }
                }
            }));
        EditorGUILayout.EndVertical();

        GUILayout.Space(6);
        GUILayout.Label("クリップボード : ");
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(Screen.width - LEFT_W - 7), GUILayout.Height(130));
        clipIndex = sb3.Show(new Rect(0, 0, 1000, parent.EventClipBoard.Count * 20), clipIndex, clipIndex, false, toStrArry(parent.EventClipBoard), true, true, new FuncOpener(Repaint), null, null);
        EditorGUILayout.EndVertical();

        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Delete)
            {
                if (eve.selectStart != -1 && eve.selectEnd != -1 && eve.selectStart < eve.command.Count && eve.selectEnd < eve.command.Count)
                {
                    if (eve.command.Count > 0 && eve.command[eve.selectStart].viewCommand != "■")
                    {
                        RemoveRangeCommand(eve.selectStart, eve.selectEnd - eve.selectStart + 1);
                        eve.selectEnd = eve.selectStart;
                        selectEnd = eve.selectStart;
                        selectStart = eve.selectStart;
                        Repaint();
                    }
                }
            }
            else if (e.keyCode == KeyCode.UpArrow)
            {
                if (eve.selectStart != -1 && eve.selectStart != 0 && eve.command.Count > eve.selectStart)
                {
                    int num1_min = func1(eve.selectStart - 1);
                    int num1_max = func2(eve.selectStart - 1);
                    int num2_min = func1(eve.selectStart);
                    int num2_max = func2(eve.selectStart);

                    if (eve.command[num1_min].viewCommand != "■" &&
                        eve.command[num1_max].viewCommand != "■" &&
                        eve.command[num2_min].viewCommand != "■" &&
                        eve.command[num2_max].viewCommand != "■" &&
                        !(!(eve.command[eve.selectStart - 1].type.IndexOf("分岐終了") > -1) && eve.command[eve.selectStart - 1].type.IndexOf("分岐") > -1))
                    {
                        ChangeRangeCommand(num1_min, num1_max, num2_min, num2_max);
                        eve.selectStart = num1_min;
                        selectStart = num1_min;
                        selectEnd = num1_max;
                        Repaint();
                    }
                }
            }
            else if (e.keyCode == KeyCode.DownArrow)
            {
                if (eve.selectStart != -1 && eve.selectStart != eve.command.Count - 1 && eve.command.Count > eve.selectStart)
                {
                    int num1_min = func1(eve.selectStart);
                    int num1_max = func2(eve.selectStart);
                    int num2_min = func1(num1_max + 1);
                    int num2_max = func2(num1_max + 1);

                    if (eve.command[num1_min].viewCommand != "■" &&
                        eve.command[num1_max].viewCommand != "■" &&
                        eve.command[num2_min].viewCommand != "■" &&
                        eve.command[num2_max].viewCommand != "■")
                    {
                        ChangeRangeCommand(num1_min, num1_max, num2_min, num2_max);
                        eve.selectStart = num2_min;
                        selectStart = num2_min;
                        selectEnd = num2_max;
                        Repaint();
                    }
                }
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        if (e.type == EventType.MouseMove)
            Repaint();
    }

    private void SetParent(MapEditor _parent)
    {
        parent = _parent;
    }

    public void RemoveCommand(int num)
    {
        parent.EventChips[selectID]._event.command.RemoveAt(num);
    }

    public void SetCommand(int num, EventCommand command)
    {
        parent.EventChips[selectID]._event.command.RemoveAt(num);
        parent.EventChips[selectID]._event.command.Insert(num, command);
    }

    public void AddCommand(EventCommand command)
    {
        parent.EventChips[selectID]._event.command.Add(command);
    }

    public void InsertCommand(int index, EventCommand command)
    {
        parent.EventChips[selectID]._event.command.Insert(index, command);
    }

    public void InsertCommand(EventCommand command)
    {
        int index = parent.EventChips[selectID]._event.selectStart == -1 ? parent.EventChips[selectID]._event.command.Count - 1 : parent.EventChips[selectID]._event.selectStart;
        InsertCommand(index, command);
    }

    public void InsertAddCommand(EventCommand command)
    {
        InsertCommand(parent.EventChips[selectID]._event.command.Count - 1, command);
    }

    public void ChangeCommand(int num1, int num2)
    {
        EventCommand com = parent.EventChips[selectID]._event.command[num2];
        parent.EventChips[selectID]._event.command.Insert(num1, com);
        parent.EventChips[selectID]._event.command.RemoveAt(num2 + 1);
    }

    public void RemoveRangeCommand(int num1, int num2)
    {
        parent.EventChips[selectID]._event.command.RemoveRange(num1, num2);
    }

    public void SetRangeIfCommand(int num, List<EventCommand> commands)
    {
        EventFold eve = parent.EventChips[selectID]._event;

        System.Func<int, int> func = (int id) =>
        {
            if (eve.command[id].type == null)
                return eve.selectStart;

            int i;
            int ifCount = 1;
            for (i = id; i < eve.command.Count; i++)
            {
                if (eve.command[i].jsonCommand != "" && (eve.command[i].type.IndexOf("条件") > -1 || eve.command[i].type.IndexOf("分岐") > -1))
                {
                    int index = JsonUtility.FromJson<EventIfData>(eve.command[i].jsonCommand).nowContNum;
                    if (index == -1)
                    {
                        ifCount--;
                        if (ifCount == 0)
                            break;
                    }
                    else if (id != i && index == 0)
                        ifCount++;
                }
            }
            return eve.command[id].type.IndexOf("条件") > -1 ||
            eve.command[id].type.IndexOf("分岐") > -1 ? i : id;
        };

        int size = func(eve.selectStart) - num + 1;

        List<List<EventCommand>> ifCom = new List<List<EventCommand>>();

        int count = 0;
        for (int i = num; i < num + size; i++)
        {
            if (eve.command[i].viewCommand != "■")
            {
                if ((!(eve.command[i].type.IndexOf("条件") > -1) &&
                !(eve.command[i].type.IndexOf("分岐") > -1)))
                    ifCom[count - 1].Add(eve.command[i]);
                else if (!(eve.command[i].type.IndexOf("分岐終了") > -1) && eve.command[i].type.IndexOf("分岐") > -1)
                {
                    count++;
                    ifCom.Add(new List<EventCommand>());
                }
            }
        }

        EventIfData oldData = JsonUtility.FromJson<EventIfData>(eve.command[num].jsonCommand);
        SetRangeCommand(num, size, commands);
        EventIfData data = JsonUtility.FromJson<EventIfData>(eve.command[num].jsonCommand);

        int addIndex = 2;
        for (int i = 0; i < data.contNum + (data.elseFlg ? 1 : 0); i++)
        {
            if (oldData.elseFlg && data.elseFlg && i == data.contNum)
            {
                int size2 = func(eve.selectStart) - num + 1;
                InsertRangeCommand(num + size2 - 2, ifCom[ifCom.Count - 1]);
            }
            else if (i < ifCom.Count)
            {
                if (oldData.elseFlg)
                {
                    if (i != oldData.contNum)
                        InsertRangeCommand(num + addIndex, ifCom[i]);
                }
                else
                {
                    if (data.elseFlg)
                    {
                        if (i != data.contNum)
                            InsertRangeCommand(num + addIndex, ifCom[i]);
                    }
                    else
                        InsertRangeCommand(num + addIndex, ifCom[i]);
                }

                addIndex += 2 + ifCom[i].Count;
            }
        }
    }

    public void SetRangeCommand(int num_min, int num_max, List<EventCommand> commands)
    {
        parent.EventChips[selectID]._event.command.RemoveRange(num_min, num_max);
        parent.EventChips[selectID]._event.command.InsertRange(num_min, commands);
    }

    public void AddRangeCommand(List<EventCommand> commands)
    {
        parent.EventChips[selectID]._event.command.AddRange(commands);
    }

    public void InsertRangeCommand(int index, List<EventCommand> commands)
    {
        parent.EventChips[selectID]._event.command.InsertRange(index, commands);
    }

    public void InsertRangeCommand(List<EventCommand> commands)
    {
        InsertRangeCommand(parent.EventChips[selectID]._event.selectStart, commands);
    }

    public void InsertAddRangeCommand(List<EventCommand> commands)
    {
        InsertRangeCommand(parent.EventChips[selectID]._event.command.Count - 1, commands);
    }

    public void ChangeRangeCommand(int num1_min, int num1_max, int num2_min, int num2_max)
    {
        int num1_size = num1_max - num1_min + 1;
        int num2_size = num2_max - num2_min + 1;
        List<EventCommand> com = parent.EventChips[selectID]._event.command.GetRange(num2_min, num2_size);
        parent.EventChips[selectID]._event.command.InsertRange(num1_min, com);
        parent.EventChips[selectID]._event.command.RemoveRange(num1_min + num2_size + num1_size, num2_size);
    }
}
public class EventCommandWindow : EditorWindow
{
    // 基本
    private const float WINDOW_W = 580;
    private const float WINDOW_H = 300;
    private const float MENU_W = 150;
    private const float LABEL_W = 100;
    private int selectCommandList;
    private int mode;
    private int selectID;
    private bool nullComFlg;
    private Drawer d = new Drawer();
    private SelectBox sb = new SelectBox();
    private string[] commandList = new string[] { "文章の表示", "画像操作", "変数操作", "条件文", "場所移動", "サウンド", "エフェクト", "その他" };

    public MapEditorEventWindow parent;

    // 文章の表示
    private EventMessageData eventMessage = new EventMessageData();
    private bool autoCloseMessageFlag;
    private bool continueAddFlag;
    private int messageSelectVarIndex;

    // 画像操作
    private EventImageData eventImage = new EventImageData();
    private string[] imageExt = new string[] { "psd", "tif", "tiff", "jpg", "jpeg", "tga", "png", "gif", "bmp", "iff", "pict" };
    private List<string> imageFilePaths = new List<string>();

    // 変数操作
    private EventVarData eventVar = new EventVarData();
    private List<string> flgVarNames = new List<string>();
    private List<string> intVarNames = new List<string>();
    private List<string> strVarNames = new List<string>();

    // 場所移動
    private EventMoveData eventMove = new EventMoveData();
    private List<string> filePaths = new List<string>();

    // 条件文
    private EventIfData eventIf = new EventIfData();

    // サウンド
    private EventSoundData eventSound = new EventSoundData();
    private string[] soundExt = new string[] { "mp3", "ogg", "wav", "aiff", "aif", "mod", "it", "s3m", "xm" };
    private List<string> soundFilePaths = new List<string>();
    private AudioSource audioSource = new AudioSource();
    private AudioClip clip = new AudioClip();

    // エフェクト
    private EventEffectData eventEffect = new EventEffectData();

    // その他
    private EventOtherData eventOther = new EventOtherData();

    public EventCommandMoveSelectWindow subWindow;

    public static EventCommandWindow WillAppear(MapEditorEventWindow _parent)
    {
        GetWindow<EventCommandWindow>().Close();
        EventCommandWindow window = CreateInstance<EventCommandWindow>();
        window.ShowUtility();
        window.minSize = new Vector2(WINDOW_W, WINDOW_H);
        window.maxSize = new Vector2(WINDOW_W, WINDOW_H);
        window.titleContent = new GUIContent("コマンドウィンドウ");
        window.SetParent(_parent);
        window.init();
        return window;
    }

    public void init()
    {
        wantsMouseMove = true;
        mode = 0;
        nullComFlg = false;

        // 文章
        autoCloseMessageFlag = true;

        // 画像操作
        foreach (string ext in imageExt)
        {
            imageFilePaths.AddRange(Directory.GetFiles(parent.parent.DefaultImageDirectory, "*." + ext, SearchOption.AllDirectories));
        }

        // 場所移動
        filePaths.AddRange(Directory.GetFiles(parent.parent.DefaultDirectory, "*.txt"));
        for (int i = 0; i < filePaths.Count; i++)
        {
            filePaths[i] = filePaths[i].Split('/')[filePaths[i].Split('/').Length - 1].Split('.')[0];
        }

        // サウンド
        foreach (string ext in soundExt)
        {
            soundFilePaths.AddRange(Directory.GetFiles(parent.parent.DefaultSoundDirectory, "*." + ext));
        }
        for (int i = 0; i < soundFilePaths.Count; i++)
        {
            soundFilePaths[i] = soundFilePaths[i].Split('/')[soundFilePaths[i].Split('/').Length - 1];
        }
        eventSound.selectSoundIndex = 0;
        eventSound.soundVolume = 1;
        eventSound.soundPitch = 1;
        eventSound.isPlayFlag = false;
        eventSound.soundPlayTime = 0;

        if (GameObject.Find("EditorSystem") != null)
            DestroyImmediate(GameObject.Find("EditorSystem"));

        // 変数
        eventVar.varMode = 0;
        for (int i = 0; i < 10; i++)
        {
            flgVarNames.Add("ローカルフラグ変数 " + parent.parent.OpenMapFiles[parent.parent.ActiveFileIndex].flgVar[i].name);
            intVarNames.Add("ローカル整数変数 " + parent.parent.OpenMapFiles[parent.parent.ActiveFileIndex].intVar[i].name);
            strVarNames.Add("ローカル文字列変数 " + parent.parent.OpenMapFiles[parent.parent.ActiveFileIndex].strVar[i].name);
        }
        // フラグ
        for (int i = 0; i < parent.parent.var.var_flg.Count; i++)
        {
            flgVarNames.Add("システムフラグ変数 " + parent.parent.var.var_flg[i].name);
        }
        // 整数
        for (int i = 0; i < parent.parent.var.var_int.Count; i++)
        {
            intVarNames.Add("システム整数変数 " + parent.parent.var.var_int[i].name);
        }
        // 文字列
        for (int i = 0; i < parent.parent.var.var_str.Count; i++)
        {
            strVarNames.Add("システム文字列変数 " + parent.parent.var.var_str[i].name);
        }
    }

    public void initCommand(int id, EventCommand evcom)
    {
        if (evcom.viewCommand == "■")
        {
            mode = 0;
            nullComFlg = true;
            selectID = id;
            return;
        }

        titleContent = new GUIContent("編集");
        selectID = id;
        mode = 1;
        string command = evcom.type;
        System.Func<string, int> getCmdIndexFunc = (string str) =>
        {
            for (int i = 0; i < commandList.Length; i++) if (commandList[i].IndexOf(str) > -1)
                return i;
            return -1;
        };

        if (command.IndexOf("分岐") > -1)
        {
            eventIf = JsonUtility.FromJson<EventIfData>(evcom.jsonCommand);
            selectCommandList = 3;
            Repaint();
            return;
        }

        switch (command)
        {
            case "文章":
                selectCommandList = getCmdIndexFunc(command);
                eventMessage = JsonUtility.FromJson<EventMessageData>(evcom.jsonCommand);
                break;
            case "画像表示":
                eventImage = JsonUtility.FromJson<EventImageData>(evcom.jsonCommand);
                selectCommandList = getCmdIndexFunc("画像操作");
                break;
            case "画像非表示":

                break;
            case "変数":
                eventVar = JsonUtility.FromJson<EventVarData>(evcom.jsonCommand);
                selectCommandList = getCmdIndexFunc(command);
                break;
            case "条件":
                eventIf = JsonUtility.FromJson<EventIfData>(evcom.jsonCommand);
                selectCommandList = getCmdIndexFunc(command);
                break;
            case "移動":
                eventMove = JsonUtility.FromJson<EventMoveData>(evcom.jsonCommand);

                if (!eventMove.moveStageSameFlag)
                {
                    if (filePaths.Count >= eventMove.selectMoveStageIndex)
                    {
                        if (filePaths[eventMove.selectMoveStageIndex] != eventMove.selectMoveStageName)
                        {
                            eventMove.selectMoveStageIndex = -1;
                            for (int i = 0; i < filePaths.Count; i++)
                            {
                                if (filePaths[i] == eventMove.selectMoveStageName)
                                {
                                    eventMove.selectMoveStageIndex = i;
                                    break;
                                }
                            }
                        }
                    }

                    if (eventMove.selectMoveStageIndex == -1)
                    {
                        if (EditorUtility.DisplayDialog("コマンドウィンドウ 警告", "指定されているマップファイルが見つかりませんでした。\nコマンドを削除しますか？", " はい ", " いいえ "))
                            parent.parent.EventChips[parent.parent.SelectEventIndex]._event.command.RemoveAt(selectID);
                        Close();
                    }
                }

                selectCommandList = getCmdIndexFunc(command);
                break;
            case "サウンド再生":
                eventSound = JsonUtility.FromJson<EventSoundData>(evcom.jsonCommand);

                if (soundFilePaths.Count >= eventSound.selectSoundIndex)
                {
                    if (soundFilePaths[eventSound.selectSoundIndex] != eventSound.soundPath)
                    {
                        eventSound.selectSoundIndex = -1;
                        for (int i = 0; i < soundFilePaths.Count; i++)
                        {
                            Debug.Log(soundFilePaths[i] + ":" + eventSound.soundPath);
                            if (soundFilePaths[i] == eventSound.soundPath)
                            {
                                eventSound.selectSoundIndex = i;
                                break;
                            }
                        }
                    }
                }

                if (eventSound.selectSoundIndex == -1)
                {
                    if (EditorUtility.DisplayDialog("コマンドウィンドウ 警告", "指定されているサウンドファイルが見つかりませんでした。\nコマンドを削除しますか？", " はい ", " いいえ "))
                        parent.parent.EventChips[parent.parent.SelectEventIndex]._event.command.RemoveAt(selectID);
                    Close();
                }

                selectCommandList = getCmdIndexFunc("サウンド");
                break;
            case "サウンド停止":
                Close();
                break;
        }
        Repaint();
    }

    void Update()
    {
        if (parent == null)
            Close();

        if (focusedWindow != null && focusedWindow.titleContent.text == "イベントウィンドウ")
            Focus();
    }

    void OnGUI()
    {
        string btName = mode == 0 ? "入力" : "OK";

        EventFold eve = parent.parent.EventChips[parent.selectID]._event;

        EditorGUILayout.BeginHorizontal();

        if (mode == 0)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(MENU_W), GUILayout.Height(Screen.height - 10));
            selectCommandList = sb.Show(new Rect(0, 0, MENU_W - 10, commandList.Length * 20), selectCommandList, selectCommandList, true, commandList, false, false, new FuncOpener(Repaint), null, null);
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();
        switch (commandList[selectCommandList])
        {
            case "文章の表示":
                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("名前", GUILayout.Height(20)))
                {
                    GUI.FocusControl("");
                    eventMessage.messageWindow_text += "[名前=\"\"]";
                }
                if (GUILayout.Button("名前非表示", GUILayout.Height(20)))
                {
                    GUI.FocusControl("");
                    eventMessage.messageWindow_text += "[名前非表示]";
                }
                if (GUILayout.Button("改行", GUILayout.Height(20)))
                {
                    GUI.FocusControl("");
                    eventMessage.messageWindow_text += "[r]";
                }
                if (GUILayout.Button("改ページ", GUILayout.Height(20)))
                {
                    GUI.FocusControl("");
                    eventMessage.messageWindow_text += "[p]";
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                List<string> mVarList = new List<string>();
                mVarList.AddRange(flgVarNames);
                mVarList.InsertRange(10, intVarNames);
                mVarList.InsertRange(20, strVarNames);
                mVarList.RemoveRange(30, mVarList.Count - 30);
                mVarList.AddRange(flgVarNames.GetRange(10, flgVarNames.Count - 10));
                mVarList.AddRange(intVarNames.GetRange(10, intVarNames.Count - 10));
                mVarList.AddRange(strVarNames.GetRange(10, strVarNames.Count - 10));
                messageSelectVarIndex = EditorGUILayout.Popup(messageSelectVarIndex, mVarList.ToArray());
                if (GUILayout.Button("変数呼び出し", GUILayout.Height(20)))
                {
                    GUI.FocusControl("");
                    eventMessage.messageWindow_text += "[変数=\"" + mVarList[messageSelectVarIndex] + "\"]";
                }
                if (GUILayout.Button("メッセージウィンドウを閉じる", GUILayout.Height(20)))
                {
                    GUI.FocusControl("");
                    eventMessage.messageWindow_text += "[閉じる]";
                }
                EditorGUILayout.EndHorizontal();

                GUIStyle style = new GUIStyle(GUI.skin.textArea);
                style.wordWrap = true;
                eventMessage.messageWindow_text = EditorGUILayout.TextArea(eventMessage.messageWindow_text, style, GUILayout.Height(Screen.height - (mode == 0 ? 120 : 80)));
                if (mode == 0)
                {
                    autoCloseMessageFlag = GUILayout.Toggle(autoCloseMessageFlag, " メッセージウィンドウを自動で閉じる");
                    continueAddFlag = GUILayout.Toggle(continueAddFlag, " 連続で入力する");
                }
                EditorGUILayout.EndVertical();
                break;
            case "画像操作":
                EditorGUILayout.BeginVertical();
                eventImage.imageName = EditorGUILayout.TextField("管理名 : ", eventImage.imageName);

                string oldName = eventImage.selectName;
                string[] paths = new string[imageFilePaths.Count];
                for (int i = 0; i < imageFilePaths.Count; i++)
                {
                    paths[i] = imageFilePaths[i].Replace("Assets/Resources/Textures/", "").Replace("\\", "/");
                }
                eventImage.selectIndex = EditorGUILayout.Popup("画像 : ", eventImage.selectIndex, paths);
                eventImage.selectName = paths[eventImage.selectIndex];

                Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Resources/Textures/" + eventImage.selectName, typeof(Texture2D));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(106), GUILayout.Height(106));
                Rect workArea = GUILayoutUtility.GetRect(10, 100, 10, 100);
                if (tex != null)
                {
                    float x = workArea.x;
                    float y = workArea.y;
                    float w = 100;
                    float h = 100;
                    if (tex.width > tex.height)
                        h = tex.height * (w / tex.width);
                    else
                        w = tex.width * (h / tex.height);
                    GUI.DrawTexture(new Rect(x, y, w, h), tex);

                    if (oldName != eventImage.selectName)
                    {
                        eventImage.w = tex.width;
                        eventImage.h = tex.height;
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.Space();
                float oldW = eventImage.w;
                float oldH = eventImage.h;
                eventImage.layerIndex = EditorGUILayout.IntField("レイヤー : ", eventImage.layerIndex);
                eventImage.x = EditorGUILayout.FloatField("X座標 : ", eventImage.x);
                eventImage.y = EditorGUILayout.FloatField("Y座標 : ", eventImage.y);
                eventImage.w = EditorGUILayout.FloatField("幅 : ", eventImage.w);
                eventImage.h = EditorGUILayout.FloatField("高さ : ", eventImage.h);
                eventImage.aspectFlg = GUILayout.Toggle(eventImage.aspectFlg, " アスペクト比を固定");
                if (eventImage.aspectFlg)
                {
                    if (oldW != eventImage.w)
                        eventImage.h = tex.height * (eventImage.w / tex.width);
                    else if (oldH != eventImage.h)
                        eventImage.w = tex.width * (eventImage.h / tex.height);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                break;
            case "変数操作":
                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Toggle(eventVar.varMode == 0, "フラグ", GUI.skin.button, GUILayout.Height(30)))
                    eventVar.varMode = 0;
                if (GUILayout.Toggle(eventVar.varMode == 1, "整数", GUI.skin.button, GUILayout.Height(30)))
                    eventVar.varMode = 1;
                if (GUILayout.Toggle(eventVar.varMode == 2, "文字列", GUI.skin.button, GUILayout.Height(30)))
                    eventVar.varMode = 2;
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(20);

                switch (eventVar.varMode)
                {
                    case 0:
                        // フラグ
                        EditorGUILayout.BeginHorizontal();
                        eventVar.selectFlgIndex = EditorGUILayout.Popup(eventVar.selectFlgIndex, flgVarNames.ToArray());
                        GUILayout.Space(30);
                        eventVar.selectOprIndex = EditorGUILayout.Popup(eventVar.selectOprIndex, new string[] { "=" }, GUILayout.Width(50));
                        GUILayout.Space(30);
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(20);
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(150));
                        if (GUILayout.Toggle(eventVar.varInputMode == 0, "手動", GUI.skin.button))
                            eventVar.varInputMode = 0;
                        if (GUILayout.Toggle(eventVar.varInputMode == 1, "変数", GUI.skin.button))
                            eventVar.varInputMode = 1;
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(20);
                        EditorGUILayout.BeginHorizontal();
                        switch (eventVar.varInputMode)
                        {
                            case 0:
                                eventVar.varSetFlg = GUILayout.Toggle(eventVar.varSetFlg, eventVar.varSetFlg ? "オン" : "オフ", GUI.skin.button, GUILayout.Width(200));
                                break;
                            case 1:
                                eventVar.selectSetFlgIndex = EditorGUILayout.Popup(eventVar.selectSetFlgIndex, flgVarNames.ToArray());
                                break;
                        }
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(20);
                        break;
                    case 1:
                        // 整数
                        EditorGUILayout.BeginHorizontal();
                        eventVar.selectIntIndex = EditorGUILayout.Popup(eventVar.selectIntIndex, intVarNames.ToArray());
                        GUILayout.Space(30);
                        eventVar.selectOprIndex = EditorGUILayout.Popup(eventVar.selectOprIndex, new string[] { "=", "+=", "-=", "×=", "÷=" }, GUILayout.Width(50));
                        GUILayout.Space(30);
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(20);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(150));
                        if (GUILayout.Toggle(eventVar.varInputMode == 0, "手動", GUI.skin.button))
                            eventVar.varInputMode = 0;
                        if (GUILayout.Toggle(eventVar.varInputMode == 1, "変数", GUI.skin.button))
                            eventVar.varInputMode = 1;
                        EditorGUILayout.EndHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(150));
                        if (GUILayout.Toggle(eventVar.varInputMode2 == 0, "手動", GUI.skin.button))
                            eventVar.varInputMode2 = 0;
                        if (GUILayout.Toggle(eventVar.varInputMode2 == 1, "変数", GUI.skin.button))
                            eventVar.varInputMode2 = 1;
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(20);
                        EditorGUILayout.BeginHorizontal();
                        switch (eventVar.varInputMode)
                        {
                            case 0:
                                eventVar.varSetInt = EditorGUILayout.IntField(eventVar.varSetInt);
                                break;
                            case 1:
                                eventVar.selectSetIntIndex = EditorGUILayout.Popup(eventVar.selectSetIntIndex, intVarNames.ToArray());
                                break;
                        }
                        GUILayout.Space(30);
                        eventVar.selectIntOprIndex = EditorGUILayout.Popup(eventVar.selectIntOprIndex, new string[] { "+", "-", "×", "÷" }, GUILayout.Width(50));
                        GUILayout.Space(30);
                        switch (eventVar.varInputMode2)
                        {
                            case 0:
                                eventVar.varSetInt2 = EditorGUILayout.IntField(eventVar.varSetInt2);
                                break;
                            case 1:
                                eventVar.selectSetIntIndex2 = EditorGUILayout.Popup(eventVar.selectSetIntIndex2, intVarNames.ToArray());
                                break;
                        }
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(20);
                        break;
                    case 2:
                        // 文字列
                        EditorGUILayout.BeginHorizontal();
                        eventVar.selectStrIndex = EditorGUILayout.Popup(eventVar.selectStrIndex, strVarNames.ToArray());
                        GUILayout.Space(30);
                        eventVar.selectOprIndex = EditorGUILayout.Popup(eventVar.selectOprIndex, new string[] { "=", "+=" }, GUILayout.Width(50));
                        GUILayout.Space(30);
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(20);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(150));
                        if (GUILayout.Toggle(eventVar.varInputMode == 0, "手動", GUI.skin.button))
                            eventVar.varInputMode = 0;
                        if (GUILayout.Toggle(eventVar.varInputMode == 1, "変数", GUI.skin.button))
                            eventVar.varInputMode = 1;
                        EditorGUILayout.EndHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(150));
                        if (GUILayout.Toggle(eventVar.varInputMode2 == 0, "手動", GUI.skin.button))
                            eventVar.varInputMode2 = 0;
                        if (GUILayout.Toggle(eventVar.varInputMode2 == 1, "変数", GUI.skin.button))
                            eventVar.varInputMode2 = 1;
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(20);
                        EditorGUILayout.BeginHorizontal();
                        switch (eventVar.varInputMode)
                        {
                            case 0:
                                eventVar.varSetStr = EditorGUILayout.TextField(eventVar.varSetStr);
                                break;
                            case 1:
                                eventVar.selectSetStrIndex = EditorGUILayout.Popup(eventVar.selectSetStrIndex, strVarNames.ToArray());
                                break;
                        }
                        GUILayout.Space(30);
                        eventVar.selectStrOprIndex = EditorGUILayout.Popup(eventVar.selectStrOprIndex, new string[] { "+" }, GUILayout.Width(50));
                        GUILayout.Space(30);
                        switch (eventVar.varInputMode2)
                        {
                            case 0:
                                eventVar.varSetStr2 = EditorGUILayout.TextField(eventVar.varSetStr2);
                                break;
                            case 1:
                                eventVar.selectSetStrIndex2 = EditorGUILayout.Popup(eventVar.selectSetStrIndex2, strVarNames.ToArray());
                                break;
                        }
                        EditorGUILayout.EndHorizontal();
                        break;
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(20);
                break;
            case "条件文":
                EditorGUILayout.BeginVertical();
                eventIf.interFlg = GUILayout.Toggle(eventIf.interFlg, " 全ての条件を連動させる");
                EditorGUILayout.Space();

                int enableCount = 0;

                for (int i = 0; i < eventIf.content.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(i == 0 || (i - 1 < 0 ? false : eventIf.content[i - 1].isEnable ? false : true));
                    eventIf.content[i].isEnable = GUILayout.Toggle(i == 0 || (i - 1 < 0 ? false : eventIf.content[i - 1].isEnable ? eventIf.content[i].isEnable : false), "", GUILayout.Width(20));
                    EditorGUI.EndDisabledGroup();
                    GUILayout.Space(10);
                    EditorGUI.BeginDisabledGroup((i != 0 && eventIf.interFlg) || !eventIf.content[i].isEnable);
                    if (GUILayout.Toggle(eventIf.content[i].mode == 0, "フラグ", GUI.skin.button))
                        eventIf.content[i].mode = 0;
                    if (GUILayout.Toggle(eventIf.content[i].mode == 1, "整数", GUI.skin.button))
                        eventIf.content[i].mode = 1;
                    if (GUILayout.Toggle(eventIf.content[i].mode == 2, "文字列", GUI.skin.button))
                        eventIf.content[i].mode = 2;
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();

                    enableCount = eventIf.content[i].isEnable ? enableCount + 1 : enableCount;

                    eventIf.content[i].mode = eventIf.interFlg ? eventIf.content[0].mode : eventIf.content[i].mode;

                    switch (eventIf.content[i].mode)
                    {
                        case 0:
                            // フラグ
                            EditorGUI.BeginDisabledGroup(!eventIf.content[i].isEnable);
                            EditorGUI.BeginDisabledGroup(i != 0 && eventIf.interFlg);
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.BeginHorizontal(GUILayout.Width(150));
                            if (GUILayout.Toggle(eventIf.content[i].varInputMode == 0, "手動", GUI.skin.button))
                                eventIf.content[i].varInputMode = 0;
                            if (GUILayout.Toggle(eventIf.content[i].varInputMode == 1, "変数", GUI.skin.button))
                                eventIf.content[i].varInputMode = 1;
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndHorizontal();
                            EditorGUI.EndDisabledGroup();
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(30);
                            EditorGUI.BeginDisabledGroup(i != 0 && eventIf.interFlg);
                            eventIf.content[i].selectVarIndex1 = EditorGUILayout.Popup(eventIf.interFlg ? eventIf.content[0].selectVarIndex1 : eventIf.content[i].selectVarIndex1, flgVarNames.ToArray());
                            EditorGUI.EndDisabledGroup();
                            GUILayout.Space(30);
                            eventIf.content[i].selectOprIndex = EditorGUILayout.Popup(eventIf.content[i].selectOprIndex, new string[] { "==", "!=" }, GUILayout.Width(50));
                            GUILayout.Space(30);
                            switch (eventIf.content[i].varInputMode)
                            {
                                case 0:
                                    eventIf.content[i].inputFlg = GUILayout.Toggle(eventIf.content[i].inputFlg, eventIf.content[i].inputFlg ? "オン" : "オフ", GUI.skin.button, GUILayout.Width(100), GUILayout.Height(15));
                                    break;
                                case 1:
                                    eventIf.content[i].selectVarIndex2 = EditorGUILayout.Popup(eventIf.content[i].selectVarIndex2, flgVarNames.ToArray());
                                    break;
                            }
                            EditorGUILayout.EndHorizontal();
                            EditorGUI.EndDisabledGroup();
                            break;
                        case 1:
                            // 整数
                            EditorGUI.BeginDisabledGroup(!eventIf.content[i].isEnable);
                            EditorGUI.BeginDisabledGroup(i != 0 && eventIf.interFlg);
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.BeginHorizontal(GUILayout.Width(150));
                            if (GUILayout.Toggle(eventIf.content[i].varInputMode == 0, "手動", GUI.skin.button))
                                eventIf.content[i].varInputMode = 0;
                            if (GUILayout.Toggle(eventIf.content[i].varInputMode == 1, "変数", GUI.skin.button))
                                eventIf.content[i].varInputMode = 1;
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndHorizontal();
                            EditorGUI.EndDisabledGroup();
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(30);
                            EditorGUI.BeginDisabledGroup(i != 0 && eventIf.interFlg);
                            eventIf.content[i].selectVarIndex1 = EditorGUILayout.Popup(eventIf.interFlg ? eventIf.content[0].selectVarIndex1 : eventIf.content[i].selectVarIndex1, intVarNames.ToArray());
                            EditorGUI.EndDisabledGroup();
                            GUILayout.Space(30);
                            eventIf.content[i].selectOprIndex = EditorGUILayout.Popup(eventIf.content[i].selectOprIndex, new string[] { "==", "!=", "<=", ">=", "<", ">" }, GUILayout.Width(50));
                            GUILayout.Space(30);
                            switch (eventIf.content[i].varInputMode)
                            {
                                case 0:
                                    eventIf.content[i].inputInt = EditorGUILayout.IntField(eventIf.content[i].inputInt, GUILayout.Width(100));
                                    break;
                                case 1:
                                    eventIf.content[i].selectVarIndex2 = EditorGUILayout.Popup(eventIf.content[i].selectVarIndex2, intVarNames.ToArray());
                                    break;
                            }
                            EditorGUILayout.EndHorizontal();
                            EditorGUI.EndDisabledGroup();
                            break;
                        case 2:
                            // 文字列
                            EditorGUI.BeginDisabledGroup(!eventIf.content[i].isEnable);
                            EditorGUI.BeginDisabledGroup(i != 0 && eventIf.interFlg);
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.BeginHorizontal(GUILayout.Width(150));
                            if (GUILayout.Toggle(eventIf.content[i].varInputMode == 0, "手動", GUI.skin.button))
                                eventIf.content[i].varInputMode = 0;
                            if (GUILayout.Toggle(eventIf.content[i].varInputMode == 1, "変数", GUI.skin.button))
                                eventIf.content[i].varInputMode = 1;
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndHorizontal();
                            EditorGUI.EndDisabledGroup();
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(30);
                            EditorGUI.BeginDisabledGroup(i != 0 && eventIf.interFlg);
                            eventIf.content[i].selectVarIndex1 = EditorGUILayout.Popup(eventIf.interFlg ? eventIf.content[0].selectVarIndex1 : eventIf.content[i].selectVarIndex1, strVarNames.ToArray());
                            EditorGUI.EndDisabledGroup();
                            GUILayout.Space(30);
                            eventIf.content[i].selectOprIndex = EditorGUILayout.Popup(eventIf.content[i].selectOprIndex, new string[] { "==", "!=" }, GUILayout.Width(50));
                            GUILayout.Space(30);
                            switch (eventIf.content[i].varInputMode)
                            {
                                case 0:
                                    eventIf.content[i].inputStr = EditorGUILayout.TextField(eventIf.content[i].inputStr, GUILayout.Width(100));
                                    break;
                                case 1:
                                    eventIf.content[i].selectVarIndex2 = EditorGUILayout.Popup(eventIf.content[i].selectVarIndex2, strVarNames.ToArray());
                                    break;
                            }
                            EditorGUILayout.EndHorizontal();
                            EditorGUI.EndDisabledGroup();
                            break;
                    }
                    EditorGUILayout.Space();
                }

                eventIf.elseFlg = GUILayout.Toggle(eventIf.elseFlg, "「上記以外の場合」を作成");

                eventIf.contNum = enableCount;

                EditorGUILayout.EndVertical();
                GUILayout.Space(20);
                break;
            case "場所移動":
                EditorGUILayout.BeginVertical();
                EditorGUI.BeginDisabledGroup(eventMove.moveStageSameFlag);
                eventMove.selectMoveStageIndex = EditorGUILayout.Popup("移動先マップ名 : ", eventMove.selectMoveStageIndex, filePaths.ToArray());
                EditorGUI.EndDisabledGroup();

                eventMove.moveStageSameFlag = GUILayout.Toggle(eventMove.moveStageSameFlag, " 同じマップ");

                GUILayout.Label("移動先座標 : ");
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("X", GUILayout.Width(40));
                eventMove.movePos.x = EditorGUILayout.IntField((int) eventMove.movePos.x);
                GUILayout.Label("Y", GUILayout.Width(40));
                eventMove.movePos.y = EditorGUILayout.IntField((int) eventMove.movePos.y);
                EditorGUILayout.EndHorizontal();
                GUILayout.Label("※ -1, -1でデフォルトスタート座標へ移動");

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("移動先を見ながら指定", GUILayout.Width(200), GUILayout.Height(80)))
                    subWindow = EventCommandMoveSelectWindow.WillAppear(this, eventMove.moveStageSameFlag ? -1 : eventMove.selectMoveStageIndex, new Vector2(eventMove.movePos.x, eventMove.movePos.y));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                break;
            case "サウンド":
                EditorGUILayout.BeginVertical();

                eventSound.soundName = EditorGUILayout.TextField("管理名 : ", eventSound.soundName);

                float oldClipLen = clip != null ? clip.length : -1;

                EditorGUILayout.BeginHorizontal();
                eventSound.selectSoundIndex = EditorGUILayout.Popup("サウンド : ", eventSound.selectSoundIndex, soundFilePaths.ToArray(), GUILayout.Width(300));
                eventSound.soundLoopFlag = GUILayout.Toggle(eventSound.soundLoopFlag, " ループ");
                EditorGUILayout.EndHorizontal();

                if (eventSound.selectSoundIndex != -1 && soundFilePaths.Count > 0)
                    clip = AssetDatabase.LoadAssetAtPath("Assets/Resources/Sounds/" + soundFilePaths[eventSound.selectSoundIndex], typeof(AudioClip)) as AudioClip;

                if (clip != null && oldClipLen == -1 || oldClipLen != clip.length)
                    eventSound.soundEndTime = clip.length;

                EditorGUI.BeginDisabledGroup(eventSound.selectSoundIndex < 0 && soundFilePaths.Count < 1);
                GUILayout.Label("開始位置 : ");
                eventSound.soundFastStartTime = EditorGUILayout.Slider(eventSound.soundFastStartTime, 0, eventSound.selectSoundIndex < 0 && soundFilePaths.Count < 1 ? 0 : clip.length);
                GUILayout.Label("ループ開始位置 : ");
                eventSound.soundStartTime = EditorGUILayout.Slider(eventSound.soundStartTime, 0, eventSound.selectSoundIndex < 0 && soundFilePaths.Count < 1 ? 0 : clip.length);
                GUILayout.Label("ループ終了位置 : ");
                eventSound.soundEndTime = EditorGUILayout.Slider(eventSound.soundEndTime, 0, eventSound.selectSoundIndex < 0 && soundFilePaths.Count < 1 ? 0 : clip.length);
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("ピッチ : ");
                eventSound.soundPitch = EditorGUILayout.Slider(eventSound.soundPitch, -3, 3);
                GUILayout.Label("音量 : ");
                eventSound.soundVolume = EditorGUILayout.Slider(eventSound.soundVolume, 0, 1);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(eventSound.isPlayFlag ? "停止" : "再生", GUILayout.Height(40)))
                {
                    if (eventSound.isPlayFlag)
                    {
                        if (audioSource != null)
                        {
                            eventSound.soundPlayTime = audioSource.time;
                            audioSource.Stop();
                            audioSource.time = eventSound.soundPlayTime;
                        }
                        eventSound.isPlayFlag = false;
                    }
                    else
                    {
                        if (GameObject.Find("EditorSystem") != null)
                            DestroyImmediate(GameObject.Find("EditorSystem"));
                        audioSource = new GameObject("EditorSystem").AddComponent<AudioSource>();
                        audioSource.clip = clip;
                        audioSource.loop = eventSound.soundLoopFlag;
                        audioSource.volume = eventSound.soundVolume;
                        audioSource.pitch = eventSound.soundPitch;
                        audioSource.time = eventSound.soundPlayTime;
                        audioSource.Play();
                        eventSound.isPlayFlag = true;
                        eventSound.isPlayOffFlag = false;
                    }
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Label("再生時間(視聴用) : ");
                eventSound.soundPlayTime = audioSource != null ? audioSource.time : eventSound.soundPlayTime;
                float oldTime = eventSound.soundPlayTime;
                eventSound.soundPlayTime = EditorGUILayout.Slider(eventSound.soundPlayTime, 0, eventSound.selectSoundIndex < 0 && soundFilePaths.Count < 1 ? 0 : clip.length);
                if (audioSource != null)
                {
                    if (oldTime != eventSound.soundPlayTime)
                        audioSource.time = eventSound.soundPlayTime;
                }
                EditorGUI.EndDisabledGroup();

                if (eventSound.isPlayFlag)
                {
                    audioSource.loop = eventSound.soundLoopFlag;
                    audioSource.volume = eventSound.soundVolume;
                    audioSource.pitch = eventSound.soundPitch;
                }

                if (audioSource != null)
                {
                    if (audioSource.time >= eventSound.soundEndTime)
                    {
                        if (eventSound.soundLoopFlag)
                            audioSource.time = eventSound.soundStartTime;
                        else
                            eventSound.isPlayFlag = false;
                    }

                    if ((audioSource.time + 1) >= eventSound.soundEndTime && audioSource.isPlaying)
                        eventSound.isPlayOffFlag = true;

                    if ((eventSound.isPlayOffFlag && !audioSource.isPlaying) || audioSource.time >= eventSound.soundEndTime)
                    {
                        eventSound.isPlayFlag = false;
                        eventSound.soundPlayTime = eventSound.soundStartTime;
                        audioSource.time = eventSound.soundStartTime;
                    }

                    if (eventSound.isPlayOffFlag && audioSource.isPlaying && eventSound.soundLoopFlag && eventSound.soundPlayTime < eventSound.soundStartTime)
                    {
                        eventSound.isPlayOffFlag = false;
                        eventSound.soundPlayTime = eventSound.soundStartTime;
                        audioSource.time = eventSound.soundStartTime;
                    }
                }

                EditorGUILayout.EndVertical();

                if (!eventSound.isPlayFlag)
                {
                    if (audioSource != null)
                    {
                        eventSound.soundPlayTime = audioSource.time;
                        audioSource.Stop();
                        audioSource.time = eventSound.soundPlayTime;
                    }

                    if (GameObject.Find("EditorSystem") != null)
                        DestroyImmediate(GameObject.Find("EditorSystem"));
                }
                else
                    Repaint();

                if (audioSource != null && audioSource.time >= eventSound.soundEndTime)
                    eventSound.isPlayFlag = false;
                break;
            case "エフェクト":
                GUILayout.Label("間に合いませんでしたごめんなさい！！！！！！！");
                break;
            case "その他":
                EditorGUILayout.BeginVertical();
                eventOther.adminName = EditorGUILayout.TextField("管理名 : ", eventOther.adminName);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("サウンド停止", GUILayout.Width(120), GUILayout.Height(30)))
                {
                    GUI.FocusControl("");
                    if (mode == 0 && !nullComFlg)
                    {
                        if (eve.selectStart != -1)
                            parent.InsertCommand(new EventCommand(JsonUtility.ToJson(eventOther), "@Color=#992;■[サウンド停止]管理名:" + eventOther.adminName, "サウンド停止"));
                        else
                            parent.InsertAddCommand(new EventCommand(JsonUtility.ToJson(eventOther), "@Color=#992;■[サウンド停止]管理名:" + eventOther.adminName, "サウンド停止"));
                    }
                    else if (nullComFlg)
                        parent.InsertCommand(new EventCommand(JsonUtility.ToJson(eventOther), "@Color=#992;■[サウンド停止]管理名:" + eventOther.adminName, "サウンド停止"));
                    parent.Repaint();
                    Close();
                }
                if (GUILayout.Button("画像非表示", GUILayout.Width(120), GUILayout.Height(30)))
                {
                    GUI.FocusControl("");
                    if (mode == 0 && !nullComFlg)
                    {
                        if (eve.selectStart != -1)
                            parent.InsertCommand(new EventCommand(JsonUtility.ToJson(eventOther), "@Color=#992;■[画像非表示]管理名:" + eventOther.adminName, "画像非表示"));
                        else
                            parent.InsertAddCommand(new EventCommand(JsonUtility.ToJson(eventOther), "@Color=#992;■[画像非表示]管理名:" + eventOther.adminName, "画像非表示"));
                    }
                    else if (nullComFlg)
                        parent.InsertCommand(new EventCommand(JsonUtility.ToJson(eventOther), "@Color=#992;■[画像非表示]管理名:" + eventOther.adminName, "画像非表示"));
                    parent.Repaint();
                    Close();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                break;
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        // 入力ボタン
        if (selectCommandList != commandList.Length - 1)
        {
            EditorGUI.BeginDisabledGroup(selectCommandList == -1);
            if (GUILayout.Button(btName, GUILayout.Height(30)))
            {
                switch (commandList[selectCommandList])
                {
                    case "文章の表示":
                        if (eventMessage.messageWindow_text != null && eventMessage.messageWindow_text != "")
                        {
                            if (mode == 0 || nullComFlg)
                                eventMessage.messageWindow_text += autoCloseMessageFlag ? "[閉じる]" : "";

                            if (mode == 0 && !nullComFlg)
                            {
                                if (eve.selectStart != -1)
                                    parent.InsertCommand(new EventCommand(JsonUtility.ToJson(eventMessage), "■[文章]" + eventMessage.messageWindow_text.Replace("\n", "\\n"), "文章"));
                                else
                                    parent.InsertAddCommand(new EventCommand(JsonUtility.ToJson(eventMessage), "■[文章]" + eventMessage.messageWindow_text.Replace("\n", "\\n"), "文章"));
                            }
                            else if (!nullComFlg)
                                parent.SetCommand(selectID, new EventCommand(JsonUtility.ToJson(eventMessage), "■[文章]" + eventMessage.messageWindow_text.Replace("\n", "\\n"), "文章"));
                            else
                                parent.InsertCommand(new EventCommand(JsonUtility.ToJson(eventMessage), "■[文章]" + eventMessage.messageWindow_text.Replace("\n", "\\n"), "文章"));
                            parent.Repaint();
                            if (continueAddFlag)
                            {
                                GUI.FocusControl("");
                                eventMessage.messageWindow_text = "";
                            }
                            else
                                Close();
                        }
                        break;
                    case "画像操作":
                        if (eventImage.imageName != "" && eventImage.imageName != null)
                        {
                            if (mode == 0 && !nullComFlg)
                            {
                                if (eve.selectStart != -1)
                                    parent.InsertCommand(new EventCommand(JsonUtility.ToJson(eventImage), "@Color=#992;■[画像表示]管理名:" + eventImage.imageName + " レイヤー:" + eventImage.layerIndex + " X:" + eventImage.x + " Y:" + eventImage.y + " W:" + eventImage.w + " H:" + eventImage.h, "画像表示"));
                                else
                                    parent.InsertAddCommand(new EventCommand(JsonUtility.ToJson(eventImage), "@Color=#992;■[画像表示]管理名:" + eventImage.imageName + " レイヤー:" + eventImage.layerIndex + " X:" + eventImage.x + " Y:" + eventImage.y + " W:" + eventImage.w + " H:" + eventImage.h, "画像表示"));
                            }
                            else if (!nullComFlg)
                                parent.SetCommand(selectID, new EventCommand(JsonUtility.ToJson(eventImage), "@Color=#992;■[画像表示]管理名:" + eventImage.imageName + " レイヤー:" + eventImage.layerIndex + " X:" + eventImage.x + " Y:" + eventImage.y + " W:" + eventImage.w + " H:" + eventImage.h, "画像表示"));
                            else
                                parent.InsertCommand(new EventCommand(JsonUtility.ToJson(eventImage), "@Color=#992;■[画像表示]管理名:" + eventImage.imageName + " レイヤー:" + eventImage.layerIndex + " X:" + eventImage.x + " Y:" + eventImage.y + " W:" + eventImage.w + " H:" + eventImage.h, "画像表示"));
                            parent.Repaint();
                            Close();
                        }
                        else
                            EditorUtility.DisplayDialog("コマンドウィンドウ 警告", "管理名が入力されていません", "OK");
                        break;
                    case "変数操作":
                        eventVar.selectFlgName = flgVarNames[eventVar.selectFlgIndex];
                        eventVar.selectIntName = intVarNames[eventVar.selectIntIndex];
                        eventVar.selectStrName = strVarNames[eventVar.selectStrIndex];
                        eventVar.selectOprName = new string[] { "=", "+=", "-=", "×=", "÷=" }[eventVar.selectOprIndex];
                        eventVar.selectIntOprName = new string[] { "+", "-", "×", "÷" }[eventVar.selectIntOprIndex];
                        eventVar.selectStrOprName = "+";
                        eventVar.selectSetFlgName = flgVarNames[eventVar.selectSetFlgIndex];
                        eventVar.selectSetIntName = intVarNames[eventVar.selectSetIntIndex];
                        eventVar.selectSetIntName2 = intVarNames[eventVar.selectSetIntIndex2];
                        eventVar.selectSetStrName = strVarNames[eventVar.selectSetStrIndex];
                        eventVar.selectSetStrName2 = strVarNames[eventVar.selectSetStrIndex2];

                        if (mode == 0 && !nullComFlg)
                        {
                            if (eve.selectStart != -1)
                                parent.InsertCommand(new EventCommand(JsonUtility.ToJson(eventVar), "@Color=#822;■[変数]" + (eventVar.varMode == 0 ? "フラグ \"" + eventVar.selectFlgName + "\" Set=" + (eventVar.varInputMode == 0 ? "" + eventVar.varSetFlg : "\"" + eventVar.selectSetFlgName + "\"") : eventVar.varMode == 1 ? "整数 \"" + eventVar.selectIntName + "\"" + eventVar.selectOprName + (eventVar.varInputMode == 0 ? "" + eventVar.varSetInt : "\"" + eventVar.selectSetIntName + "\"") + eventVar.selectIntOprName + (eventVar.varInputMode2 == 0 ? "" + eventVar.varSetInt2 : "\"" + eventVar.selectSetIntName2 + "\"") : eventVar.varMode == 2 ? "文字列 \"" + eventVar.selectStrName + "\"" + eventVar.selectOprName + (eventVar.varInputMode == 0 ? "" + eventVar.varSetStr : "\"" + eventVar.selectSetStrName + "\"") + eventVar.selectStrOprName + (eventVar.varInputMode2 == 0 ? "" + eventVar.varSetStr2 : "\"" + eventVar.selectSetStrName2 + "\"") : ""), "変数"));
                            else
                                parent.InsertAddCommand(new EventCommand(JsonUtility.ToJson(eventVar), "@Color=#822;■[変数]" + (eventVar.varMode == 0 ? "フラグ \"" + eventVar.selectFlgName + "\" Set=" + (eventVar.varInputMode == 0 ? "" + eventVar.varSetFlg : "\"" + eventVar.selectSetFlgName + "\"") : eventVar.varMode == 1 ? "整数 \"" + eventVar.selectIntName + "\"" + eventVar.selectOprName + (eventVar.varInputMode == 0 ? "" + eventVar.varSetInt : "\"" + eventVar.selectSetIntName + "\"") + eventVar.selectIntOprName + (eventVar.varInputMode2 == 0 ? "" + eventVar.varSetInt2 : "\"" + eventVar.selectSetIntName2 + "\"") : eventVar.varMode == 2 ? "文字列 \"" + eventVar.selectStrName + "\"" + eventVar.selectOprName + (eventVar.varInputMode == 0 ? "" + eventVar.varSetStr : "\"" + eventVar.selectSetStrName + "\"") + eventVar.selectStrOprName + (eventVar.varInputMode2 == 0 ? "" + eventVar.varSetStr2 : "\"" + eventVar.selectSetStrName2 + "\"") : ""), "変数"));
                        }
                        else if (!nullComFlg)
                            parent.SetCommand(selectID, new EventCommand(JsonUtility.ToJson(eventVar), "@Color=#822;■[変数]" + (eventVar.varMode == 0 ? "フラグ \"" + eventVar.selectFlgName + "\" Set=" + (eventVar.varInputMode == 0 ? "" + eventVar.varSetFlg : "\"" + eventVar.selectSetFlgName + "\"") : eventVar.varMode == 1 ? "整数 \"" + eventVar.selectIntName + "\"" + eventVar.selectOprName + (eventVar.varInputMode == 0 ? "" + eventVar.varSetInt : "\"" + eventVar.selectSetIntName + "\"") + eventVar.selectIntOprName + (eventVar.varInputMode2 == 0 ? "" + eventVar.varSetInt2 : "\"" + eventVar.selectSetIntName2 + "\"") : eventVar.varMode == 2 ? "文字列 \"" + eventVar.selectStrName + "\"" + eventVar.selectOprName + (eventVar.varInputMode == 0 ? "" + eventVar.varSetStr : "\"" + eventVar.selectSetStrName + "\"") + eventVar.selectStrOprName + (eventVar.varInputMode2 == 0 ? "" + eventVar.varSetStr2 : "\"" + eventVar.selectSetStrName2 + "\"") : ""), "変数"));
                        else
                            parent.InsertCommand(new EventCommand(JsonUtility.ToJson(eventVar), "@Color=#822;■[変数]" + (eventVar.varMode == 0 ? "フラグ \"" + eventVar.selectFlgName + "\" Set=" + (eventVar.varInputMode == 0 ? "" + eventVar.varSetFlg : "\"" + eventVar.selectSetFlgName + "\"") : eventVar.varMode == 1 ? "整数 \"" + eventVar.selectIntName + "\"" + eventVar.selectOprName + (eventVar.varInputMode == 0 ? "" + eventVar.varSetInt : "\"" + eventVar.selectSetIntName + "\"") + eventVar.selectIntOprName + (eventVar.varInputMode2 == 0 ? "" + eventVar.varSetInt2 : "\"" + eventVar.selectSetIntName2 + "\"") : eventVar.varMode == 2 ? "文字列 \"" + eventVar.selectStrName + "\"" + eventVar.selectOprName + (eventVar.varInputMode == 0 ? "" + eventVar.varSetStr : "\"" + eventVar.selectSetStrName + "\"") + eventVar.selectStrOprName + (eventVar.varInputMode2 == 0 ? "" + eventVar.varSetStr2 : "\"" + eventVar.selectSetStrName2 + "\"") : ""), "変数"));
                        parent.Repaint();
                        Close();
                        break;
                    case "条件文":
                        List<EventCommand> evIfComList = new List<EventCommand>();
                        string str = "";

                        for (int i = 0; i < eventIf.contNum; i++)
                        {
                            eventIf.content[i].selectOprName = new string[] { "==", "!=", "<=", ">=", "<", ">" }[eventIf.content[i].selectOprIndex];
                            eventIf.content[i].selectVarName1 = eventIf.content[i].mode == 0 ? flgVarNames[eventIf.content[i].selectVarIndex1] : eventIf.content[i].mode == 1 ? intVarNames[eventIf.content[i].selectVarIndex1] : eventIf.content[i].mode == 2 ? strVarNames[eventIf.content[i].selectVarIndex1] : "";
                            eventIf.content[i].selectVarName2 = eventIf.content[i].mode == 0 ? flgVarNames[eventIf.content[i].selectVarIndex2] : eventIf.content[i].mode == 1 ? intVarNames[eventIf.content[i].selectVarIndex2] : eventIf.content[i].mode == 2 ? strVarNames[eventIf.content[i].selectVarIndex2] : "";

                            str += " | \"" + eventIf.content[i].selectVarName1 + "\" " + eventIf.content[i].selectOprName + " " + (eventIf.content[i].varInputMode == 0 ? (eventIf.content[i].mode == 0 ? "" + eventIf.content[i].inputFlg : eventIf.content[i].mode == 1 ? "" + eventIf.content[i].inputInt : eventIf.content[i].mode == 2 ? eventIf.content[i].inputStr : "") : eventIf.content[i].selectVarName2);
                        }
                        eventIf.nowContNum = 0;
                        evIfComList.Add(new EventCommand(JsonUtility.ToJson(eventIf), "■[条件]" + str.Substring(3), "条件"));

                        for (int i = 0; i < eventIf.contNum; i++)
                        {
                            eventIf.nowContNum = i + 1;
                            evIfComList.Add(new EventCommand(JsonUtility.ToJson(eventIf), "@Color=#777;◇[分岐" + (i + 1) + "]\"" + eventIf.content[i].selectVarName1 + "\" " + eventIf.content[i].selectOprName + " " + (eventIf.content[i].varInputMode == 0 ? (eventIf.content[i].mode == 0 ? "" + eventIf.content[i].inputFlg : eventIf.content[i].mode == 1 ? "" + eventIf.content[i].inputInt : eventIf.content[i].mode == 2 ? eventIf.content[i].inputStr : "") : eventIf.content[i].selectVarName2), "分岐"));
                            evIfComList.Add(new EventCommand("", "■", ""));
                        }

                        if (eventIf.elseFlg)
                        {
                            eventIf.nowContNum++;
                            evIfComList.Add(new EventCommand(JsonUtility.ToJson(eventIf), "@Color=#777;◇[分岐 上記以外の場合]", "分岐 上記以外"));
                            evIfComList.Add(new EventCommand("", "■", ""));
                        }

                        eventIf.nowContNum = -1;
                        evIfComList.Add(new EventCommand(JsonUtility.ToJson(eventIf), "@Color=#777;◇分岐終了", "分岐終了"));

                        if (mode == 0 && !nullComFlg)
                        {
                            if (eve.selectStart != -1)
                                parent.InsertRangeCommand(evIfComList);
                            else
                                parent.InsertAddRangeCommand(evIfComList);
                        }
                        else if (!nullComFlg)
                            parent.SetRangeIfCommand(selectID, evIfComList);
                        else
                            parent.InsertRangeCommand(evIfComList);
                        parent.Repaint();
                        Close();
                        break;
                    case "場所移動":
                        if (eventMove.selectMoveStageIndex != -1)
                        {
                            eventMove.selectMoveStageName = (eventMove.selectMoveStageIndex != -1 ? filePaths[eventMove.selectMoveStageIndex] : "");
                            if (mode == 0 && !nullComFlg)
                            {
                                if (eve.selectStart != -1)
                                    parent.InsertCommand(new EventCommand(JsonUtility.ToJson(eventMove), "@Color=#22a;■[移動]" + (eventMove.moveStageSameFlag ? "同じマップ:" : filePaths[eventMove.selectMoveStageIndex] + ":") + eventMove.movePos.x + ":" + eventMove.movePos.y, "移動"));
                                else
                                    parent.InsertAddCommand(new EventCommand(JsonUtility.ToJson(eventMove), "@Color=#22a;■[移動]" + (eventMove.moveStageSameFlag ? "同じマップ:" : filePaths[eventMove.selectMoveStageIndex] + ":") + eventMove.movePos.x + ":" + eventMove.movePos.y, "移動"));
                            }
                            else if (!nullComFlg)
                                parent.SetCommand(selectID, new EventCommand(JsonUtility.ToJson(eventMove), "@Color=#22a;■[移動]" + (eventMove.moveStageSameFlag ? "同じマップ:" : filePaths[eventMove.selectMoveStageIndex] + ":") + eventMove.movePos.x + ":" + eventMove.movePos.y, "移動"));
                            else
                                parent.InsertCommand(new EventCommand(JsonUtility.ToJson(eventMove), "@Color=#22a;■[移動]" + (eventMove.moveStageSameFlag ? "同じマップ:" : filePaths[eventMove.selectMoveStageIndex] + ":") + eventMove.movePos.x + ":" + eventMove.movePos.y, "移動"));
                            parent.Repaint();
                            Close();
                        }
                        break;

                    case "サウンド":
                        if (eventSound.selectSoundIndex != -1)
                        {
                            if (eventSound.soundName != "" && eventSound.soundName != null)
                            {
                                eventSound.soundPath = soundFilePaths[eventSound.selectSoundIndex];
                                if (mode == 0 && !nullComFlg)
                                {
                                    if (eve.selectStart != -1)
                                        parent.InsertCommand(new EventCommand(JsonUtility.ToJson(eventSound), "@Color=#992;■[サウンド再生]管理名:" + eventSound.soundName + (eventSound.soundLoopFlag ? " ループ" : "") + " ファイル名:" + soundFilePaths[eventSound.selectSoundIndex] + " 開始時間:" + eventSound.soundFastStartTime + " ループ開始時間:" + eventSound.soundStartTime + " ループ終了時間:" + eventSound.soundEndTime + " ピッチ:" + eventSound.soundPitch + " 音量:" + eventSound.soundVolume, "サウンド再生"));
                                    else
                                        parent.InsertAddCommand(new EventCommand(JsonUtility.ToJson(eventSound), "@Color=#992;■[サウンド再生]管理名:" + eventSound.soundName + (eventSound.soundLoopFlag ? " ループ" : "") + " ファイル名:" + soundFilePaths[eventSound.selectSoundIndex] + " 開始時間:" + eventSound.soundFastStartTime + " ループ開始時間:" + eventSound.soundStartTime + " ループ終了時間:" + eventSound.soundEndTime + " ピッチ:" + eventSound.soundPitch + " 音量:" + eventSound.soundVolume, "サウンド再生"));
                                }
                                else if (!nullComFlg)
                                    parent.SetCommand(selectID, new EventCommand(JsonUtility.ToJson(eventSound), "@Color=#992;■[サウンド再生]管理名:" + eventSound.soundName + (eventSound.soundLoopFlag ? " ループ" : "") + " ファイル名:" + soundFilePaths[eventSound.selectSoundIndex] + " 開始時間:" + eventSound.soundFastStartTime + " ループ開始時間:" + eventSound.soundStartTime + " ループ終了時間:" + eventSound.soundEndTime + " ピッチ:" + eventSound.soundPitch + " 音量:" + eventSound.soundVolume, "サウンド再生"));
                                else
                                    parent.InsertCommand(new EventCommand(JsonUtility.ToJson(eventSound), "@Color=#992;■[サウンド再生]管理名:" + eventSound.soundName + (eventSound.soundLoopFlag ? " ループ" : "") + " ファイル名:" + soundFilePaths[eventSound.selectSoundIndex] + " 開始時間:" + eventSound.soundFastStartTime + " ループ開始時間:" + eventSound.soundStartTime + " ループ終了時間:" + eventSound.soundEndTime + " ピッチ:" + eventSound.soundPitch + " 音量:" + eventSound.soundVolume, "サウンド再生"));
                                parent.Repaint();
                                Close();
                            }
                            else
                                EditorUtility.DisplayDialog("コマンドウィンドウ 警告", "管理名が入力されていません", "OK");
                        }
                        break;

                    case "エフェクト":

                        break;
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        Event e = Event.current;

        if (e.type == EventType.MouseDown)
            GUI.FocusControl("");
        else if (e.type == EventType.MouseMove)
            Repaint();
    }

    public void SetParent(MapEditorEventWindow _parent)
    {
        parent = _parent;
    }

    public Vector2 MovePos
    {
        get { return eventMove.movePos; }
        set { eventMove.movePos = value; }
    }

    public int SelectMoveStageIndex
    {
        get { return eventMove.selectMoveStageIndex; }
        set { eventMove.selectMoveStageIndex = value; }
    }
}
public class EventCommandMoveSelectWindow : EditorWindow
{
    // 基本
    private const float WINDOW_W = 600;
    private const float WINDOW_H = 450;
    private const float MENU_W = 150;
    private const float LABEL_W = 100;
    private float gridSize;
    private int selectFileIndex;
    private int mapSizeX;
    private int mapSizeY;
    private int ctrlCount;
    private int ctrlCheckCount;
    private string[] filePaths;
    private bool ctrlFlag;
    private Vector2 selectVec;
    private Vector2 scrollPos;
    private Drawer d = new Drawer();
    private SelectBox sb = new SelectBox();
    private EventCommandWindow parent;
    private Rect[,] gridRect;

    // マップ
    private string[,] map;

    // イベント
    private List<MapEventChip> eventChips = new List<MapEventChip>();

    // 背景
    private MapBackgroundData bgData = new MapBackgroundData();

    public static EventCommandMoveSelectWindow WillAppear(EventCommandWindow _parent, int id, Vector2 select)
    {
        GetWindow<EventCommandMoveSelectWindow>().Close();
        EventCommandMoveSelectWindow window = CreateInstance<EventCommandMoveSelectWindow>();
        window.ShowUtility();
        window.minSize = new Vector2(WINDOW_W, WINDOW_H);
        window.titleContent = new GUIContent("移動先座標の選択");
        window.SetParent(_parent);
        window.init(id, select);
        return window;
    }

    public void init(int id, Vector2 select)
    {
        wantsMouseMove = true;
        gridSize = 20;
        selectFileIndex = id;
        selectVec = select;

        filePaths = Directory.GetFiles(parent.parent.parent.DefaultDirectory, "*.txt");

        if (id != -1)
            OpenFile();
        else
            OpenNowFile();
    }

    void Update()
    {
        if (parent == null)
            Close();

        if (focusedWindow != null && focusedWindow.titleContent.text == "コマンドウィンドウ")
            Focus();
    }

    public void GridSizeUpdater()
    {
        gridRect = CreateGrid(mapSizeY, mapSizeX);
        Repaint();
    }

    void OnGUI()
    {
        if (map == null)
            init(0, new Vector2(-1, -1));

        EditorGUILayout.BeginHorizontal();

        if (selectFileIndex != -1)
        {
            string[] names = new string[filePaths.Length];
            for (int i = 0; i < filePaths.Length; i++)
            {
                names[i] = filePaths[i].Split('/')[filePaths[i].Split('/').Length - 1].Split('.')[0];
            }


            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(MENU_W), GUILayout.Height(Screen.height - 10));
            selectFileIndex = sb.Show(new Rect(0, 0, MENU_W - 10, filePaths.Length * 20), selectFileIndex, names, new FuncOpener(Repaint), new FuncSelectBoxOpener((int id) =>
            {
                selectFileIndex = id;
                OpenFile();
            }), null);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginVertical(GUI.skin.box);
        Rect workArea = GUILayoutUtility.GetRect(10, 10000, 10, 10000);
        scrollPos = GUI.BeginScrollView(workArea, scrollPos, new Rect(0, 0, mapSizeX * gridSize + 5, mapSizeY * gridSize + 5), false, false);

        Event e = Event.current;
        Vector2 pos = e.mousePosition;
        int mouseX = -1;
        int mouseY = -1;
        int xx;

        float backSizeX = Screen.width;
        float backSizeY = Screen.height;

        if (Screen.width < gridSize * mapSizeX + 5)
        {
            backSizeX = gridSize * mapSizeX + 5;
        }

        if (Screen.height < gridSize * mapSizeY + 5)
        {
            backSizeY = gridSize * mapSizeY + 5;
        }

        EditorGUI.DrawRect(new Rect(0, 0, backSizeX, backSizeY), bgData.backcolor);

        if (bgData.background != "" && bgData.background != null)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath(bgData.background, typeof(Texture2D)) as Texture2D;

            float stageW = gridSize * mapSizeX;
            float stageH = gridSize * mapSizeY;
            float w = tex.width;
            float h = tex.height;
            float x = 0;
            float y = stageH - h * (gridSize / 64);
            int numX = 1;
            int numY = 1;

            if (bgData.loopXFlag && bgData.loopYFlag)
            {
                numX = (int)(stageW / (w * (gridSize / 64))) + 1;
                numY = (int)(stageH / (h * (gridSize / 64))) + 1;
            }
            else if (bgData.loopXFlag)
            {
                numX = (int)(stageW / (w * (gridSize / 64))) + 1;
            }
            else if (bgData.loopYFlag)
            {
                numY = (int)(stageH / (h * (gridSize / 64))) + 1;
            }

            for (int yy = 0; yy < numY; yy++)
            {
                x = 0;
                for (xx = 0; xx < numX; xx++)
                {
                    Texture2D tex2 = (Texture2D)AssetDatabase.LoadAssetAtPath(bgData.background, typeof(Texture2D));
                    GUI.DrawTexture(new Rect(x, y, w * (gridSize / 64), h * (gridSize / 64)), tex2);

                    x += w * (gridSize / 64);
                }
                y -= h * (gridSize / 64);
            }
        }

        if (bgData.foldouts != null && bgData.mode == 1)
        {
            for (int i = 0; i < bgData.objectSize; i++)
            {
                if (bgData.foldouts[i] != null)
                {
                    if (bgData.foldouts[i].obj != "" && bgData.foldouts[i].obj != null)
                    {
                        Texture2D tex = AssetDatabase.LoadAssetAtPath(bgData.foldouts[i].obj, typeof(Texture2D)) as Texture2D;

                        float stageW = gridSize * mapSizeX;
                        float stageH = gridSize * mapSizeY;
                        float w = tex.width;
                        float h = tex.height;
                        float x = bgData.foldouts[i].objectX * (gridSize / 64);
                        float y = stageH - h * (gridSize / 64) - bgData.foldouts[i].objectY * (gridSize / 64);
                        int numX = 1;
                        int numY = 1;

                        if (bgData.foldouts[i].objectIsX && bgData.foldouts[i].objectIsY)
                        {
                            numX = (int)(stageW / (w * (gridSize / 64) + bgData.foldouts[i].objectLoopX * (gridSize / 64))) + 1;
                            numY = (int)(stageH / (h * (gridSize / 64) + bgData.foldouts[i].objectLoopY * (gridSize / 64))) + 1;
                        }
                        else if (bgData.foldouts[i].objectIsX)
                        {
                            numX = (int)(stageW / (w * (gridSize / 64) + bgData.foldouts[i].objectLoopX * (gridSize / 64))) + 1;
                        }
                        else if (bgData.foldouts[i].objectIsY)
                        {
                            numY = (int)(stageH / (h * (gridSize / 64) + bgData.foldouts[i].objectLoopY * (gridSize / 64))) + 1;
                        }

                        for (int yy = 0; yy < numY; yy++)
                        {
                            x = bgData.foldouts[i].objectX * (gridSize / 64);
                            for (xx = 0; xx < numX; xx++)
                            {
                                Texture2D tex2 = (Texture2D)AssetDatabase.LoadAssetAtPath(bgData.foldouts[i].obj, typeof(Texture2D));
                                GUI.DrawTexture(new Rect(x, y, w * (gridSize / 64), h * (gridSize / 64)), tex2);

                                x += w * (gridSize / 64) + bgData.foldouts[i].objectLoopX * (gridSize / 64);
                            }
                            y -= h * (gridSize / 64) + bgData.foldouts[i].objectLoopY * (gridSize / 64);
                        }
                    }
                }
            }
        }

        for (xx = 0; xx < mapSizeX; xx++)
        {
            if (pos.x > mapSizeX * gridSize)
            {
                xx = mapSizeX - 1;
                break;
            }
            else if (pos.x < 0)
            {
                xx = 0;
                break;
            }

            Rect r = gridRect[0, xx];
            if (r.x <= pos.x && pos.x <= r.x + r.width)
                break;
        }

        for (int yy = 0; yy < mapSizeY; yy++)
        {
            if (pos.y > mapSizeY * gridSize)
            {
                yy = mapSizeY - 1;
                break;
            }
            else if (pos.y < 0)
            {
                yy = 0;
                break;
            }

            if (gridRect[yy, xx].Contains(pos))
            {
                mouseX = xx;
                mouseY = yy;
            }
        }

        int oldCtrlCount = ctrlCount;

        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.LeftControl || e.keyCode == KeyCode.RightControl)
            {
                ctrlFlag = true;
                ctrlCount++;
            }
        }
        else if (e.type == EventType.KeyUp)
        {
            ctrlCount = 0;
            ctrlCheckCount = 0;
            ctrlFlag = false;
        }

        if (ctrlCount == oldCtrlCount)
            ctrlCheckCount++;

        if (ctrlCheckCount > 20)
        {
            ctrlCount = 0;
            ctrlCheckCount = 0;
            ctrlFlag = false;
        }

        if (e.type == EventType.ScrollWheel)
        {
            // ホイールで拡大/縮小
            if (pos.x > 0 && pos.x < Screen.width && pos.y > 0 && pos.y < 520 && ctrlFlag)
            {
                if (e.delta[1] == 3)
                    gridSize = gridSize + 5;
                else if (e.delta[1] == -3)
                    gridSize = gridSize - 5;

                if (gridSize > 100)
                    gridSize = 100;
                else if (gridSize < 5)
                    gridSize = 5;

                GridSizeUpdater();
                Repaint();
                parent.Repaint();
            }
        }
        else if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (Mathf.Floor(pos.x / gridSize) * gridSize < mapSizeX * gridSize &&
                    Mathf.Floor(pos.y / gridSize) * gridSize < mapSizeY * gridSize)
                {
                    selectVec = new Vector2(Mathf.Floor(pos.x / gridSize), Mathf.Floor(pos.y / gridSize));

                    Repaint();
                }
            }
        }

        // 選択した画像を描画する
        for (int yy = 0; yy < mapSizeY; yy++)
        {
            for (xx = 0; xx < mapSizeX; xx++)
            {
                if (map[yy, xx] != null && map[yy, xx].Length > 0)
                {
                    string[] eves = map[yy, xx].Split('#');
                    string[] stas = eves[0].Split('|');
                    string path = stas[0];

                    if (!(stas[0].IndexOf("start") > -1))
                        path = stas[0];

                    Texture2D tex;

                    try
                    {
                        if (path.IndexOf("none") > -1 || path.IndexOf("start") > -1)
                            tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
                        else
                            tex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath((AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject).GetComponent<SpriteRenderer>().sprite), typeof(Texture2D));

                        GUI.DrawTexture(gridRect[yy, xx], tex);
                    }
                    catch (System.Exception exception)
                    {
                        Debug.Log(exception);
                    }
                }
            }
        }

        // グリッド線を描画する
        for (int yy = 0; yy < mapSizeY; yy++)
        {
            for (xx = 0; xx < mapSizeX; xx++)
            {
                d.DrawLine(gridRect[yy, xx], new Color(1f, 1f, 1f, 0.5f));
            }
        }

        // イベントチップの描画
        for (int i = 0; i < eventChips.Count; i++)
        {
            Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Editor/MapEditor/event_chip.png", typeof(Texture2D));

            Color oldcolor = GUI.color;
            GUI.color = new Color(oldcolor.r, oldcolor.g, oldcolor.b, oldcolor.a - 0.8f);
            GUI.DrawTexture(new Rect(eventChips[i].x * gridSize, eventChips[i].y * gridSize, gridSize, gridSize), tex);
            GUI.color = oldcolor;

            d.DrawLine(new Rect(eventChips[i].x * gridSize + (eventChips[i].rect.x * gridSize) + (gridSize / 2) - ((eventChips[i].rect.width * gridSize) / 2), eventChips[i].y * gridSize + (eventChips[i].rect.y * gridSize) + (gridSize / 2) - ((eventChips[i].rect.height * gridSize) / 2), eventChips[i].rect.width * gridSize, eventChips[i].rect.height * gridSize), new Color(1, 0, 0, 0.2f));
        }

        // 選択グリッドの描画
        if (selectVec.x != -1 && selectVec.y != -1)
            GUI.DrawTexture(new Rect(selectVec.x * gridSize, selectVec.y * gridSize, gridSize, gridSize), (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Editor/MapEditor/cmdpos_chip.png", typeof(Texture2D)));

        GUI.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        GUILayout.Label("Mouse : X " + mouseX + " / Y " + mouseY);
        GUILayout.Label("Select : X " + selectVec.x + " / Y " + selectVec.y);
        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        EditorGUI.BeginDisabledGroup(selectVec.x == -1 && selectVec.y == -1);
        if (GUILayout.Button("OK", GUILayout.Width(120), GUILayout.Height(50)))
        {
            parent.MovePos = selectVec;
            if (selectFileIndex != -1)
                parent.SelectMoveStageIndex = selectFileIndex;
            parent.Repaint();
            Close();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        if (e.type == EventType.MouseMove)
            Repaint();
    }

    private void OpenFile()
    {
        string path = filePaths[selectFileIndex];

        if (!string.IsNullOrEmpty(path))
        {
            StreamReader sr = new StreamReader(path, System.Text.Encoding.UTF8);
            MapSaveData data = JsonUtility.FromJson<MapSaveData>(sr.ReadToEnd());
            sr.Close();

            // マップ
            mapSizeX = data.map.mapSizeX;
            mapSizeY = data.map.mapSizeY;
            map = new string[mapSizeY, mapSizeX];

            int i = 0;
            for (int y = 0; y < mapSizeY; y++)
            {
                for (int x = 0; x < mapSizeX; x++)
                {
                    map[y, x] = data.map.map[i];
                    i++;
                }
            }

            gridRect = CreateGrid(mapSizeY, mapSizeX);

            // イベント
            eventChips = new List<MapEventChip>();

            for (i = 0; i < data.ev.eventChip.Length; i++)
            {
                eventChips.Add(data.ev.eventChip[i]);
            }

            // 背景
            bgData.mode = data.bg.mode;
            bgData.background = data.bg.background;
            bgData.backcolor = data.bg.backcolor;
            bgData.loopXFlag = data.bg.loopXFlag;
            bgData.loopYFlag = data.bg.loopYFlag;
            bgData.objectSize = data.bg.objectSize;

            bgData.foldouts = new FoldOut[bgData.objectSize];

            for (i = 0; i < bgData.objectSize; i++)
            {
                bgData.foldouts[i] = data.bg.foldouts[i];
            }

            Focus();
            Repaint();
        }
    }

    private void OpenNowFile()
    {
        // マップ
        mapSizeX = parent.parent.parent.GetMapSaveData.map.mapSizeX;
        mapSizeY = parent.parent.parent.GetMapSaveData.map.mapSizeY;
        map = new string[mapSizeY, mapSizeX];

        int i = 0;
        for (int y = 0; y < mapSizeY; y++)
        {
            for (int x = 0; x < mapSizeX; x++)
            {
                map[y, x] = parent.parent.parent.GetMapSaveData.map.map[i];
                i++;
            }
        }

        gridRect = CreateGrid(mapSizeY, mapSizeX);

        // イベント
        eventChips = new List<MapEventChip>();

        for (i = 0; i < parent.parent.parent.GetMapSaveData.ev.eventChip.Length; i++)
        {
            eventChips.Add(parent.parent.parent.GetMapSaveData.ev.eventChip[i]);
        }

        // 背景
        bgData.mode = parent.parent.parent.GetMapSaveData.bg.mode;
        bgData.background = parent.parent.parent.GetMapSaveData.bg.background;
        bgData.backcolor = parent.parent.parent.GetMapSaveData.bg.backcolor;
        bgData.loopXFlag = parent.parent.parent.GetMapSaveData.bg.loopXFlag;
        bgData.loopYFlag = parent.parent.parent.GetMapSaveData.bg.loopYFlag;
        bgData.objectSize = parent.parent.parent.GetMapSaveData.bg.objectSize;

        bgData.foldouts = new FoldOut[bgData.objectSize];

        for (i = 0; i < bgData.objectSize; i++)
        {
            bgData.foldouts[i] = parent.parent.parent.GetMapSaveData.bg.foldouts[i];
        }

        Focus();
        Repaint();
    }

    private Rect[,] CreateGrid(int divY, int divX)
    {
        int sizeW = divX;
        int sizeH = divY;

        float x = 0.0f;
        float y = 0.0f;
        float w = gridSize;
        float h = gridSize;

        Rect[,] resultRects = new Rect[sizeH, sizeW];

        for (int yy = 0; yy < sizeH; yy++)
        {
            x = 0.0f;
            for (int xx = 0; xx < sizeW; xx++)
            {
                Rect r = new Rect(new Vector2(x, y), new Vector2(w, h));
                resultRects[yy, xx] = r;
                x += w;
            }
            y += h;
        }

        return resultRects;
    }

    public void SetParent(EventCommandWindow _parent)
    {
        parent = _parent;
    }
}
public class Drawer
{
    public void DrawLine(Rect r, Color color)
    {
        Handles.color = color;

        // top
        Handles.DrawLine(
            new Vector2(r.position.x, r.position.y),
            new Vector2(r.position.x + r.size.x, r.position.y));
        // bottom
        Handles.DrawLine(
            new Vector2(r.position.x, r.position.y + r.size.y),
            new Vector2(r.position.x + r.size.x, r.position.y + r.size.y));
        // left
        Handles.DrawLine(
            new Vector2(r.position.x, r.position.y),
            new Vector2(r.position.x, r.position.y + r.size.y));
        // right
        Handles.DrawLine(
            new Vector2(r.position.x + r.size.x, r.position.y),
            new Vector2(r.position.x + r.size.x, r.position.y + r.size.y));
    }

    public void drawTabButton(Rect rect, string str, bool flag, Color tcol, Color col, Color lcol, FuncOpener func)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        GUIStyleState state = new GUIStyleState();
        state.textColor = tcol;
        Color color = col;
        Color color_line = lcol;
        EditorGUI.DrawRect(rect, color);
        DrawLine(rect, color_line);
        style.normal = state;
        GUI.Label(new Rect(rect.x + 10, rect.y + 2, rect.width, rect.height), EditorStyles.label.CalcSize(new GUIContent(str)).x > rect.width ? str.Substring(0, (int) rect.width / 13 - 1) : str, style);
        if (flag)
        {
            if (GUI.Button(rect, "", style))
            {
                func();
            }
        }
    }
}
public class SelectBox
{
    private int selectID;
    private int oldSelectID;
    private long time;
    private Vector2 scrollPos;

    public int Show(Rect rect, int id, string[] str, FuncOpener repaintFunc, FuncSelectBoxOpener clickFunc, FuncSelectBoxOpener doubleFunc)
    {
        return Show(rect, id, id, false, str, false, false, repaintFunc, clickFunc, doubleFunc);
    }

    public int Show(Rect rect, int id, int maxId, bool indexMinusFlg, string[] str, bool scrollXFlg, bool scrollYFlg, FuncOpener repaintFunc, FuncSelectBoxOpener clickFunc, FuncSelectBoxOpener doubleFunc)
    {
        selectID = id;

        if (str != null)
        {
            bool flag = false;
            Rect workArea = GUILayoutUtility.GetRect(10, 10000, 10, 10000);
            scrollPos = GUI.BeginScrollView(workArea, scrollPos, rect, scrollXFlg, scrollYFlg);

            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] != null)
                {
                    GUIStyleState state = new GUIStyleState();
                    GUIStyleState state2 = new GUIStyleState();
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    Color[] c = new Color[9];
                    for (int m = 0; m < 9; ++m)
                    {
                        c[m].a = 1.0f;
                        c[m].r = 0.24f;
                        c[m].g = 0.5f;
                        c[m].b = 1.0f;
                    }
                    Texture2D tex = new Texture2D(3, 3, TextureFormat.ARGB32, false);
                    tex.SetPixels(c);
                    state2.background = tex;
                    state2.textColor = Color.white;
                    Regex reg = new Regex(@"^@Color=(?<value>.*?);");
                    Color col = new Color();
                    if (ColorUtility.TryParseHtmlString(reg.Match(str[i]).Groups["value"].Value, out col))
                    {
                        state.textColor = col;
                        str[i] = str[i].Replace("@Color=" + reg.Match(str[i]).Groups["value"].Value + ";", "");
                    }
                    else
                        state.textColor = Color.black;

                    style.normal = selectID <= i && maxId >= i ? state2 : state;

                    Handles.color = new Color(0, 0, 0, 0.1f);
                    if (GUI.Button(new Rect(0, i * 19, rect.width, 19), str[i], style))
                    {
                        flag = true;

                        if (time / 10000000 + 1 > System.DateTime.Now.Ticks / 10000000 && oldSelectID == i)
                        {
                            if (doubleFunc != null)
                                doubleFunc(i);
                        }
                        else
                        {
                            GUI.FocusControl("");
                            selectID = i;
                            oldSelectID = i;
                            time = System.DateTime.Now.Ticks;

                            if (clickFunc != null)
                                clickFunc(i);
                        }

                        repaintFunc();
                    }
                }
            }
            GUI.EndScrollView();

            Event e = Event.current;
            Vector2 pos = e.mousePosition;

            if (!flag)
            {
                if (workArea.x < pos.x && workArea.x + workArea.width > pos.x &&
                    workArea.y < pos.y && workArea.y + workArea.height > pos.y)
                {
                    if (e.type == EventType.MouseDown && !indexMinusFlg)
                    {
                        selectID = -1;
                        repaintFunc();
                    }
                }
            }
        }

        return selectID;
    }

    public Vector2 GetScrollPos
    {
        get { return scrollPos; }
    }
}