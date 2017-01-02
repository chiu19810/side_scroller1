using UnityEngine;
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
	// 出力先ディレクトリ(nullだとAssets下に出力します)
	private Object outputDirectory;
	// マップエディタのマスの数
	private int mapSizeX = 10;
    private int mapSizeY = 10;
	// グリッドの大きさ、小さいほど細かくなる
	private float gridSize = 50.0f;
	// 出力ファイル名
	private string outputFileName;
    // 選択した画像パス
    private string selectedLeftImagePath;
    private string selectedRightImagePath;
    // エリアチェンジ用
    private string areaChangeMapName;
    private string areaChangeMapX;
    private string areaChangeMapY;
    // ウィンドウのサイズ
    const float WINDOW_W = 300.0f;
    const float WINDOW_H = 700.0f;
	// サブウィンドウ
	private MapCreaterSubWindow subWindow;

    private List<string> mapChipList = new List<string>();
    private List<string> mapObjectList = new List<string>();
    private Vector2 ToolSelectBoxScrollPos = Vector2.zero;
    private Vector2 ChipSelectBoxScrollPos = Vector2.zero;
    private Vector2 ObjectSelectBoxScrollPos = Vector2.zero;
    private string chipSearchPath = "Assets/Prefabs/MapChip/";
    private string objectSearchPath = "Assets/Prefabs/MapObject/";

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

        outputDirectory = AssetDatabase.LoadMainAssetAtPath("Assets/Map");
        areaChangeMapName = "";
    }

    void OnGUI()
    {
        // GUI
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

        EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Save Directory : ", GUILayout.Width(110));
		outputDirectory = EditorGUILayout.ObjectField(outputDirectory, typeof(UnityEngine.Object), true);
        EditorGUILayout.EndHorizontal();
		EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Save filename : ", GUILayout.Width(110));
		outputFileName = (string)EditorGUILayout.TextField(outputFileName);
        EditorGUILayout.EndHorizontal();
		EditorGUILayout.Space();

        DrawMapWindowButton();
        SelectChipBox();
        DrawSelectedImage("left");
        DrawSelectedImage("right");

        if (GUILayout.Button("Chip Reload", GUILayout.Height(50)))
        {
            this.init();
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
        float maxW = winMaxW - 50;

        GUILayout.Label("ツールチップ : ", GUILayout.Width(110));
        EditorGUILayout.BeginVertical(GUI.skin.box);
        Rect workArea = GUILayoutUtility.GetRect(10, 10000, 10, 50);
        ToolSelectBoxScrollPos = GUI.BeginScrollView(workArea, ToolSelectBoxScrollPos, new Rect(0, 0, winMaxW, 50), false, false);

        string path = "Assets/Editor/MapCreater/eraser.png";
        Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        GUILayout.BeginArea(new Rect(x, y, w, h));
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
        GUILayout.EndArea();

        path = "Assets/Editor/MapCreater/zoom_in.png";
        tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        GUILayout.BeginArea(new Rect(x + w, y, w, h));
        if (GUILayout.Button(tex, GUILayout.MaxWidth(w), GUILayout.MaxHeight(h), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
        {
            ZoomIn();
        }
        GUILayout.EndArea();

        path = "Assets/Editor/MapCreater/zoom_out.png";
        tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        GUILayout.BeginArea(new Rect(x + w * 2, y, w, h));
        if (GUILayout.Button(tex, GUILayout.MaxWidth(w), GUILayout.MaxHeight(h), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
        {
            ZoomOut();
        }
        GUILayout.EndArea();

        path = "Assets/Editor/MapCreater/start.png";
        tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        GUILayout.BeginArea(new Rect(x + w * 3, y, w, h));
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
        GUILayout.EndArea();

        path = "Assets/Editor/MapCreater/areachange.png";
        tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        GUILayout.BeginArea(new Rect(x + w * 4, y, w, h));
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
        GUILayout.EndArea();

        GUI.EndScrollView();
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
                y += h;
            }

            tex = (Texture2D)AssetDatabase.LoadAssetAtPath(d, typeof(Texture2D));

            GUILayout.BeginArea(new Rect(x, y, w, h));
            if (GUILayout.Button(tex, GUILayout.MaxWidth(w), GUILayout.MaxHeight(h), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
            {
                Event e = Event.current;
                if (e.button == 1)
                {
                    selectedRightImagePath = d;
                }
                else
                {
                    selectedLeftImagePath = d;
                }
            }
            GUILayout.EndArea();
            x += w;
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
                y2 += h;
            }

            tex = (Texture2D)AssetDatabase.LoadAssetAtPath(d, typeof(Texture2D));

            GUILayout.BeginArea(new Rect(x2, y2, w, h));
            if (GUILayout.Button(tex, GUILayout.MaxWidth(w), GUILayout.MaxHeight(h), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
            {
                Event e = Event.current;
                if (e.button == 1)
                {
                    selectedRightImagePath = d;
                }
                else
                {
                    selectedLeftImagePath = d;
                }
            }
            GUILayout.EndArea();
            x2 += w;
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
			GUILayout.Label("select " + mode + " : " + selectedImagePath);
            EditorGUILayout.BeginHorizontal();
			Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(selectedImagePath, typeof(Texture2D));
			GUILayout.Box(tex);
            if (selectedImagePath.IndexOf("areachange") > -1)
            {
                EditorGUILayout.BeginVertical();
                GUILayout.Label("エリア移動先（マップ名） : ", GUILayout.Width(150));
                areaChangeMapName = (string)EditorGUILayout.TextField(areaChangeMapName);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("X（マス） : ", GUILayout.Width(150));
                areaChangeMapX = (string)EditorGUILayout.TextField(areaChangeMapX);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Y （マス）: ", GUILayout.Width(150));
                areaChangeMapY = (string)EditorGUILayout.TextField(areaChangeMapY);
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
        if (GUILayout.Button("Open Map Editor", GUILayout.Height(50)))
        {
			subWindow = MapCreaterSubWindow.WillAppear(this);
			subWindow.Focus();
		}
    }

    private void ZoomIn()
    {

    }

    private void ZoomOut()
    {

    }

    public string SelectedLeftImagePath
    {
        get { return selectedLeftImagePath; }
    }

    public string SelectedRightImagePath
    {
        get { return selectedRightImagePath; }
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

    // 出力先パスを生成
    public string OutputFilePath()
    {
		string resultPath = "";

		if(outputDirectory != null)
			resultPath = AssetDatabase.GetAssetPath(outputDirectory);
		else
			resultPath = Application.dataPath;

		return resultPath + "/" + outputFileName + ".txt"; 
	}
}

/// <summary>
/// Map creater sub window.
/// </summary>
public class MapCreaterSubWindow : EditorWindow
{
    // マップウィンドウのサイズ
    const float WINDOW_W = 150.0f;
    const float WINDOW_H = 150.0f;
    // マップのグリッド数
    private int mapSizeX = 0;
    private int mapSizeY = 0;
    // グリッドサイズ。親から値をもらう
    private float gridSize = 0.0f;
    // マップデータ
    private string[,] map;
    // グリッドの四角
    private Rect[,] gridRect;
    // 親ウィンドウの参照を持つ
    private MapCreater parent;

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
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        Rect workArea = GUILayoutUtility.GetRect(10, 10000, 10, 520);
        ScrollPos = GUI.BeginScrollView(workArea, ScrollPos, new Rect(0, 0, mapSizeX * gridSize, mapSizeY * gridSize), false, false);
        Vector2 pos = Event.current.mousePosition;

        int mouseX = -1;
        int mouseY = -1;
        int xx;
        string status = "";

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

        // クリックされた位置を探して、その場所に画像データを入れる
        Event e = Event.current;
        if (e.button == 1)
        {
            if (mouseX != -1 && mouseY != -1)
            {
                if (parent.SelectedRightImagePath != null)
                {
                    // 消しゴムの時はデータを消す
                    if (parent.SelectedRightImagePath.IndexOf("eraser") > -1)
                        map[mouseY, mouseX] = "";
                    else if (parent.SelectedRightImagePath.IndexOf("start") > -1)
                    {
                        for (int yyy = 0; yyy < mapSizeY; yyy++)
                        {
                            for (int xxx = 0; xxx < mapSizeX; xxx++)
                            {
                                if (map[yyy, xxx] == parent.SelectedRightImagePath)
                                {
                                    map[yyy, xxx] = "";
                                }
                            }
                        }

                        map[mouseY, mouseX] = parent.SelectedRightImagePath;
                    }
                    else if (parent.SelectedRightImagePath.IndexOf("areachange") > -1)
                    {
                        if ((parent.AreaChangeMapName == "" || parent.AreaChangeMapName == null) ||
                            (parent.AreaChangeMapX == "" || parent.AreaChangeMapX == null) ||
                            (parent.AreaChangeMapY == "" || parent.AreaChangeMapY == null))
                        {
                            EditorUtility.DisplayDialog("MapCreater エラー", "移動先マップ名/X座標/Y座標が入力されていません！", "ok");
                        }
                        else
                        {
                            map[mouseY, mouseX] = parent.SelectedRightImagePath + "|" + parent.AreaChangeMapName + ":" + parent.AreaChangeMapX + ":" + parent.AreaChangeMapY;
                        }
                    }
                    else
                        map[mouseY, mouseX] = parent.SelectedRightImagePath;
                }
            }
        }
        else if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
        {
            if (mouseX != -1 && mouseY != -1)
            {
                if (parent.SelectedLeftImagePath != null)
                {
                    // 消しゴムの時はデータを消す
                    if (parent.SelectedLeftImagePath.IndexOf("eraser") > -1)
                        map[mouseY, mouseX] = "";
                    else if (parent.SelectedLeftImagePath.IndexOf("start") > -1)
                    {
                        for (int yyy = 0; yyy < mapSizeY; yyy++)
                        {
                            for (int xxx = 0; xxx < mapSizeX; xxx++)
                            {
                                if (map[yyy, xxx] == parent.SelectedLeftImagePath)
                                {
                                    map[yyy, xxx] = "";
                                }
                            }
                        }

                        map[mouseY, mouseX] = parent.SelectedLeftImagePath;
                    }
                    else if (parent.SelectedLeftImagePath.IndexOf("areachange") > -1)
                    {
                        if ((parent.AreaChangeMapName == "" || parent.AreaChangeMapName == null) ||
                            (parent.AreaChangeMapX == "" || parent.AreaChangeMapX == null) ||
                            (parent.AreaChangeMapY == "" || parent.AreaChangeMapY == null))
                        {
                            EditorUtility.DisplayDialog("MapCreater エラー", "移動先マップ名/X座標/Y座標が入力されていません！", "ok");
                        }
                        else
                        {
                            map[mouseY, mouseX] = parent.SelectedLeftImagePath + "|" + parent.AreaChangeMapName + ":" + parent.AreaChangeMapX + ":" + parent.AreaChangeMapY;
                        }
                    }
                    else
                        map[mouseY, mouseX] = parent.SelectedLeftImagePath;
                }
            }
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
                    string[]  stas = sta.Split('|');
                    string path = map[yy, xx];

                    if (stas[0].IndexOf("areachange") > -1)
                    {
                        path = stas[0];
                    }

                    Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
                    GUI.DrawTexture(gridRect[yy, xx], tex);
                }
            }
        }
        GUI.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        GUILayout.Label("X : " + mouseX + " / Y : " + mouseY, GUILayout.Width(200));
        GUILayout.Label("MapChipStatus : " + status, GUILayout.Width(500));
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();
        // 出力ボタン
        if (GUILayout.Button("保存", GUILayout.MinHeight(50)))
        {
            OutputFile();
        }

        // 開くボタン
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
        string path = parent.OutputFilePath();

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

        // 完了ポップアップ
        EditorUtility.DisplayDialog("MapCreater", "output file success\n" + path, "ok");
    }

    // ファイルを開く
    private void OpenFile()
    {
        
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
            return fileName.Split('.')[0];
        }
        else
            return "";
    }
}