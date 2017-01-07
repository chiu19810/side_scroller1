﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Map creater 
/// </summary>
public class MapCreater : EditorWindow
{
    private const float WINDOW_W = 300.0f;
    private const float WINDOW_H = 700.0f;
	private float gridSize = 50.0f;
	private int mapSizeX = 10;
    private int mapSizeY = 10;
    private string selectedLeftImagePath;
    private string selectedRightImagePath;
    private string areaChangeMapName;
    private string areaChangeMapX;
    private string areaChangeMapY;
    private string openFileName;
    private string chipSearchPath = "Assets/Resources/Prefabs/MapChip/";
    private string objectSearchPath = "Assets/Resources/Prefabs/MapObject/";
    private string defaultMapDirectory = "Assets/Resources/Map/";
    private string defaultBackgroundDirectory = "Assets/Resources/Map/Backgrounds/";

    private List<string> mapChipList = new List<string>();
    private List<string> mapObjectList = new List<string>();
    private Vector2 ToolSelectBoxScrollPos = Vector2.zero;
    private Vector2 ChipSelectBoxScrollPos = Vector2.zero;
    private Vector2 ObjectSelectBoxScrollPos = Vector2.zero;

	public MapCreaterSubWindow subWindow;
    public MapCreaterBackGroundWindow bgWindow;

    [UnityEditor.MenuItem("Window/MapCreater")]
	static void ShowTestMainWindow(){
        MapCreater window = (MapCreater) EditorWindow.GetWindow (typeof (MapCreater));
        window.minSize = new Vector2(WINDOW_W, WINDOW_H);
        window.init();
    }

    public void init()
    {
        mapChipList.Clear();

        string[] filePaths = Directory.GetFiles(chipSearchPath, "*.prefab");
        foreach (string filePath in filePaths)
        {
            string path = filePath.Replace("\\", "/").Replace(Application.dataPath, "");
            GameObject obj = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
            if (obj != null)
            {
                mapChipList.Add(AssetDatabase.GetAssetPath(obj.GetComponent<SpriteRenderer>().sprite));
            }
        }

        filePaths = Directory.GetFiles(objectSearchPath, "*.prefab");
        foreach (string filePath in filePaths)
        {
            string path = filePath.Replace("\\", "/").Replace(Application.dataPath, "");
            GameObject obj = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
            if (obj != null)
            {
                mapObjectList.Add(AssetDatabase.GetAssetPath(obj.GetComponent<SpriteRenderer>().sprite));
            }
        }

        openFileName = "";
        areaChangeMapName = "";
        areaChangeMapX = "-1";
        areaChangeMapY = "-1";
        selectedRightImagePath = "Assets/Editor/MapCreater/eraser.png";
    }

    void OnGUI()
    {
        // GUI
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("map size X : ", GUILayout.Width(110));
        mapSizeX = EditorGUILayout.IntField(mapSizeX);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("map size Y : ", GUILayout.Width(110));
        mapSizeY = EditorGUILayout.IntField(mapSizeY);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
		GUILayout.Label("grid size : ", GUILayout.Width(110));
		gridSize = EditorGUILayout.FloatField(gridSize);
        EditorGUILayout.EndHorizontal();
		EditorGUILayout.Space();

        GUILayout.Label(openFileName);
        EditorGUILayout.Space();

        DrawMapWindowButton();
        SelectChipBox();
        DrawSelectedImage("left");
        DrawSelectedImage("right");

        if (GUILayout.Button("Reload", GUILayout.Height(50)))
        {
            this.init();
            Repaint();

            if (subWindow != null)
                subWindow.Repaint();
        }
    }

    private void SelectChipBox()
    {
        float x = 0.0f;
        float y = 0.0f;
        float x2 = 0.0f;
        float y2 = 0.0f;
        float w = 50.0f;
        float h = 50.0f;
        float winMaxW = Screen.width - 40;
        float maxW = winMaxW - 20;

        GUILayout.Label("ツールチップ : ", GUILayout.Width(110));
        EditorGUILayout.BeginVertical(GUI.skin.box);
        Rect workArea;
        ToolSelectBoxScrollPos = EditorGUILayout.BeginScrollView(ToolSelectBoxScrollPos, true, false, GUILayout.Height(75));

        EditorGUILayout.BeginHorizontal();
        string path = "Assets/Editor/MapCreater/eraser.png";
        Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        if (GUILayout.Button(tex, GUILayout.MaxWidth(w), GUILayout.MaxHeight(h), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
        {
            Event e = Event.current;
            if (e.button == 1)
            {
                selectedRightImagePath = path;
            }
            else
            {
                selectedLeftImagePath = path;
            }
        }

        path = "Assets/Editor/MapCreater/zoom_in.png";
        tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        if (GUILayout.Button(tex, GUILayout.MaxWidth(w), GUILayout.MaxHeight(h), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
        {
            ZoomIn();
        }

        path = "Assets/Editor/MapCreater/zoom_out.png";
        tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        if (GUILayout.Button(tex, GUILayout.MaxWidth(w), GUILayout.MaxHeight(h), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
        {
            ZoomOut();
        }

        path = "Assets/Editor/MapCreater/start.png";
        tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        if (GUILayout.Button(tex, GUILayout.MaxWidth(w), GUILayout.MaxHeight(h), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
        {
            Event e = Event.current;
            if (e.button == 1)
            {
                selectedRightImagePath = path;
            }
            else
            {
                selectedLeftImagePath = path;
            }
        }

        path = "Assets/Editor/MapCreater/areachange.png";
        tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        if (GUILayout.Button(tex, GUILayout.MaxWidth(w), GUILayout.MaxHeight(h), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
        {
            Event e = Event.current;
            if (e.button == 1)
            {
                selectedRightImagePath = path;
            }
            else
            {
                selectedLeftImagePath = path;
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        GUILayout.Label("マップチップ : ", GUILayout.Width(110));
        EditorGUILayout.BeginVertical(GUI.skin.box);
        workArea = GUILayoutUtility.GetRect(10, 10000, 10, 200);
        ChipSelectBoxScrollPos = GUI.BeginScrollView(workArea, ChipSelectBoxScrollPos, new Rect(0, 0, winMaxW, h * (mapChipList.Count / (maxW / h))), false, true);

        foreach (string d in mapChipList)
        {
            if (x > maxW)
            {
                x = 0.0f;
                y += h + 4;
            }

            tex = (Texture2D)AssetDatabase.LoadAssetAtPath(d, typeof(Texture2D));

            GUILayout.BeginArea(new Rect(x, y, w, h));
            if (GUILayout.Button(tex, GUILayout.MaxWidth(w), GUILayout.MaxHeight(h), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
            {
                Event e = Event.current;
                if (e.button == 1)
                {
                    selectedRightImagePath = d + "|MapChip";
                }
                else
                {
                    selectedLeftImagePath = d + "|MapChip";
                }
            }
            GUILayout.EndArea();
            x += w + 4;
        }

        GUI.EndScrollView();
        EditorGUILayout.EndVertical();

        GUILayout.Label("オブジェクト : ", GUILayout.Width(110));
        EditorGUILayout.BeginVertical(GUI.skin.box);
        workArea = GUILayoutUtility.GetRect(10, 10000, 10, 200);
        ObjectSelectBoxScrollPos = GUI.BeginScrollView(workArea, ObjectSelectBoxScrollPos, new Rect(0, 0, winMaxW, h * (mapObjectList.Count / (maxW / h))), false, true);

        foreach (string d in mapObjectList)
        {
            if (x2 > maxW)
            {
                x2 = 0.0f;
                y2 += h + 4;
            }

            tex = (Texture2D)AssetDatabase.LoadAssetAtPath(d, typeof(Texture2D));

            GUILayout.BeginArea(new Rect(x2, y2, w, h));
            if (GUILayout.Button(tex, GUILayout.MaxWidth(w), GUILayout.MaxHeight(h), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
            {
                Event e = Event.current;
                if (e.button == 1)
                {
                    selectedRightImagePath = d + "|MapObject";
                }
                else
                {
                    selectedLeftImagePath = d + "|MapObject";
                }
            }
            GUILayout.EndArea();
            x2 += w + 4;
        }

        GUI.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // 選択した画像データを表示
    private void DrawSelectedImage(string mode)
    {
        string selectedImagePath = "";

        switch (mode)
        {
            case "left" :
                selectedImagePath = selectedLeftImagePath;
                break;
            case "right":
                selectedImagePath = selectedRightImagePath;
                break;
        }

		if (selectedImagePath != null)
        {
            selectedImagePath = selectedImagePath.Split('|')[0];
			GUILayout.Label("select " + mode + " : " + selectedImagePath);
            EditorGUILayout.BeginHorizontal();
			Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(selectedImagePath, typeof(Texture2D));
			GUILayout.Box(tex);
            if (selectedImagePath.IndexOf("areachange") > -1)
            {
                if (areaChangeMapX == "")
                    areaChangeMapX = "-1";

                if  (areaChangeMapY == "")
                    areaChangeMapY = "-1";

                EditorGUILayout.BeginVertical();
                GUILayout.Label("エリア移動先（マップ名） : ", GUILayout.Width(150));
                areaChangeMapName = (string)EditorGUILayout.TextField(areaChangeMapName);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("X（マス） : ", GUILayout.Width(150));
                areaChangeMapX = EditorGUILayout.IntField(int.Parse(areaChangeMapX)).ToString();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Y （マス）: ", GUILayout.Width(150));
                areaChangeMapY = EditorGUILayout.IntField(int.Parse(areaChangeMapY)).ToString();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
		}
        else
        {
            Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Editor/MapCreater/none.png", typeof(Texture2D));
            GUILayout.Label("select " + mode + " : ");
            GUILayout.Box(tex);
        }
    }

	// マップウィンドウを開くボタンを生成
	private void DrawMapWindowButton()
    {
        if (GUILayout.Button("Open Map Editor", GUILayout.Height(20)))
        {
            if (subWindow != null)
            {
                if (subWindow.MapSaveFlag)
                {
                    if (!EditorUtility.DisplayDialog("MapCreater 警告", "変更が保存されていませんが、新しくマップウィンドウを開きますか？", " はい ", " いいえ "))
                    {
                        return;
                    }
                }
            }

			subWindow = MapCreaterSubWindow.WillAppear(this);
			subWindow.Focus();
            openFileName = "新規マップ.map";
		}

        if (GUILayout.Button("Open BackGround Editor", GUILayout.Height(20)))
        {
            /*            if (subWindow != null)
                        {
                            if (subWindow.MapSaveFlag)
                            {
                                if (!EditorUtility.DisplayDialog("MapCreater 警告", "変更が保存されていませんが、新しくマップウィンドウを開きますか？", " はい ", " いいえ "))
                                {
                                    return;
                                }
                            }
                        }*/

            bgWindow = MapCreaterBackGroundWindow.WillAppear(this);
            bgWindow.Focus();
        }
    }

    private void ZoomIn()
    {
        gridSize += 5;
        if (gridSize > 100)
        {
            gridSize = 100;
        }

        if (subWindow)
        {
            subWindow.GridSizeUpdate();
            subWindow.Repaint();
        }
    }

    private void ZoomOut()
    {
        gridSize -= 5;
        if (gridSize < 5)
        {
            gridSize = 5;
        }

        if (subWindow)
        {
            subWindow.GridSizeUpdate();
            subWindow.Repaint();
        }
    }

    public string SelectedLeftImagePath
    {
        get { return selectedLeftImagePath; }
    }

    public string SelectedRightImagePath
    {
        get { return selectedRightImagePath; }
    }

    public void SetSelectedImagePath(string path)
    {
        selectedLeftImagePath = path;
    }

    public int MapSizeX
    {
        get { return mapSizeX; }
    }

    public int MapSizeY
    {
        get { return mapSizeY; }
    }

    public float GridSize
    {
		get { return gridSize; }
	}

    public void SetGridSize(float size)
    {
        gridSize = size;
    }

    public string AreaChangeMapName
    {
        get { return areaChangeMapName; }
    }

    public string AreaChangeMapX
    {
        get { return areaChangeMapX; }
    }

    public string AreaChangeMapY
    {
        get { return areaChangeMapY; }
    }

    public string OpenFileName
    {
        get { return openFileName; }
    }

    public void SetFileName(string name)
    {
        openFileName = name;
    }

    public string DefaultMapDirectory
    {
        get { return defaultMapDirectory; }
    }

    public string DefaultBackgroundDirectory
    {
        get { return defaultBackgroundDirectory; }
    }
}

/// <summary>
/// Map creater sub window.
/// </summary>
public class MapCreaterSubWindow : EditorWindow
{
    private const float WINDOW_W = 150.0f;
    private const float WINDOW_H = 150.0f;
    private int mapSizeX = 0;
    private int mapSizeY = 0;
    private float gridSize = 0.0f;
    private string[,] map;
    private string[,] mapSave;
    private string[,] oldMap = null;
    private bool mapSaveFlag;
    private bool prevFlag;
    private bool nextFlag;
    private bool playButtonFlag;
    private bool ctrlFlag;
    private Rect[,] gridRect;
    private MapCreater parent;
    private List<bool> mapPrevSaveFlagList = new List<bool>();
    private List<bool> mapNextSaveFlagList = new List<bool>();
    private List<string[,]> mapPrevList = new List<string[,]>();
    private List<string[,]> mapNextList = new List<string[,]>();
    private Vector2 ScrollPos = Vector2.zero;

    // サブウィンドウを開く
    public static MapCreaterSubWindow WillAppear(MapCreater _parent)
    {
        MapCreaterSubWindow window = (MapCreaterSubWindow)EditorWindow.GetWindow(typeof(MapCreaterSubWindow), false);
        window.Show();
        window.minSize = new Vector2(WINDOW_W, WINDOW_H);
        window.SetParent(_parent);
        window.init();
        return window;
    }

    private void SetParent(MapCreater _parent)
    {
        parent = _parent;
    }

    // サブウィンドウの初期化
    public void init()
    {
        mapSizeX = parent.MapSizeX;
        mapSizeY = parent.MapSizeY;
        gridSize = parent.GridSize;

        // マップデータを初期化
        map = new string[mapSizeY, mapSizeX];
        for (int i = 0; i < mapSizeY; i++)
        {
            for (int j = 0; j < mapSizeX; j++)
            {
                map[i, j] = "";
            }
        }

        // グリッドデータを生成
        gridRect = CreateGrid(mapSizeY, mapSizeX);

        mapSaveFlag = false;
        prevFlag = false;
        nextFlag = false;
        playButtonFlag = false;
        ctrlFlag = false;
        mapPrevList.Clear();
        mapNextList.Clear();
        mapPrevSaveFlagList.Clear();
        mapNextSaveFlagList.Clear();
        mapSave = (string[,]) map.Clone();
    }

    public void GridSizeUpdate()
    {
        gridSize = parent.GridSize;
        gridRect = CreateGrid(mapSizeY, mapSizeX);
    }

    void OnGUI()
    {
        PlaymodeStateObserver.OnPressedPlayButton += () =>
        {
            if (!playButtonFlag && mapSaveFlag)
            {
                mapSaveFlag = false;
                if (!EditorUtility.DisplayDialog("MapCreater 警告", "再生すると変更が破棄されます。\n保存しますか？（保存しなかった場合変更は破棄されます。）", " はい ", " いいえ "))
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
        };

        EditorGUILayout.BeginVertical(GUI.skin.box);
        Rect workArea = GUILayoutUtility.GetRect(10, 10000, 10, 520);
        ScrollPos = GUI.BeginScrollView(workArea, ScrollPos, new Rect(0, 0, mapSizeX * gridSize, mapSizeY * gridSize), false, false);
        Vector2 pos = Event.current.mousePosition;

        int mouseX = -1;
        int mouseY = -1;
        int xx;
        string status = "";

        if (gridRect == null)
        {
/*            if (!playButtonFlag)
                EditorUtility.DisplayDialog("MapCreater エラー", "MapCreaterが正常に終了されなかった為、\n編集中のマップデータが初期化されました。", "OK");*/

            playButtonFlag = false;
            parent.SetFileName("新規マップ.map");
            parent.Repaint();

            // マップデータを初期化
            map = new string[mapSizeY, mapSizeX];
            for (int i = 0; i < mapSizeY; i++)
            {
                for (int j = 0; j < mapSizeX; j++)
                {
                    map[i, j] = "";
                }
            }

            GridSizeUpdate();
        }

        // グリッド線を描画する
        for (int yy = 0; yy < mapSizeY; yy++)
        {
            for (xx = 0; xx < mapSizeX; xx++)
            {
                DrawGridLine(gridRect[yy, xx]);
            }
        }

        // x位置を先に計算して、計算回数を減らす
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

        // 後はy位置だけ探す
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

        Event e = Event.current;

        if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag && oldMap != null)
        {
            mapPrevList.Add((string[,]) oldMap.Clone());
            mapNextList.Clear();
            oldMap = null;
        }

        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.LeftControl || e.keyCode == KeyCode.RightControl))
        {
            ctrlFlag = true;
        }
        else if (e.type == EventType.KeyUp)
        {
            ctrlFlag = false;
        }

        if (e.type == EventType.ScrollWheel)
        {
            // ホイールで拡大/縮小
            if (pos.x > 0 && pos.x < Screen.width && pos.y > 0 && pos.y < 520 && ctrlFlag)
            {
                if (e.delta[1] == 3)
                {
                    parent.SetGridSize(parent.GridSize + 5);
                }
                else if (e.delta[1] == -3)
                {
                    parent.SetGridSize(parent.GridSize - 5);
                }

                if (parent.GridSize > 100)
                    parent.SetGridSize(100);
                else if (parent.GridSize < 5)
                    parent.SetGridSize(5);

                GridSizeUpdate();
                Repaint();
                parent.Repaint();
            }
        }
        else if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
        {
            if (mouseX != -1 && mouseY != -1)
            {
                // 左クリック/右クリック
                if (e.button == 0 || e.button == 1)
                {
                    string path = "";

                    if (e.button == 0)
                        path = parent.SelectedLeftImagePath;
                    else if (e.button == 1)
                        path = parent.SelectedRightImagePath;

                    if (path != null)
                    {
                        bool flag = false;
                        string[,] _oldmap = (string[,]) map.Clone();

                        if (path.IndexOf("eraser") > -1)
                        {
                            if (map[mouseY, mouseX] != "")
                            {
                                flag = true;
                                map[mouseY, mouseX] = "";
                            }
                        }
                        else if (path.IndexOf("start") > -1)
                        {
                            for (int yyy = 0; yyy < mapSizeY; yyy++)
                            {
                                for (int xxx = 0; xxx < mapSizeX; xxx++)
                                {
                                    if (map[yyy, xxx] == path)
                                    {
                                        map[yyy, xxx] = "";
                                    }
                                }
                            }

                            if (map[mouseY, mouseX] != path)
                            {
                                flag = true;
                                map[mouseY, mouseX] = path;
                            }
                        }
                        else if (path.IndexOf("areachange") > -1)
                        {
                            string areaX = parent.AreaChangeMapX;
                            string areaY = parent.AreaChangeMapY;

                            if (areaX == "" || areaX == null)
                                areaX = "-1";

                            if (areaY == "" || areaY == null)
                                areaY = "-1";

                            if (parent.AreaChangeMapName == "" || parent.AreaChangeMapName == null)
                            {
                                EditorUtility.DisplayDialog("MapCreater エラー", "移動先マップ名/X座標/Y座標が入力されていません！", "OK");
                            }
                            else
                            {
                                string set = path + "|" + parent.AreaChangeMapName + ":" + areaX + ":" + areaY;
                                if (map[mouseY, mouseX] != set)
                                {
                                    flag = true;
                                    map[mouseY, mouseX] = set;
                                }
                            }
                        }
                        else
                        {
                            if (map[mouseY, mouseX] != path)
                            {
                                flag = true;
                                map[mouseY, mouseX] = path;
                            }
                        }

                        if (flag)
                        {
                            if (e.type == EventType.mouseDown)
                            {
                                oldMap = _oldmap;
                                mapPrevSaveFlagList.Add(mapSaveFlag);
                                mapNextSaveFlagList.Clear();
                                mapSaveFlag = true;
                            }
                        }
                    }
                }
                else if (e.button == 2)
                {
                    // 中クリック
                    string[] stas = map[mouseY, mouseX].Split('|');

                    if (map[mouseY, mouseX] != "")
                    {
                        if (map[mouseY, mouseX].IndexOf("start") > -1)
                            parent.SetSelectedImagePath(map[mouseY, mouseX]);
                        else
                            parent.SetSelectedImagePath(stas[0]);

                        parent.Repaint();
                    }
                }
            }
        }

        if (e.type == EventType.MouseUp && oldMap != null)
        {
            mapPrevList.Add((string[,]) oldMap.Clone());
            mapNextList.Clear();
            oldMap = null;
        }

        if (mouseX != -1 && mouseY != -1)
        {
            string sta = map[mouseY, mouseX];
            string[] stas = sta.Split('|');

            if (stas[0].IndexOf("areachange") > -1)
            {
                string[] maps = stas[1].Split(':');
                string mapName = maps[0];
                int mapX = int.Parse(maps[1]);
                int mapY = int.Parse(maps[2]);

                status = "移動先マップ : " + mapName + " / X : " + mapX + " / Y : " + mapY;
            }
            else if (!(stas[0].IndexOf("start") > -1))
            {
                if (stas.Length > 1)
                {
                    status = stas[1];
                }
            }
            else
            {
                status = "プレイヤーのスタート地点";
            }
        }
        else
        {
            if (mouseX == -1)
                mouseX = 0;

            if (mouseY == -1)
                mouseY = 0;
        }

        // 選択した画像を描画する
        for (int yy = 0; yy < mapSizeY; yy++)
        {
            for (xx = 0; xx < mapSizeX; xx++)
            {
                if (map[yy, xx] != null && map[yy, xx].Length > 0)
                {
                    string sta = map[yy, xx];
                    string[] stas = sta.Split('|');
                    string path = map[yy, xx];

                    if (!(stas[0].IndexOf("start") > -1))
                        path = stas[0];

                    Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
                    GUI.DrawTexture(gridRect[yy, xx], tex);
                }
            }
        }
        GUI.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        GUILayout.Label("GridSize: " + gridSize + " / X : " + mouseX + " / Y : " + mouseY, GUILayout.Width(200));
        GUILayout.Label("MapChipStatus : " + status, GUILayout.Width(500));
        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();

        if (mapPrevList.Count > 0)
            prevFlag = true;
        else
            prevFlag = false;
        if (mapNextList.Count > 0)
            nextFlag = true;
        else
            nextFlag = false;

        if (!prevFlag)
            EditorGUI.BeginDisabledGroup(true);
        if (GUILayout.Button("元に戻す", GUILayout.Width(100), GUILayout.Height(30)))
        {
            mapNextList.Add((string[,]) map.Clone());
            map = (string[,]) mapPrevList[mapPrevList.Count -1].Clone();
            mapPrevList.RemoveAt(mapPrevList.Count -1);

            mapNextSaveFlagList.Add(mapSaveFlag);
            mapSaveFlag = mapPrevSaveFlagList[mapPrevSaveFlagList.Count - 1];
            mapPrevSaveFlagList.RemoveAt(mapPrevSaveFlagList.Count - 1);
        }
        if (!prevFlag)
            EditorGUI.EndDisabledGroup();

        if (!nextFlag)
            EditorGUI.BeginDisabledGroup(true);
        if (GUILayout.Button("やり直し", GUILayout.Width(100), GUILayout.Height(30)))
        {
            mapPrevList.Add((string[,]) map.Clone());
            map = (string[,]) mapNextList[mapNextList.Count -1].Clone();
            mapNextList.RemoveAt(mapNextList.Count -1);

            mapPrevSaveFlagList.Add(mapSaveFlag);
            mapSaveFlag = mapNextSaveFlagList[mapNextSaveFlagList.Count - 1];
            mapNextSaveFlagList.RemoveAt(mapNextSaveFlagList.Count - 1);
        }
        if (!nextFlag)
            EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("保存", GUILayout.MinHeight(50)))
        {
            OutputFile();
        }

        if (GUILayout.Button("開く", GUILayout.MinHeight(50)))
        {
            OpenFile();
        }
        EditorGUILayout.EndHorizontal();
        Repaint();
    }

    // グリッドデータを生成
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

    // グリッド線を描画
    private void DrawGridLine(Rect r)
    {
        // grid
        Handles.color = new Color(1f, 1f, 1f, 0.5f);

        // upper line
        Handles.DrawLine(
            new Vector2(r.position.x, r.position.y),
            new Vector2(r.position.x + r.size.x, r.position.y));

        // bottom line
        Handles.DrawLine(
            new Vector2(r.position.x, r.position.y + r.size.y),
            new Vector2(r.position.x + r.size.x, r.position.y + r.size.y));

        // left line
        Handles.DrawLine(
            new Vector2(r.position.x, r.position.y),
            new Vector2(r.position.x, r.position.y + r.size.y));

        // right line
        Handles.DrawLine(
            new Vector2(r.position.x + r.size.x, r.position.y),
            new Vector2(r.position.x + r.size.x, r.position.y + r.size.y));
    }

    // ファイルで出力
    private void OutputFile()
    {
        string path = EditorUtility.SaveFilePanel("select file", parent.DefaultMapDirectory, parent.OpenFileName, "map");

        /*        if (path == "" || path == null)
                {
                    if (EditorUtility.DisplayDialog("MapCreater エラー", "ファイルが選択されていません！\n保存をキャンセルしますか？", " はい ", " いいえ "))
                        return;
                    else
                    {
                        OutputFile();
                        return;
                    }
                }

                if (!EditorUtility.DisplayDialog("MapCreater", path + " に保存します。\nよろしいですか？", " はい ", " いいえ "))
                {
                    OutputFile();
                    return;
                }*/

        if (path == "" || path == null)
            return;

        if (System.IO.File.Exists(path))
        {
            System.IO.FileStream st = new System.IO.FileStream(path, FileMode.Open);
            st.SetLength(0);
            st.Close();
        }

        FileInfo fileInfo = new FileInfo(path);
        StreamWriter sw = fileInfo.AppendText();
        sw.WriteLine(GetMapStrFormat());
        sw.Flush();
        sw.Close();

        mapSaveFlag = false;
        mapSave = (string[,])map.Clone();

        // 完了ポップアップ
        EditorUtility.DisplayDialog("MapCreater", "保存が完了しました。\n" + path, "OK");
    }

    // ファイルを開く
    private void OpenFile()
    {
        string path = EditorUtility.OpenFilePanel("select file", parent.DefaultMapDirectory, "map");

        if (!string.IsNullOrEmpty(path))
        {
            string text = "";
            int sizeX = 0;
            int sizeY = 0;

            string[] pathdir = path.Split('/');
            string filename = pathdir[pathdir.Length - 1];

            parent.SetFileName(filename);

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
            map = new string[mapSizeY, mapSizeX];
            for (int i = 0; i < mapSizeY; i++)
            {
                for (int j = 0; j < mapSizeX; j++)
                {
                    if (text.Split('!')[i].Split(',')[j].IndexOf("start") > -1)
                        map[i, j] = "Assets/Editor/MapCreater/" + text.Split('!')[i].Split(',')[j] + ".png";
                    else if (text.Split('!')[i].Split(',')[j].IndexOf("areachange") > -1)
                        map[i, j] = "Assets/Editor/MapCreater/" + text.Split('!')[i].Split(',')[j].Split('|')[0] + ".png|" + text.Split('!')[i].Split(',')[j].Split('|')[1].Split(':')[0] + ":" + text.Split('!')[i].Split(',')[j].Split('|')[1].Split(':')[1] + ":" + text.Split('!')[i].Split(',')[j].Split('|')[1].Split(':')[2];
                    else if (text.Split('!')[i].Split(',')[j] != "")
                        map[i, j] = "Assets/Resources/Textures/" + text.Split('!')[i].Split(',')[j].Split('|')[0] + ".png|" + text.Split('!')[i].Split(',')[j].Split('|')[1];
                    else
                        map[i, j] = "";
                }
            }

            mapSave = (string[,]) map.Clone();
            gridRect = CreateGrid(mapSizeY, mapSizeX);
            Repaint();
        }
    }

    // 出力するマップデータ整形
    private string GetMapStrFormat()
    {
        string result = "";
        for (int i = 0; i < mapSizeY; i++)
        {
            for (int j = 0; j < mapSizeX; j++)
            {
                result += OutputDataFormat(map[i, j]);

                if (j < mapSizeX - 1)
                    result += ",";
            }
            result += "\n";
        }
        return result;
    }

    private string OutputDataFormat(string data)
    {
        if (data != null && data.Length > 0)
        {
            string[] tmps = data.Split('/');
            string fileName = tmps[tmps.Length - 1];
            return fileName.Replace(".png", "");
        }
        else
            return "";
    }

    public bool MapSaveFlag
    {
        get { return mapSaveFlag; }
    }
}

public class MapCreaterBackGroundWindow: EditorWindow
{
    private const float WINDOW_W = 150.0f;
    private const float WINDOW_H = 150.0f;
    private float gridSize = 0.0f;
    private int mapSizeX = 0;
    private int mapSizeY = 0;
    private int objectSize;
    private int mode;
    private bool ctrlFlag;
    private bool saveFlag;
    private bool loopModeX;
    private bool loopModeY;
    private Rect[,] gridRect;
    private MapCreater parent;
    private Vector2 ScrollPos = Vector2.zero;
    private Vector2 ScrollPos2 = Vector2.zero;
    private FoldOut[] foldout;
    private Object background;
    private Color backcolor;

    public static MapCreaterBackGroundWindow WillAppear(MapCreater _parent)
    {
        MapCreaterBackGroundWindow window = (MapCreaterBackGroundWindow)EditorWindow.GetWindow(typeof(MapCreaterBackGroundWindow), false);
        window.Show();
        window.minSize = new Vector2(WINDOW_W, WINDOW_H);
        window.SetParent(_parent);
        window.init();
        return window;
    }

    public void init()
    {
        mapSizeX = parent.MapSizeX;
        mapSizeY = parent.MapSizeY;
        gridSize = parent.GridSize;
        gridRect = CreateGrid(mapSizeY, mapSizeX);
        objectSize = 1;
        mode = 0;
        loopModeX = false;
        loopModeY = false;
        background = null;
        backcolor = new Color(119f / 255f, 211f / 255f, 255f / 255f);
        foldout = new FoldOut[objectSize];
        foldout[0] = new FoldOut();
        foldout[0].foldout = true;
    }

    public void GridSizeUpdate()
    {
        gridSize = parent.GridSize;
        gridRect = CreateGrid(mapSizeY, mapSizeX);
    }

    private class FoldOut
    {
        public bool foldout = false;
        public bool objectIsX = false;
        public bool objectIsY = false;
        public float objectX = 0;
        public float objectY = 0;
        public float objectLoopX = 0;
        public float objectLoopY = 0;
        public Object obj = null;
    }

    void OnGUI()
    {
        int oldObjectSize = objectSize;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUI.skin.box);
        Rect workArea = GUILayoutUtility.GetRect(10, 10000, 10, 525);
        ScrollPos = GUI.BeginScrollView(workArea, ScrollPos, new Rect(0, 0, mapSizeX * gridSize, mapSizeY * gridSize), false, false);
        Vector2 pos = Event.current.mousePosition;

        if (gridRect == null)
        {
//            EditorUtility.DisplayDialog("MapCreater エラー", "MapCreaterが正常に終了されなかった為、\n編集中のマップデータが初期化されました。", "OK");
            GridSizeUpdate();
        }

        float backSizeX = Screen.width - 300;
        float backSizeY = 525;

        if (800 < gridSize * mapSizeX)
        {
            backSizeX = gridSize * mapSizeX;
        }

        if (525 < gridSize * mapSizeY)
        {
            backSizeY = gridSize * mapSizeY;
        }

        EditorGUI.DrawRect(new Rect(0, 0, backSizeX, backSizeY), backcolor);

        if (background != null)
        {
            float stageW = gridSize * mapSizeX;
            float stageH = gridSize * mapSizeY;
            float w = ((Texture2D)background).width;
            float h = ((Texture2D)background).height;
            float x = 0;
            float y = stageH - h * (gridSize / 64);
            int numX = 1;
            int numY = 1;

            if (loopModeX && loopModeY)
            {
                numX = (int)(stageW / (w * (gridSize / 64))) + 1;
                numY = (int)(stageH / (h * (gridSize / 64))) + 1;
            }
            else if (loopModeX)
            {
                numX = (int)(stageW / (w * (gridSize / 64))) + 1;
            }
            else if (loopModeY)
            {
                numY = (int)(stageH / (h * (gridSize / 64))) + 1;
            }

            for (int yy = 0; yy < numY; yy++)
            {
                x = 0;
                for (int xx = 0; xx < numX; xx++)
                {
                    Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(background), typeof(Texture2D));
                    GUI.DrawTexture(new Rect(x, y, w * (gridSize / 64), h * (gridSize / 64)), tex);

                    x += w * (gridSize / 64);
                }
                y -= h * (gridSize / 64);
            }
        }

        for (int yy = 0; yy < mapSizeY; yy++)
        {
            for (int xx = 0; xx < mapSizeX; xx++)
            {
                DrawGridLine(gridRect[yy, xx]);
            }
        }

        Event e = Event.current;
        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.LeftControl || e.keyCode == KeyCode.RightControl))
        {
            ctrlFlag = true;
        }
        else if (e.type == EventType.KeyUp)
        {
            ctrlFlag = false;
        }

        if (e.type == EventType.ScrollWheel)
        {
            // ホイールで拡大/縮小
            if (pos.x > 0 && pos.x < 800 && pos.y > 0 && pos.y < 525 && ctrlFlag)
            {
                if (e.delta[1] == 3)
                {
                    parent.SetGridSize(parent.GridSize + 5);
                }
                else if (e.delta[1] == -3)
                {
                    parent.SetGridSize(parent.GridSize - 5);
                }

                if (parent.GridSize > 100)
                    parent.SetGridSize(100);
                else if (parent.GridSize < 5)
                    parent.SetGridSize(5);

                GridSizeUpdate();
                Repaint();
                parent.Repaint();
            }
        }

        if (foldout != null)
        {
            for (int i = 0; i < objectSize; i++)
            {
                if (foldout[i] != null)
                {
                    if (foldout[i].obj != null)
                    {
                        float stageW = gridSize * mapSizeX;
                        float stageH = gridSize * mapSizeY;
                        float w = ((Texture2D)foldout[i].obj).width;
                        float h = ((Texture2D)foldout[i].obj).height;
                        float x = foldout[i].objectX * (gridSize / 64);
                        float y = stageH - h * (gridSize / 64) - foldout[i].objectY * (gridSize / 64);
                        int numX = 1;
                        int numY = 1;

                        if (foldout[i].objectIsX && foldout[i].objectIsY)
                        {
                            numX = (int)(stageW / (w * (gridSize / 64) + foldout[i].objectLoopX * (gridSize / 64))) + 1;
                            numY = (int)(stageH / (h * (gridSize / 64) + foldout[i].objectLoopY * (gridSize / 64))) + 1;
                        }
                        else if (foldout[i].objectIsX)
                        {
                            numX = (int)(stageW / (w * (gridSize / 64) + foldout[i].objectLoopX * (gridSize / 64))) + 1;
                        }
                        else if (foldout[i].objectIsY)
                        {
                            numY = (int)(stageH / (h * (gridSize / 64) + foldout[i].objectLoopY * (gridSize / 64))) + 1;
                        }

                        for (int yy = 0; yy < numY; yy++)
                        {
                            x = foldout[i].objectX * (gridSize / 64);
                            for (int xx = 0; xx < numX; xx++)
                            {
                                Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(foldout[i].obj), typeof(Texture2D));
                                GUI.DrawTexture(new Rect(x, y, w * (gridSize / 64), h * (gridSize / 64)), tex);

                                x += w * (gridSize / 64) + foldout[i].objectLoopX * (gridSize / 64);
                            }
                            y -= h * (gridSize / 64) + foldout[i].objectLoopY * (gridSize / 64);
                        }
                    }
                }
            }
        }

        GUI.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUILayout.Width(300));
        ScrollPos2 = EditorGUILayout.BeginScrollView(ScrollPos2);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        mode = EditorGUILayout.Popup("モード : ", mode, new string[] { "背景ループ", "オブジェクト別ループ" });
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        background = EditorGUILayout.ObjectField("背景 : ", background, typeof(Texture2D), false);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        backcolor = EditorGUILayout.ColorField("背景色 : ", backcolor);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("ループモード : ", GUILayout.Width(100));
        GUILayout.Label("X座標");
        loopModeX = EditorGUILayout.Toggle(loopModeX);
        GUILayout.Label("Y座標");
        loopModeY = EditorGUILayout.Toggle(loopModeY);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        if (mode == 1)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("オブジェクト数 : ", GUILayout.Width(100));
            objectSize = EditorGUILayout.IntField(objectSize);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (objectSize != oldObjectSize)
            {
                if (objectSize >= 10)
                    objectSize = 10;

                FoldOut[] oldFold = foldout;
                foldout = new FoldOut[objectSize];

                for (int i = 0; i < objectSize; i++)
                {
                    if (oldFold != null)
                    {
                        if (i < oldFold.Length)
                        {
                            if (oldFold[i] != null)
                            {
                                foldout[i] = oldFold[i];
                                continue;
                            }
                        }
                    }

                    foldout[i] = new FoldOut();
                }
            }

            if (foldout != null)
            {
                for (int i = 0; i < objectSize; i++)
                {
                    if (foldout[i] != null)
                    {
                        if (foldout[i].foldout = EditorGUILayout.Foldout(foldout[i].foldout, "Object [" + i + "]"))
                        {
                            EditorGUILayout.BeginHorizontal();
                            foldout[i].obj = EditorGUILayout.ObjectField("オブジェクト : ", foldout[i].obj, typeof(Texture2D), false);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.Space();

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label("ループモード : ", GUILayout.Width(100));
                            GUILayout.Label("X座標");
                            foldout[i].objectIsX = EditorGUILayout.Toggle(foldout[i].objectIsX);
                            GUILayout.Label("Y座標");
                            foldout[i].objectIsY = EditorGUILayout.Toggle(foldout[i].objectIsY);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.Space();

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label("X座標 : ", GUILayout.Width(100));
                            foldout[i].objectX = EditorGUILayout.FloatField(foldout[i].objectX);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.Space();

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label("Y座標 : ", GUILayout.Width(100));
                            foldout[i].objectY = EditorGUILayout.FloatField(foldout[i].objectY);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.Space();

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label("ループの間隔 : ", GUILayout.Width(100));
                            GUILayout.Label("X座標");
                            foldout[i].objectLoopX = EditorGUILayout.FloatField(foldout[i].objectLoopX);
                            GUILayout.Label("Y座標");
                            foldout[i].objectLoopY = EditorGUILayout.FloatField(foldout[i].objectLoopY);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.Space();
                        }
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("保存", GUILayout.MinHeight(50)))
        {
            OutputFile();
        }

        if (GUILayout.Button("開く", GUILayout.MinHeight(50)))
        {
            OpenFile();
        }

        EditorGUILayout.EndHorizontal();
        Repaint();
    }

    // ファイルで出力
    private void OutputFile()
    {
        string path = EditorUtility.SaveFilePanel("select file", parent.DefaultBackgroundDirectory, parent.OpenFileName.Split('.')[0], "mbg");

        if (path == "" || path == null)
            return;

        if (System.IO.File.Exists(path))
        {
            System.IO.FileStream st = new System.IO.FileStream(path, FileMode.Open);
            st.SetLength(0);
            st.Close();
        }

        FileInfo fileInfo = new FileInfo(path);
        StreamWriter sw = fileInfo.AppendText();
        sw.WriteLine("");
        sw.Flush();
        sw.Close();

        saveFlag = false;

        // 完了ポップアップ
        EditorUtility.DisplayDialog("MapCreater", "保存が完了しました。\n" + path, "OK");
    }

    // ファイルを開く
    private void OpenFile()
    {
        string path = EditorUtility.OpenFilePanel("select file", parent.DefaultBackgroundDirectory, "mbg");

        if (!string.IsNullOrEmpty(path))
        {
            Repaint();
        }
    }

    private void SetParent(MapCreater _parent)
    {
        parent = _parent;
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

    private void DrawGridLine(Rect r)
    {
        Handles.color = new Color(1f, 1f, 1f, 0.5f);

        Handles.DrawLine(
            new Vector2(r.position.x, r.position.y),
            new Vector2(r.position.x + r.size.x, r.position.y));

        Handles.DrawLine(
            new Vector2(r.position.x, r.position.y + r.size.y),
            new Vector2(r.position.x + r.size.x, r.position.y + r.size.y));

        Handles.DrawLine(
            new Vector2(r.position.x, r.position.y),
            new Vector2(r.position.x, r.position.y + r.size.y));

        Handles.DrawLine(
            new Vector2(r.position.x + r.size.x, r.position.y),
            new Vector2(r.position.x + r.size.x, r.position.y + r.size.y));
    }
}