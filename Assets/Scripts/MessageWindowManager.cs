using UnityEngine;
using UnityEngine.UI;

public class MessageWindowManager : MonoBehaviour
{
    private GameObject canvas;
    private GameObject messageBox;

    public Text uiText1;
    public Text nameText1;
    public Text uiText2;
    public Text nameText2;

    void Start ()
    {
        canvas = GameObject.Find("Canvas");
        messageBox = canvas.transform.FindChild("NobelUI").FindChild("MessageBox").gameObject;
        messageBox.transform.SetSiblingIndex(10000);
        nameText1.transform.parent.gameObject.SetActive(false);
        nameText2.transform.parent.gameObject.SetActive(false);
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

    public Text getText1
    {
        get { return uiText1; }
    }

    public Text getNameText1
    {
        get { return nameText1; }
    }

    public Text getText2
    {
        get { return uiText2; }
    }

    public Text getNameText2
    {
        get { return nameText2; }
    }
}
