using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public class EventManager : MonoBehaviour
{
    public int eventID;

    private StageManager stage;
    private string stageName;
    private string[] command;
    private int counter;
    private GameObject canvas;
    private TextController textController;
    private Text uiText;
    private bool isShowWindow;

    void Start()
    {
        stage = GameObject.Find("StageManager").GetComponent<StageManager>();
        stageName = stage.getNowStageName;
        canvas = GameObject.Find("SystemManager").GetComponent<MessageWindowManager>().getCanvas;
        uiText = GameObject.Find("SystemManager").GetComponent<MessageWindowManager>().getText;
        init();
    }

    private void init()
    {
        counter = 0;
    }

    void Update()
    {
        if (command != null && command.Length > 0)
        {
            if (counter >= command.Length)
            {
                isShowWindow = false;
                canvas.SetActive(false);
                Destroy(GameObject.Find("TextManager"));
                command = null;
                uiText.text = "";
                init();
                return;
            }

            Regex reg = new Regex(@"\[(?<value>.*?)\]");
            int i = counter;
            string cmd = reg.Match(command[i]).Groups["value"].Value;

            if (cmd.IndexOf("Message") > -1)
            {
                string[] messages = command[i].Replace("\\n", "\n").Replace("[" + cmd + "]", "").Split(new string[] { "[p]" }, System.StringSplitOptions.None);

                if (!isShowWindow)
                {
                    isShowWindow = true;
                    canvas.SetActive(true);
                    Destroy(GameObject.Find("TextManager"));
                    GameObject textManager = new GameObject("TextManager");
                    textManager.AddComponent<TextController>();
                    textController = textManager.GetComponent<TextController>();
                    textController.scenarios = messages;
                    textController.uiText = uiText;

                    if (textController.IsCompleteAllText)
                        i++;
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
            else
            {
                canvas.SetActive(false);
                i++;
            }

            counter = i;
        }
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Player")
        {
            MapEventData data = stage.OpenEventFile(stageName);

            if (!(data.eventFold.Length >= eventID))
            {
                Debug.Log("EventIDが正しくありません");
                return;
            }

            command = data.eventFold[eventID].command.ToArray();
        }
    }
}
