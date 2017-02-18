using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class EventManager : MonoBehaviour
{
    public LayerMask playerLayer;
    public MapEventChip eventData;
    public int mode;

    private int evMode;
    private int counter;
    private int ifCounter;
    private List<List<EventIfData>> ifData = new List<List<EventIfData>>();
    private List<EventCommand> command = new List<EventCommand>();
    private List<bool> cmdSkipFlg = new List<bool>();
    private List<bool> cmdSkipCheckFlg = new List<bool>();
    private bool isShowWindow;
    private bool eventComplete;
    private bool moveFlag;
    private GameObject canvas;
    private GameObject messageBox;
    private TextController textController;
    private Text uiText;
    private Text nameText;
    private StageManager stage;

    public static Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

    void Start()
    {
        MessageWindowManager messageMgr = GameObject.Find("SystemManager").GetComponent<MessageWindowManager>();
        stage = GameObject.Find("StageManager").GetComponent<StageManager>();
        canvas = messageMgr.getCanvas;
        messageBox = messageMgr.getMessageBox;
        uiText = messageMgr.getText;
        nameText = messageMgr.getNameText;
        init();

        // 自動実行
        if (mode == 0 && !eventComplete)
            command = eventData._event.command;
    }

    private void init()
    {
        moveFlag = false;
        eventComplete = false;
        isShowWindow = false;
        counter = 0;
        ifCounter = 0;
    }

    void Update()
    {
        if (!eventComplete && command != null && command.Count > 0)
        {
            int i = counter;

            if (command.Count > i && command[i] != null && command[i].viewCommand == "■")
            {
                counter++;
                return;
            }

            if (counter >= command.Count)
            {
                eventComplete = true;
                GameObject tm = GameObject.Find("TextManager");
                if (tm != null)
                    Destroy(tm);
                command = null;
                stage.GetPlayer.GetComponent<PlayerController>().PlayerMoveFlag = true;
                if (moveFlag)
                    Destroy(GameObject.Find("Event"));
                init();
                return;
            }

            string cmd = command[i].type;

            if (ifCounter > 0 && ifData.Count > ifCounter - 1 && ifData[ifCounter - 1].Count > 0 && !(cmd.IndexOf("分岐") > -1) && !cmdSkipCheckFlg[ifCounter - 1])
            {
                EventIfData eventData = ifData[ifCounter - 1][ifData[ifCounter - 1].Count - 1];
                int count = eventData.contNum;
                int now = eventData.nowContNum - 1;

                if (eventData.nowContNum <= eventData.contNum)
                {
                    EventIfContent content = eventData.content[now];
                    int ifMode = content.mode;
                    string opr = content.selectOprName;

                    System.Func<object, object, bool> OprFunc = (object var1, object var2) =>
                    {
                        switch (opr)
                        {
                            case "==":
                                return var1.Equals(var2);
                            case "!=":
                                return !var1.Equals(var2);
                            case "<=":
                                return (int)var1 <= (int)var2;
                            case ">=":
                                return (int)var1 >= (int)var2;
                            case "<":
                                return (int)var1 < (int)var2;
                            case ">":
                                return (int)var1 > (int)var2;
                        }
                        return false;
                    };

                    switch (ifMode)
                    {
                        case 0:
                            bool setFlg1 = false;
                            bool setFlg2 = false;
                            string name1 = content.selectVarName1;
                            string name2 = content.selectVarName2;

                            for (int j = 0; j < (10 > stage.Var.var_flg.Count ? 10 : stage.Var.var_flg.Count); j++)
                            {
                                System.Func<string, bool> SetFunc1 = (string nam) => j < stage.FlgVar.Length && "ローカルフラグ変数 " + stage.FlgVar[j].name == nam ? stage.FlgVar[j].var : false;
                                System.Func<string, bool> SetFunc2 = (string nam) => j < stage.Var.var_flg.Count && "システムフラグ変数 " + stage.Var.var_flg[j].name == nam ? stage.Var.var_flg[j].var : false;
                                setFlg1 = setFlg1 ? true : name1.IndexOf("ローカル") > -1 ? SetFunc1(name1) : SetFunc2(name1);
                                setFlg2 = setFlg2 ? true : content.varInputMode == 0 ? content.inputFlg : name2.IndexOf("ローカル") > -1 ? SetFunc1(name2) : SetFunc2(name2);
                            }

                            if (!OprFunc(setFlg1, setFlg2))
                            {
                                counter++;
                                return;
                            }
                            break;
                        case 1:
                            int setInt1 = 0;
                            int setInt2 = 0;
                            name1 = content.selectVarName1;
                            name2 = content.selectVarName2;

                            for (int j = 0; j < (10 > stage.Var.var_int.Count ? 10 : stage.Var.var_int.Count); j++)
                            {
                                System.Func<string, int> SetFunc1 = (string nam) => j < stage.IntVar.Length && "ローカル整数変数 " + stage.IntVar[j].name == nam ? stage.IntVar[j].var : 0;
                                System.Func<string, int> SetFunc2 = (string nam) => j < stage.Var.var_int.Count && "システム整数変数 " + stage.Var.var_int[j].name == nam ? stage.Var.var_int[j].var : 0;
                                setInt1 = setInt1 != 0 ? setInt1 : name1.IndexOf("ローカル") > -1 ? SetFunc1(name1) : SetFunc2(name1);
                                setInt2 = setInt2 != 0 ? setInt2 : content.varInputMode == 0 ? content.inputInt : name2.IndexOf("ローカル") > -1 ? SetFunc1(name2) : SetFunc2(name2);
                            }

                            if (!OprFunc(setInt1, setInt2))
                            {
                                counter++;
                                return;
                            }
                            break;
                        case 2:
                            string setStr1 = "";
                            string setStr2 = "";
                            name1 = content.selectVarName1;
                            name2 = content.selectVarName2;

                            for (int j = 0; j < (10 > stage.Var.var_str.Count ? 10 : stage.Var.var_str.Count); j++)
                            {
                                System.Func<string, string> SetFunc1 = (string nam) => j < stage.StrVar.Length && "ローカル文字列変数 " + stage.StrVar[j].name == nam ? stage.StrVar[j].var : "";
                                System.Func<string, string> SetFunc2 = (string nam) => j < stage.Var.var_str.Count && "システム文字列変数 " + stage.Var.var_str[j].name == nam ? stage.Var.var_str[j].var : "";
                                setStr1 = setStr1 != "" ? setStr1 : name1.IndexOf("ローカル") > -1 ? SetFunc1(name1) : SetFunc2(name1);
                                setStr2 = setStr2 != "" ? setStr2 : content.varInputMode == 0 ? content.inputStr : name2.IndexOf("ローカル") > -1 ? SetFunc1(name2) : SetFunc2(name2);
                            }

                            if (!OprFunc(setStr1, setStr2))
                            {
                                counter++;
                                return;
                            }
                            break;
                    }
                    cmdSkipCheckFlg[ifCounter - 1] = true;
                }
            }

            if (!(cmd.IndexOf("分岐終了") > -1) && !(cmd.IndexOf("条件") > -1))
            {
                if (ifCounter > 0 && cmdSkipFlg[ifCounter - 1])
                {
                    counter++;
                    return;
                }
            }

            if (cmd.IndexOf("文章") > -1)
            {
                // 文章の表示
                if (!isShowWindow)
                {
                    EventMessageData data = JsonUtility.FromJson<EventMessageData>(command[i].jsonCommand);

                    // 改ページ
                    string[] messages = data.messageWindow_text.Replace("\n", "").Split(new string[] { "[p]" }, System.StringSplitOptions.None);

                    isShowWindow = true;
                    messageBox.SetActive(true);
                    Destroy(GameObject.Find("TextManager"));
                    GameObject textManager = new GameObject("TextManager");
                    textManager.AddComponent<TextController>();
                    textController = textManager.GetComponent<TextController>();
                    textController.scenarios = messages;
                    textController.uiText = uiText;
                    textController.nameText = nameText;
                    stage.GetPlayer.GetComponent<PlayerController>().PlayerMoveFlag = false;
                }
                else
                {
                    if (textController.IsCompleteAllText)
                    {
                        isShowWindow = false;
                        i++;
                    }
                }
            }
            else if (cmd.IndexOf("画像表示") > -1)
            {
                // 画像操作
                EventImageData data = JsonUtility.FromJson<EventImageData>(command[i].jsonCommand);
                Texture2D tex = Resources.Load("Textures/" + stage.ExtRemover(data.selectName)) as Texture2D;
                GameObject imgObj = new GameObject(data.imageName);
                imgObj.transform.parent = canvas.transform.FindChild("NobelUI");
                imgObj.transform.SetSiblingIndex(data.layerIndex);
                Image img = imgObj.AddComponent<Image>();
                img.material.mainTexture = tex;
                RectTransform recTra = imgObj.GetComponent<RectTransform>();
                recTra.pivot = Vector2.zero;
                recTra.sizeDelta = new Vector2(data.w, data.h);
                recTra.localPosition = new Vector2(data.x, data.y);
                i++;
            }
            else if (cmd.IndexOf("画像非表示") > -1)
            {
                EventOtherData data = JsonUtility.FromJson<EventOtherData>(command[i].jsonCommand);
                GameObject obj = canvas.transform.FindChild("NobelUI").FindChild(data.adminName).gameObject;
                if (obj != null)
                    Destroy(obj);
                i++;
            }
            else if (cmd.IndexOf("変数") > -1)
            {
                // 変数操作
                EventVarData data = JsonUtility.FromJson<EventVarData>(command[i].jsonCommand);
                switch (data.varMode)
                {
                    case 0:
                        // フラグ
                        if (data.selectFlgIndex < stage.FlgVarNames.Count && data.selectFlgName == stage.FlgVarNames[data.selectFlgIndex])
                        {
                            if (data.selectFlgName.IndexOf("ローカル") > -1 && data.selectFlgIndex < stage.FlgVar.Length)
                            {
                                if (data.varInputMode == 0)
                                    stage.FlgVar[data.selectFlgIndex].var = data.varSetFlg;
                                else
                                {
                                    if (data.selectSetFlgIndex < stage.FlgVarNames.Count && data.selectSetFlgName == stage.FlgVarNames[data.selectSetFlgIndex])
                                    {
                                        if (data.selectSetFlgName.IndexOf("ローカル") > -1)
                                            stage.FlgVar[data.selectFlgIndex].var = stage.FlgVar[data.selectSetFlgIndex].var;
                                        else if (data.selectSetFlgName.IndexOf("システム") > -1)
                                            stage.FlgVar[data.selectFlgIndex].var = stage.Var.var_flg[data.selectSetFlgIndex - 10].var;
                                    }
                                }
                            }
                            else if (data.selectFlgName.IndexOf("システム") > -1 && data.selectFlgIndex - 10 < stage.Var.var_flg.Count)
                            {
                                if (data.varInputMode == 0)
                                    stage.Var.var_flg[data.selectFlgIndex - 10].var = data.varSetFlg;
                                else
                                {
                                    stage.Var.var_flg[data.selectFlgIndex - 10].var = stage.FlgVar[data.selectSetFlgIndex - 10].var;
                                    if (data.selectSetFlgIndex < stage.FlgVarNames.Count && data.selectSetFlgName == stage.FlgVarNames[data.selectSetFlgIndex])
                                    {
                                        if (data.selectSetFlgName.IndexOf("ローカル") > -1)
                                            stage.Var.var_flg[data.selectFlgIndex - 10].var = stage.FlgVar[data.selectSetFlgIndex].var;
                                        else if (data.selectSetFlgName.IndexOf("システム") > -1)
                                            stage.Var.var_flg[data.selectFlgIndex - 10].var = stage.Var.var_flg[data.selectSetFlgIndex - 10].var;
                                    }
                                }
                            }
                        }
                        break;
                    case 1:
                        // 整数
                        if (data.selectIntIndex < stage.IntVarNames.Count && data.selectIntName == stage.IntVarNames[data.selectIntIndex])
                        {
                            if (data.selectIntName.IndexOf("ローカル") > -1 && data.selectIntIndex < stage.IntVar.Length)
                            {
                                // ローカル
                                if (data.varInputMode == 0)
                                {
                                    // INPUT1 手動
                                    if (data.varInputMode2 == 0)
                                    {
                                        // INPUT2 手動
                                        stage.IntVar[data.selectIntIndex].var =
                                            data.selectOprName == "=" ?
                                                (data.selectIntOprName == "+" ?
                                                (data.varSetInt + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                (data.varSetInt - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                (data.varSetInt * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                (data.varSetInt / data.varSetInt2) : 0) :
                                            data.selectOprName == "+=" ?
                                                stage.IntVar[data.selectIntIndex].var +
                                                (data.selectIntOprName == "+" ?
                                                (data.varSetInt + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                (data.varSetInt - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                (data.varSetInt * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                (data.varSetInt / data.varSetInt2) : 0) :
                                            data.selectOprName == "-=" ?
                                                stage.IntVar[data.selectIntIndex].var -
                                                (data.selectIntOprName == "+" ?
                                                (data.varSetInt + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                (data.varSetInt - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                (data.varSetInt * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                (data.varSetInt / data.varSetInt2) : 0) :
                                            data.selectOprName == "×=" ?
                                                stage.IntVar[data.selectIntIndex].var *
                                                (data.selectIntOprName == "+" ?
                                                (data.varSetInt + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                (data.varSetInt - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                (data.varSetInt * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                (data.varSetInt / data.varSetInt2) : 0) :
                                            data.selectOprName == "÷=" ?
                                                stage.IntVar[data.selectIntIndex].var /
                                                (data.selectIntOprName == "+" ?
                                                (data.varSetInt + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                (data.varSetInt - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                (data.varSetInt * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                (data.varSetInt / data.varSetInt2) : 0) : 0;
                                    }
                                    else
                                    {
                                        // INPUT2 変数選択
                                        if (data.selectSetIntIndex2 < stage.IntVarNames.Count && data.selectSetIntName2 == stage.IntVarNames[data.selectSetIntIndex2])
                                        {
                                            if (data.selectSetIntName2.IndexOf("ローカル") > -1 && data.selectSetIntIndex2 < stage.IntVar.Length)
                                            {
                                                // ローカル
                                                stage.IntVar[data.selectIntIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.IntVar[data.selectSetIntIndex2].var) : 0) : 0;
                                            }
                                            else if (data.selectSetIntName2.IndexOf("システム") > -1 && data.selectSetIntIndex2 - 10 < stage.Var.var_int.Count)
                                            {
                                                // システム
                                                stage.IntVar[data.selectIntIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) : 0;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // INPUT1 変数選択
                                    if (data.varInputMode2 == 0)
                                    {
                                        // INPUT2 手動
                                        if (data.selectSetIntIndex < stage.IntVarNames.Count && data.selectSetIntName == stage.IntVarNames[data.selectSetIntIndex])
                                        {
                                            if (data.selectSetIntName.IndexOf("ローカル") > -1 && data.selectSetIntIndex < stage.IntVar.Length)
                                            {
                                                // ローカル
                                                stage.IntVar[data.selectIntIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / data.varSetInt2) : 0) : 0;
                                            }
                                            else if (data.selectSetIntName.IndexOf("システム") > -1 && data.selectSetIntIndex - 10 < stage.Var.var_int.Count)
                                            {
                                                // システム
                                                stage.IntVar[data.selectIntIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / data.varSetInt2) : 0) : 0;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // INPUT2 変数選択
                                        if (data.selectSetIntIndex < stage.IntVarNames.Count && data.selectSetIntName == stage.IntVarNames[data.selectSetIntIndex] &&
                                            data.selectSetIntIndex2 < stage.IntVarNames.Count && data.selectSetIntName2 == stage.IntVarNames[data.selectSetIntIndex2])
                                        {
                                            if (data.selectSetIntName.IndexOf("ローカル") > -1 && data.selectSetIntName2.IndexOf("ローカル") > -1 && data.selectSetIntIndex < stage.IntVar.Length && data.selectSetIntIndex2 < stage.IntVar.Length)
                                            {
                                                // INPUT1 ローカル INPUT2 ローカル
                                                stage.IntVar[data.selectIntIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) : 0;
                                            }
                                            else if (data.selectSetIntName.IndexOf("システム") > -1 && data.selectSetIntName2.IndexOf("システム") > -1 && data.selectSetIntIndex - 10 < stage.Var.var_int.Count && data.selectSetIntIndex2 - 10 < stage.Var.var_int.Count)
                                            {
                                                // INPUT1 システム INPUT2 システム
                                                stage.IntVar[data.selectIntIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) : 0;
                                            }
                                            else if (data.selectSetIntName.IndexOf("ローカル") > -1 && data.selectSetIntName2.IndexOf("システム") > -1 && data.selectSetIntIndex < stage.IntVar.Length && data.selectSetIntIndex2 - 10 < stage.Var.var_int.Count)
                                            {
                                                // INPUT1 ローカル INPUT2 システム
                                                stage.IntVar[data.selectIntIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) : 0;
                                            }
                                            else if (data.selectSetIntName.IndexOf("システム") > -1 && data.selectSetIntName2.IndexOf("ローカル") > -1 && data.selectSetIntIndex - 10 < stage.Var.var_int.Count && data.selectSetIntIndex2 < stage.IntVar.Length)
                                            {
                                                // INPUT1 システム INPUT2 ローカル
                                                stage.IntVar[data.selectIntIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) : 0;
                                            }
                                        }
                                    }
                                }
                            }
                            else if (data.selectIntName.IndexOf("システム") > -1 && data.selectIntIndex - 10 < stage.Var.var_int.Count)
                            {
                                // システム
                                if (data.varInputMode == 0)
                                {
                                    // INPUT1 手動
                                    if (data.varInputMode2 == 0)
                                    {
                                        // INPUT2 手動
                                        stage.Var.var_int[data.selectIntIndex - 10].var =
                                            data.selectOprName == "=" ?
                                                (data.selectIntOprName == "+" ?
                                                (data.varSetInt + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                (data.varSetInt - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                (data.varSetInt * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                (data.varSetInt / data.varSetInt2) : 0) :
                                            data.selectOprName == "+=" ?
                                                stage.IntVar[data.selectIntIndex].var +
                                                (data.selectIntOprName == "+" ?
                                                (data.varSetInt + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                (data.varSetInt - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                (data.varSetInt * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                (data.varSetInt / data.varSetInt2) : 0) :
                                            data.selectOprName == "-=" ?
                                                stage.IntVar[data.selectIntIndex].var -
                                                (data.selectIntOprName == "+" ?
                                                (data.varSetInt + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                (data.varSetInt - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                (data.varSetInt * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                (data.varSetInt / data.varSetInt2) : 0) :
                                            data.selectOprName == "×=" ?
                                                stage.IntVar[data.selectIntIndex].var *
                                                (data.selectIntOprName == "+" ?
                                                (data.varSetInt + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                (data.varSetInt - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                (data.varSetInt * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                (data.varSetInt / data.varSetInt2) : 0) :
                                            data.selectOprName == "÷=" ?
                                                stage.IntVar[data.selectIntIndex].var /
                                                (data.selectIntOprName == "+" ?
                                                (data.varSetInt + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                (data.varSetInt - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                (data.varSetInt * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                (data.varSetInt / data.varSetInt2) : 0) : 0;
                                    }
                                    else
                                    {
                                        // INPUT2 変数選択
                                        if (data.selectSetIntIndex2 < stage.IntVarNames.Count && data.selectSetIntName2 == stage.IntVarNames[data.selectSetIntIndex2])
                                        {
                                            if (data.selectSetIntName2.IndexOf("ローカル") > -1 && data.selectSetIntIndex2 < stage.IntVar.Length)
                                            {
                                                // ローカル
                                                stage.Var.var_int[data.selectIntIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.IntVar[data.selectSetIntIndex2].var) : 0) : 0;
                                            }
                                            else if (data.selectSetIntName2.IndexOf("システム") > -1 && data.selectSetIntIndex2 - 10 < stage.Var.var_int.Count)
                                            {
                                                // システム
                                                stage.Var.var_int[data.selectIntIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (data.varSetInt + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (data.varSetInt - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (data.varSetInt * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (data.varSetInt / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) : 0;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // INPUT1 変数選択
                                    if (data.varInputMode2 == 0)
                                    {
                                        // INPUT2 手動
                                        if (data.selectSetIntIndex < stage.IntVarNames.Count && data.selectSetIntName == stage.IntVarNames[data.selectSetIntIndex])
                                        {
                                            if (data.selectSetIntName.IndexOf("ローカル") > -1 && data.selectSetIntIndex < stage.IntVar.Length)
                                            {
                                                // ローカル
                                                stage.Var.var_int[data.selectIntIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / data.varSetInt2) : 0) : 0;
                                            }
                                            else if (data.selectSetIntName.IndexOf("システム") > -1 && data.selectSetIntIndex - 10 < stage.Var.var_int.Count)
                                            {
                                                // システム
                                                stage.Var.var_int[data.selectIntIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / data.varSetInt2) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + data.varSetInt2) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - data.varSetInt2) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * data.varSetInt2) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / data.varSetInt2) : 0) : 0;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // INPUT2 変数選択
                                        if (data.selectSetIntIndex < stage.IntVarNames.Count && data.selectSetIntName == stage.IntVarNames[data.selectSetIntIndex] &&
                                            data.selectSetIntIndex2 < stage.IntVarNames.Count && data.selectSetIntName2 == stage.IntVarNames[data.selectSetIntIndex2])
                                        {
                                            if (data.selectSetIntName.IndexOf("ローカル") > -1 && data.selectSetIntName2.IndexOf("ローカル") > -1 && data.selectSetIntIndex < stage.IntVar.Length && data.selectSetIntIndex2 < stage.IntVar.Length)
                                            {
                                                // INPUT1 ローカル INPUT2 ローカル
                                                stage.Var.var_int[data.selectIntIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) : 0;
                                            }
                                            else if (data.selectSetIntName.IndexOf("システム") > -1 && data.selectSetIntName2.IndexOf("システム") > -1 && data.selectSetIntIndex - 10 < stage.Var.var_int.Count && data.selectSetIntIndex2 - 10 < stage.Var.var_int.Count)
                                            {
                                                // INPUT1 システム INPUT2 システム
                                                stage.Var.var_int[data.selectIntIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) : 0;
                                            }
                                            else if (data.selectSetIntName.IndexOf("ローカル") > -1 && data.selectSetIntName2.IndexOf("システム") > -1 && data.selectSetIntIndex < stage.IntVar.Length && data.selectSetIntIndex2 - 10 < stage.Var.var_int.Count)
                                            {
                                                // INPUT1 ローカル INPUT2 システム
                                                stage.Var.var_int[data.selectIntIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var + stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "-" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var - stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "×" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var * stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : data.selectIntOprName == "÷" ?
                                                        (stage.IntVar[data.selectSetIntIndex].var / stage.Var.var_int[data.selectSetIntIndex2 - 10].var) : 0) : 0;
                                            }
                                            else if (data.selectSetIntName.IndexOf("システム") > -1 && data.selectSetIntName2.IndexOf("ローカル") > -1 && data.selectSetIntIndex - 10 < stage.Var.var_int.Count && data.selectSetIntIndex2 < stage.IntVar.Length)
                                            {
                                                // INPUT1 システム INPUT2 ローカル
                                                stage.Var.var_int[data.selectIntIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "+=" ?
                                                        stage.IntVar[data.selectIntIndex].var +
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "-=" ?
                                                        stage.IntVar[data.selectIntIndex].var -
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "×=" ?
                                                        stage.IntVar[data.selectIntIndex].var *
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) :
                                                    data.selectOprName == "÷=" ?
                                                        stage.IntVar[data.selectIntIndex].var /
                                                        (data.selectIntOprName == "+" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var + stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "-" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var - stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "×" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var * stage.IntVar[data.selectSetIntIndex2].var) : data.selectIntOprName == "÷" ?
                                                        (stage.Var.var_int[data.selectSetIntIndex - 10].var / stage.IntVar[data.selectSetIntIndex2].var) : 0) : 0;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case 2:
                        // 文字列
                        if (data.selectStrIndex < stage.StrVarNames.Count && data.selectStrName == stage.StrVarNames[data.selectStrIndex])
                        {
                            if (data.selectStrName.IndexOf("ローカル") > -1 && data.selectStrIndex < stage.StrVar.Length)
                            {
                                // ローカル
                                if (data.varInputMode == 0)
                                {
                                    // INPUT1 手動
                                    if (data.varInputMode2 == 0)
                                    {
                                        // INPUT2 手動
                                        stage.StrVar[data.selectStrIndex].var =
                                            data.selectOprName == "=" ?
                                                (data.varSetStr + data.varSetStr2) :
                                            data.selectOprName == "+=" ?
                                                stage.StrVar[data.selectStrIndex].var +
                                                (data.varSetStr + data.varSetStr2) : "";
                                    }
                                    else
                                    {
                                        // INPUT2 変数選択
                                        if (data.selectSetStrIndex2 < stage.StrVarNames.Count && data.selectSetStrName2 == stage.StrVarNames[data.selectSetStrIndex2])
                                        {
                                            if (data.selectSetStrName2.IndexOf("ローカル") > -1 && data.selectSetStrIndex2 < stage.StrVar.Length)
                                            {
                                                // ローカル
                                                stage.StrVar[data.selectStrIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (data.varSetStr + stage.StrVar[data.selectSetStrIndex2].var) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (data.varSetStr + stage.StrVar[data.selectSetStrIndex2].var) : "";
                                            }
                                            else if (data.selectSetStrName2.IndexOf("システム") > -1 && data.selectSetStrIndex2 - 10 < stage.Var.var_str.Count)
                                            {
                                                // システム
                                                stage.StrVar[data.selectStrIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (data.varSetStr + stage.Var.var_str[data.selectSetStrIndex2 - 10].var) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (data.varSetStr + stage.Var.var_str[data.selectSetStrIndex2 - 10].var) : "";
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // INPUT1 変数選択
                                    if (data.varInputMode2 == 0)
                                    {
                                        // INPUT2 手動
                                        if (data.selectSetStrIndex < stage.StrVarNames.Count && data.selectSetStrName == stage.StrVarNames[data.selectSetStrIndex])
                                        {
                                            if (data.selectSetStrName.IndexOf("ローカル") > -1 && data.selectSetStrIndex < stage.StrVar.Length)
                                            {
                                                // ローカル
                                                stage.StrVar[data.selectStrIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (stage.StrVar[data.selectSetStrIndex].var + data.varSetStr2) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (stage.StrVar[data.selectSetStrIndex].var + data.varSetStr2) : "";
                                            }
                                            else if (data.selectSetStrName.IndexOf("システム") > -1 && data.selectSetStrIndex - 10 < stage.Var.var_str.Count)
                                            {
                                                // システム
                                                stage.StrVar[data.selectStrIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (stage.Var.var_str[data.selectSetStrIndex - 10].var + data.varSetStr2) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (stage.Var.var_str[data.selectSetStrIndex - 10].var + data.varSetStr2) : "";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // INPUT2 変数選択
                                        if (data.selectSetStrIndex < stage.StrVarNames.Count && data.selectSetStrName == stage.StrVarNames[data.selectSetStrIndex] &&
                                            data.selectSetStrIndex2 < stage.StrVarNames.Count && data.selectSetStrName2 == stage.StrVarNames[data.selectSetStrIndex2])
                                        {
                                            if (data.selectSetStrName.IndexOf("ローカル") > -1 && data.selectSetStrName2.IndexOf("ローカル") > -1 && data.selectSetStrIndex < stage.StrVar.Length && data.selectSetStrIndex2 < stage.StrVar.Length)
                                            {
                                                // INPUT1 ローカル INPUT2 ローカル
                                                stage.StrVar[data.selectStrIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (stage.StrVar[data.selectSetStrIndex].var + stage.StrVar[data.selectSetStrIndex2].var) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (stage.StrVar[data.selectSetStrIndex].var + stage.StrVar[data.selectSetStrIndex2].var) : "";
                                            }
                                            else if (data.selectSetStrName.IndexOf("システム") > -1 && data.selectSetStrName2.IndexOf("システム") > -1 && data.selectSetStrIndex - 10 < stage.Var.var_str.Count && data.selectSetStrIndex2 - 10 < stage.Var.var_str.Count)
                                            {
                                                // INPUT1 システム INPUT2 システム
                                                stage.StrVar[data.selectStrIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (stage.Var.var_str[data.selectSetStrIndex - 10].var + stage.Var.var_str[data.selectSetStrIndex2 - 10].var) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (stage.Var.var_str[data.selectSetStrIndex - 10].var + stage.Var.var_str[data.selectSetStrIndex2 - 10].var) : "";
                                            }
                                            else if (data.selectSetStrName.IndexOf("ローカル") > -1 && data.selectSetStrName2.IndexOf("システム") > -1 && data.selectSetStrIndex < stage.StrVar.Length && data.selectSetStrIndex2 - 10 < stage.Var.var_str.Count)
                                            {
                                                // INPUT1 ローカル INPUT2 システム
                                                stage.StrVar[data.selectStrIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (stage.StrVar[data.selectSetStrIndex].var + stage.Var.var_str[data.selectSetStrIndex2 - 10].var) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (stage.StrVar[data.selectSetStrIndex].var + stage.Var.var_str[data.selectSetStrIndex2 - 10].var) : "";
                                            }
                                            else if (data.selectSetStrName.IndexOf("システム") > -1 && data.selectSetStrName2.IndexOf("ローカル") > -1 && data.selectSetStrIndex - 10 < stage.Var.var_str.Count && data.selectSetStrIndex2 < stage.StrVar.Length)
                                            {
                                                // INPUT1 システム INPUT2 ローカル
                                                stage.StrVar[data.selectStrIndex].var =
                                                    data.selectOprName == "=" ?
                                                        (stage.Var.var_str[data.selectSetStrIndex - 10].var + stage.StrVar[data.selectSetStrIndex2].var) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (stage.Var.var_str[data.selectSetStrIndex - 10].var + stage.StrVar[data.selectSetStrIndex2].var) : "";
                                            }
                                        }
                                    }
                                }
                            }
                            else if (data.selectStrName.IndexOf("システム") > -1 && data.selectStrIndex - 10 < stage.Var.var_str.Count)
                            {
                                // システム
                                if (data.varInputMode == 0)
                                {
                                    // INPUT1 手動
                                    if (data.varInputMode2 == 0)
                                    {
                                        // INPUT2 手動
                                        stage.Var.var_str[data.selectStrIndex - 10].var =
                                            data.selectOprName == "=" ?
                                                (data.varSetStr + data.varSetStr2) :
                                            data.selectOprName == "+=" ?
                                                stage.StrVar[data.selectStrIndex].var +
                                                (data.varSetStr + data.varSetStr2) : "";
                                    }
                                    else
                                    {
                                        // INPUT2 変数選択
                                        if (data.selectSetStrIndex2 < stage.StrVarNames.Count && data.selectSetStrName2 == stage.StrVarNames[data.selectSetStrIndex2])
                                        {
                                            if (data.selectSetStrName2.IndexOf("ローカル") > -1 && data.selectSetStrIndex2 < stage.StrVar.Length)
                                            {
                                                // ローカル
                                                stage.Var.var_str[data.selectStrIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (data.varSetStr + stage.StrVar[data.selectSetStrIndex2].var) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (data.varSetStr + stage.StrVar[data.selectSetStrIndex2].var) : "";
                                            }
                                            else if (data.selectSetStrName2.IndexOf("システム") > -1 && data.selectSetStrIndex2 - 10 < stage.Var.var_str.Count)
                                            {
                                                // システム
                                                stage.Var.var_str[data.selectStrIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (data.varSetStr + stage.Var.var_str[data.selectSetStrIndex2 - 10].var) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (data.varSetStr + stage.Var.var_str[data.selectSetStrIndex2 - 10].var) : "";
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // INPUT1 変数選択
                                    if (data.varInputMode2 == 0)
                                    {
                                        // INPUT2 手動
                                        if (data.selectSetStrIndex < stage.StrVarNames.Count && data.selectSetStrName == stage.StrVarNames[data.selectSetStrIndex])
                                        {
                                            if (data.selectSetStrName.IndexOf("ローカル") > -1 && data.selectSetStrIndex < stage.StrVar.Length)
                                            {
                                                // ローカル
                                                stage.Var.var_str[data.selectStrIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (stage.StrVar[data.selectSetStrIndex].var + data.varSetStr2) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (stage.StrVar[data.selectSetStrIndex].var + data.varSetStr2) : "";
                                            }
                                            else if (data.selectSetStrName.IndexOf("システム") > -1 && data.selectSetStrIndex - 10 < stage.Var.var_str.Count)
                                            {
                                                // システム
                                                stage.Var.var_str[data.selectStrIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (stage.Var.var_str[data.selectSetStrIndex - 10].var + data.varSetStr2) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (stage.Var.var_str[data.selectSetStrIndex - 10].var + data.varSetStr2) : "";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // INPUT2 変数選択
                                        if (data.selectSetStrIndex < stage.StrVarNames.Count && data.selectSetStrName == stage.StrVarNames[data.selectSetStrIndex] &&
                                            data.selectSetStrIndex2 < stage.StrVarNames.Count && data.selectSetStrName2 == stage.StrVarNames[data.selectSetStrIndex2])
                                        {
                                            if (data.selectSetStrName.IndexOf("ローカル") > -1 && data.selectSetStrName2.IndexOf("ローカル") > -1 && data.selectSetStrIndex < stage.StrVar.Length && data.selectSetStrIndex2 < stage.StrVar.Length)
                                            {
                                                // INPUT1 ローカル INPUT2 ローカル
                                                stage.Var.var_str[data.selectStrIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (stage.StrVar[data.selectSetStrIndex].var + stage.StrVar[data.selectSetStrIndex2].var) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (stage.StrVar[data.selectSetStrIndex].var + stage.StrVar[data.selectSetStrIndex2].var) : "";
                                            }
                                            else if (data.selectSetStrName.IndexOf("システム") > -1 && data.selectSetStrName2.IndexOf("システム") > -1 && data.selectSetStrIndex - 10 < stage.Var.var_str.Count && data.selectSetStrIndex2 - 10 < stage.Var.var_str.Count)
                                            {
                                                // INPUT1 システム INPUT2 システム
                                                stage.Var.var_str[data.selectStrIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (stage.Var.var_str[data.selectSetStrIndex - 10].var + stage.Var.var_str[data.selectSetStrIndex2 - 10].var) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (stage.Var.var_str[data.selectSetStrIndex - 10].var + stage.Var.var_str[data.selectSetStrIndex2 - 10].var) : "";
                                            }
                                            else if (data.selectSetStrName.IndexOf("ローカル") > -1 && data.selectSetStrName2.IndexOf("システム") > -1 && data.selectSetStrIndex < stage.StrVar.Length && data.selectSetStrIndex2 - 10 < stage.Var.var_str.Count)
                                            {
                                                // INPUT1 ローカル INPUT2 システム
                                                stage.Var.var_str[data.selectStrIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (stage.StrVar[data.selectSetStrIndex].var + stage.Var.var_str[data.selectSetStrIndex2 - 10].var) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (stage.StrVar[data.selectSetStrIndex].var + stage.Var.var_str[data.selectSetStrIndex2 - 10].var) : "";
                                            }
                                            else if (data.selectSetStrName.IndexOf("システム") > -1 && data.selectSetStrName2.IndexOf("ローカル") > -1 && data.selectSetStrIndex - 10 < stage.Var.var_str.Count && data.selectSetStrIndex2 < stage.StrVar.Length)
                                            {
                                                // INPUT1 システム INPUT2 ローカル
                                                stage.Var.var_str[data.selectStrIndex - 10].var =
                                                    data.selectOprName == "=" ?
                                                        (stage.Var.var_str[data.selectSetStrIndex - 10].var + stage.StrVar[data.selectSetStrIndex2].var) :
                                                    data.selectOprName == "+=" ?
                                                        stage.StrVar[data.selectStrIndex].var +
                                                        (stage.Var.var_str[data.selectSetStrIndex - 10].var + stage.StrVar[data.selectSetStrIndex2].var) : "";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
                i++;
            }
            else if (cmd.IndexOf("移動") > -1)
            {
                // 場所移動
                EventMoveData data = JsonUtility.FromJson<EventMoveData>(command[i].jsonCommand);

                Vector2 pos = data.movePos;

                stage.StageInit(data.selectMoveStageName);
                if (pos.x != -1 && pos.y != -1)
                    stage.GetPlayer.transform.position = new Vector3(pos.x * stage.chipSizeX, stage.Data.map.mapSizeY * stage.chipSizeY - pos.y * stage.chipSizeY - stage.chipSizeY);
                moveFlag = true;
                stage.GetPlayer.GetComponent<PlayerController>().SetStart();
                stage.GetPlayer.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                i++;
            }
            else if (cmd.IndexOf("条件") > -1)
            {
                // 条件文
                ifCounter++;
                ifData.Add(new List<EventIfData>());
                cmdSkipFlg.Add(false);
                cmdSkipCheckFlg.Add(false);
                i++;
            }
            else if (cmd.IndexOf("分岐") > -1)
            {
                if (cmd.IndexOf("分岐終了") > -1)
                {
                    cmdSkipFlg.RemoveAt(ifCounter - 1);
                    cmdSkipCheckFlg.RemoveAt(ifCounter - 1);
                    ifData.RemoveAt(ifCounter - 1);
                    ifCounter--;
                }
                else
                {
                    ifData[ifCounter - 1].Add(JsonUtility.FromJson<EventIfData>(command[i].jsonCommand));
                    if (cmdSkipCheckFlg[ifCounter - 1])
                    {
                        cmdSkipCheckFlg[ifCounter - 1] = false;
                        cmdSkipFlg[ifCounter - 1] = true;
                    }
                }
                i++;
            }
            else if (cmd.IndexOf("サウンド再生") > -1)
            {
                // サウンド再生
                EventSoundData data = JsonUtility.FromJson<EventSoundData>(command[i].jsonCommand);
                string name = data.soundName;
                bool loopFlag = data.soundLoopFlag;
                string soundPath = data.soundPath;
                float startTime = data.soundStartTime;
                float endTime = data.soundEndTime;
                float pitch = data.soundPitch;
                float volume = data.soundVolume;
                AudioClip clip = Resources.Load("Sounds/" + ((System.Func<string>)(() => { string str = ""; for (int m = 0; m < soundPath.Split('.').Length; m++) { if (soundPath.Split('.').Length - 1 > m) str += "." + soundPath.Split('.')[m]; } return str; }))().Substring(1, ((System.Func<string>)(() => { string str = ""; for (int m = 0; m < soundPath.Split('.').Length; m++) { if (soundPath.Split('.').Length - 1 > m) str += "." + soundPath.Split('.')[m]; } return str; }))().Length - 1)) as AudioClip;
                audioSources[name] = gameObject.GetComponent<AudioSource>() == null ? gameObject.AddComponent<AudioSource>() : gameObject.GetComponent<AudioSource>();
                audioSources[name].loop = true;
                audioSources[name].volume = volume;
                audioSources[name].pitch = pitch;
                audioSources[name].clip = clip;
                audioSources[name].time = startTime;
                audioSources[name].Play();
                i++;
            }
            else if (cmd.IndexOf("エフェクト") > -1)
            {
                // エフェクト
            }
            else if (cmd.IndexOf("サウンド停止") > -1)
            {
                // サウンド停止
                EventOtherData data = JsonUtility.FromJson<EventOtherData>(command[i].jsonCommand);
                string name = data.adminName;

                if (name == "")
                {
                    if (audioSources != null)
                    {
                        foreach (KeyValuePair<string, AudioSource> source in audioSources)
                        {
                            if (source.Value != null && source.Value.isPlaying)
                                source.Value.Stop();
                        }
                    }
                }
                else
                {
                    if (audioSources != null)
                    {
                        if (audioSources[name] != null && audioSources[name].isPlaying)
                            audioSources[name].Stop();
                    }
                }
                i++;
            }
            else
            {
                messageBox.SetActive(false);
                textController.uiText.text = "";
                i++;
            }

            counter = i;
        }
    }

    void FixedUpdate()
    {
        // 決定キー
        if (mode == 2)
        {
            Vector2 pos = transform.position;

            if (Physics2D.OverlapArea(new Vector2(pos.x - eventData.rect.width / 2 * stage.chipSizeX - 0.05f, pos.y - eventData.rect.height / 2 * stage.chipSizeY + 0.05f), new Vector2(pos.x - eventData.rect.width / 2 * stage.chipSizeX + eventData.rect.width * stage.chipSizeX + 0.05f, pos.y - eventData.rect.height / 2 * stage.chipSizeY - eventData.rect.height * stage.chipSizeY - 0.05f), playerLayer))
            {
                if (Input.GetButtonDown("Submit"))
                    command = eventData._event.command;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        // プレイヤー接触
        if (mode == 1)
        {
            if (collider.gameObject.tag == "Player")
                command = eventData._event.command;
        }
    }

    public bool EventComplete
    {
        get { return eventComplete; }
    }
}
