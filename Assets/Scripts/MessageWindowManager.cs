using UnityEngine;
using UnityEngine.UI;

public class MessageWindowManager : MonoBehaviour
{
    private GameObject canvas;
    private GameObject messageBox;

    public Text uiText;
    public Text nameText;

	void Start ()
    {
        canvas = GameObject.Find("Canvas");
        messageBox = canvas.transform.FindChild("NobelUI").FindChild("MessageBox").gameObject;
        messageBox.transform.SetSiblingIndex(10000);
        nameText.transform.parent.gameObject.SetActive(false);
        messageBox.SetActive(false);
	}

    public GameObject getCanvas
    {
        get { return canvas; }
    }

    public GameObject getMessageBox
    {
        get { return messageBox; }
    }

    public Text getText
    {
        get { return uiText; }
    }

    public Text getNameText
    {
        get { return nameText; }
    }
}
