using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class TextController : MonoBehaviour 
{
    [SerializeField][Range(0.1f, 2.0f)]
	public float intervalForCharacterDisplay = 1.5f;
	public string[] scenarios;
	[SerializeField]
    public Text uiText1;
    public Text nameText1;
    public Text uiText2;
    public Text nameText2;

    private float timeUntilDisplay = 0;
	private float timeElapsed = 1;
    private int time = 0;
	private int currentLine = 0;
	private int lastUpdateCharacter = -1;
    private int window_mode = 0;
	private string currentText = "";
    private bool isCompleteAllTextFlag = false;
    private bool textWindowCloseFlag = false;
    private StageManager stage;
    private GameObject canvas;
    private GameObject messageBox;

    void Start()
	{
        MessageWindowManager messageMgr = GameObject.Find("SystemManager").GetComponent<MessageWindowManager>();
        stage = GameObject.Find("StageManager").GetComponent<StageManager>();
        canvas = messageMgr.getCanvas;
        messageBox = messageMgr.getMessageBox;

        SetNextLine();
	}

	void Update () 
	{
        bool input = Input.GetMouseButtonDown(0) || Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return);
        Text uiText = window_mode == 0 ? uiText1 : window_mode == 1 ? uiText2 : null;

        if (currentText == "")
        {
            messageBox.SetActive(false);
            isCompleteAllTextFlag = true;
            return;
        }

        int displayCharacterCount = (int)(Mathf.Clamp01((time - timeElapsed) / timeUntilDisplay) * currentText.Length);
        if (displayCharacterCount != lastUpdateCharacter)
        {
            uiText.text = currentText.Substring(0, displayCharacterCount);
            lastUpdateCharacter = displayCharacterCount;
        }

        // 文字の表示が完了してるならクリック時に次の行を表示する
        if (IsCompleteDisplayText)
        {
            if (currentLine < scenarios.Length && input)
                SetNextLine();
            else if (currentLine >= scenarios.Length && input)
            {
                if (textWindowCloseFlag)
                {
                    messageBox.SetActive(false);
                    uiText1.text = "";
                    uiText2.text = "";
                    nameText1.text = "";
                    nameText2.text = "";
                    isCompleteAllTextFlag = true;
                }
                else
                {
                    isCompleteAllTextFlag = true;
                }
            }
        }
        else
        {
		    // 完了してないなら文字をすべて表示する
			if(input)
            {
				timeUntilDisplay = 0;
			}
		}
        time++;
	}

	void SetNextLine()
	{
		currentText = scenarios[currentLine];

        // コマンド置換
        if (currentLine < scenarios.Length)
            currentText = CommandReplace(currentText);

        Text uiText = window_mode == 0 ? uiText1 : window_mode == 1 ? uiText2 : null;
        uiText.color = new Color(1, 1, 1, 1);

        timeUntilDisplay = currentText.Length * intervalForCharacterDisplay;
		timeElapsed = time;
		currentLine ++;
		lastUpdateCharacter = -1;
	}

    // 文字の表示が完了しているかどうか
    public bool IsCompleteDisplayText
    {
        get { return time > timeElapsed + timeUntilDisplay; }
    }

    // 全ての文字を表示し終わったかどうか
    public bool IsCompleteAllText
    {
        get { return isCompleteAllTextFlag; }
    }

    private string CommandReplace(string str)
    {
        string result = str
            .Replace("[r]", "\n")
        ;

        Regex reg = new Regex(@"\[(?<value>.*?)\]");
        while (true)
        {
            Text nameText = window_mode == 0 ? nameText1 : window_mode == 1 ? nameText2 : null;

            string cmd = reg.Match(result).Groups["value"].Value;
            if (cmd == "")
                break;

            if (cmd.IndexOf("名前非表示") > -1)
            {
                nameText.transform.parent.gameObject.SetActive(false);
            }
            else if (cmd.IndexOf("名前") > -1)
            {
                nameText.transform.parent.gameObject.SetActive(true);
                nameText.text = cmd.Split('=')[1].Replace("\"", "");
            }
            else if (cmd.IndexOf("閉じる") > -1)
            {
                textWindowCloseFlag = true;
            }
            else if (cmd.IndexOf("ウィンドウ1") > -1)
            {
                window_mode = 0;
                uiText2.color = new Color(1, 1, 1, 0.5f);
            }
            else if (cmd.IndexOf("ウィンドウ2") > -1)
            {
                window_mode = 1;
                uiText1.color = new Color(1, 1, 1, 0.5f);
            }
            else if (cmd.IndexOf("変数") > -1)
            {
                if (cmd.IndexOf("ローカル") > -1)
                {
                    if (cmd.IndexOf("フラグ") > -1)
                    {
                        for (int v = 0; v < stage.FlgVar.Length; v++)
                        {
                            if (cmd.Split('=')[1].Replace("\"", "") == "ローカルフラグ変数 " + stage.FlgVar[v].name)
                                result = result.Replace("[" + cmd + "]", "" + stage.FlgVar[v].var);
                        }
                    }
                    else if (cmd.IndexOf("整数") > -1)
                    {
                        for (int v = 0; v < stage.IntVar.Length; v++)
                        {
                            if (cmd.Split('=')[1].Replace("\"", "") == "ローカル整数変数 " + stage.IntVar[v].name)
                                result = result.Replace("[" + cmd + "]", "" + stage.IntVar[v].var);
                        }
                    }
                    else if (cmd.IndexOf("文字列") > -1)
                    {
                        for (int v = 0; v < stage.StrVar.Length; v++)
                        {
                            if (cmd.Split('=')[1].Replace("\"", "") == "ローカル文字列変数 " + stage.StrVar[v].name)
                                result = result.Replace("[" + cmd + "]", "" + stage.StrVar[v].var);
                        }
                    }
                }
                else if (cmd.IndexOf("システム") > -1)
                {
                    if (cmd.IndexOf("フラグ") > -1)
                    {
                        for (int v = 0; v < stage.Var.var_flg.Count; v++)
                        {
                            if (cmd.Split('=')[1].Replace("\"", "") == "システムフラグ変数 " + stage.Var.var_flg[v].name)
                                result = result.Replace("[" + cmd + "]", "" + stage.Var.var_flg[v].var);
                        }
                    }
                    else if (cmd.IndexOf("整数") > -1)
                    {
                        for (int v = 0; v < stage.Var.var_int.Count; v++)
                        {
                            if (cmd.Split('=')[1].Replace("\"", "") == "システム整数変数 " + stage.Var.var_int[v].name)
                                result = result.Replace("[" + cmd + "]", "" + stage.Var.var_int[v].var);
                        }
                    }
                    else if (cmd.IndexOf("文字列") > -1)
                    {
                        for (int v = 0; v < stage.Var.var_str.Count; v++)
                        {
                            if (cmd.Split('=')[1].Replace("\"", "") == "システム文字列変数 " + stage.Var.var_str[v].name)
                                result = result.Replace("[" + cmd + "]", "" + stage.Var.var_str[v].var);
                        }
                    }
                }
            }
            result = result.Replace("[" + cmd + "]", "");
        }

        return result;
    }
}
