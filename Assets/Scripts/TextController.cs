using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TextController : MonoBehaviour 
{
	public string[] scenarios;
	[SerializeField]
    public Text uiText;

	[SerializeField][Range(0.001f, 0.3f)]
	public float intervalForCharacterDisplay = 0.05f;

	private string currentText = string.Empty;
	private float timeUntilDisplay = 0;
	private float timeElapsed = 1;
	private int currentLine = 0;
	private int lastUpdateCharacter = -1;
    private bool isCompleteAllText = false;

	void Start()
	{
		SetNextLine();
	}

	void Update () 
	{
		// 文字の表示が完了してるならクリック時に次の行を表示する
		if (IsCompleteDisplayText)
        {
			if(currentLine < scenarios.Length && Input.GetMouseButtonDown(0))
				SetNextLine();
            else if (currentLine >= scenarios.Length && Input.GetMouseButtonDown(0))
                isCompleteAllText = true;
        }
        else
        {
		// 完了してないなら文字をすべて表示する
			if(Input.GetMouseButtonDown(0))
            {
				timeUntilDisplay = 0;
			}
		}

		int displayCharacterCount = (int)(Mathf.Clamp01((Time.time - timeElapsed) / timeUntilDisplay) * currentText.Length);
		if(displayCharacterCount != lastUpdateCharacter)
        {
			uiText.text = currentText.Substring(0, displayCharacterCount);
			lastUpdateCharacter = displayCharacterCount;
		}
	}

	void SetNextLine()
	{
		currentText = scenarios[currentLine];
		timeUntilDisplay = currentText.Length * intervalForCharacterDisplay;
		timeElapsed = Time.time;
		currentLine ++;
		lastUpdateCharacter = -1;
	}

    // 文字の表示が完了しているかどうか
    public bool IsCompleteDisplayText
    {
        get { return Time.time > timeElapsed + timeUntilDisplay; }
    }

    // 全ての文字を表示し終わったかどうか
    public bool IsCompleteAllText
    {
        get { return isCompleteAllText; }
    }
}
