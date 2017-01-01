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
    private string selectedImagePath;
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
        DrawSelectedImage();

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
        float maxW = 250.0f;

        GUILayout.Label("ツールチップ : ", GUILayout.Width(110));
        EditorGUILayout.BeginVertical(GUI.skin.box);
        Rect workArea = GUILayoutUtility.GetRect(10, 10000, 10, 50);
        ToolSelectBoxScrollPos = GUI.BeginScrollView(workArea, ToolSelectBoxScrollPos, new Rect(0, 0, 300, 50), false, false);

        string path = "Assets/Editor/MapCreater/eraser.png";
        Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        GUILayout.BeginArea(new Rect(x, y, w, h));
        if (GUILayout.Button(tex, GUILayout.MaxWidth(w), GUILayout.MaxHeight(h), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
        {
            selectedImagePath = path;
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

        GUI.EndScrollView();
        EditorGUILayout.EndVertical();

        GUILayout.Label("マップチップ : ", GUILayout.Width(110));
        EditorGUILayout.BeginVertical(GUI.skin.box);
        workArea = GUILayoutUtility.GetRect(10, 10000, 10, 300);
        ChipSelectBoxScrollPos = GUI.BeginScrollView(workArea, ChipSelectBoxScrollPos, new Rect(0, 0, 300, h * (mapChipList.Count / (maxW / h))), false, true);

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
                selectedImagePath = d;
            }
            GUILayout.EndArea();
            x += w;
        }

        GUI.EndScrollView();
        EditorGUILayout.EndVertical();

        GUILayout.Label("オブジェクト : ", GUILayout.Width(110));
        EditorGUILayout.BeginVertical(GUI.skin.box);
        workArea = GUILayoutUtility.GetRect(10, 10000, 10, 200);
        ObjectSelectBoxScrollPos = GUI.BeginScrollView(workArea, ObjectSelectBoxScrollPos, new Rect(0, 0, 0, h * (mapObjectList.Count / (maxW / h))), false, true);

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
                selectedImagePath = d;
            }
            GUILayout.EndArea();
            x2 += w;
        }

        GUI.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // 選択した画像データを表示
    private void DrawSelectedImage()
    {
		if (selectedImagePath != null)
        {
			Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(selectedImagePath, typeof(Texture2D));
			GUILayout.Label("select : " + selectedImagePath);
			GUILayout.Box(tex);
		}
        else
        {
            Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Editor/MapCreater/none.png", typeof(Texture2D));
            GUILayout.Label("select : ");
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

    public string SelectedImagePath
    {
        get { return selectedImagePath; }
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
		get{ return gridSize; }
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
    const float WINDOW_W = 850.0f;
    const float WINDOW_H = 650.0f;
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

        // グリッド線を描画する
        for (int yy = 0; yy < mapSizeY; yy++)
        {
            for (int xx = 0; xx < mapSizeX; xx++)
            {
                DrawGridLine(gridRect[yy, xx]);
            }
        }

        // クリックされた位置を探して、その場所に画像データを入れる
        Event e = Event.current;
        if (e.type == EventType.MouseDown)
        {
            Vector2 pos = Event.current.mousePosition;
            int xx;

            // x位置を先に計算して、計算回数を減らす
            for (xx = 0; xx < mapSizeX; xx++)
            {
                Rect r = gridRect[0, xx];
                if (r.x <= pos.x && pos.x <= r.x + r.width)
                    break;
            }

            // 後はy位置だけ探す
            for (int yy = 0; yy < mapSizeY; yy++)
            {
                if (gridRect[yy, xx].Contains(pos))
                {
                    if (parent.SelectedImagePath == null)
                        break;

                    // 消しゴムの時はデータを消す
                    if (parent.SelectedImagePath.IndexOf("eraser.png") > -1)
                        map[yy, xx] = "";
                    else
                        map[yy, xx] = parent.SelectedImagePath;

                    Repaint();
                    break;
                }
            }
        }
        else if (e.type == EventType.MouseDrag)
        {

        }

        // 選択した画像を描画する
        for (int yy = 0; yy < mapSizeY; yy++)
        {
            for (int xx = 0; xx < mapSizeX; xx++)
            {
                if (map[yy, xx] != null && map[yy, xx].Length > 0)
                {
                    Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(map[yy, xx], typeof(Texture2D));
                    GUI.DrawTexture(gridRect[yy, xx], tex);
                }
            }
        }
        GUI.EndScrollView();
        EditorGUILayout.EndVertical();

        // 出力ボタン
        if (GUILayout.Button("Output File", GUILayout.MinHeight(50)))
        {
            OutputFile();
        }
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

        FileInfo fileInfo = new FileInfo(path);
        StreamWriter sw = fileInfo.AppendText();
        sw.WriteLine(GetMapStrFormat());
        sw.Flush();
        sw.Close();

        // 完了ポップアップ
        EditorUtility.DisplayDialog("MapCreater", "output file success\n" + path, "ok");
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