using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MessageWindowManager : MonoBehaviour
{
    public Text uiText;

    private GameObject canvas;

	void Start ()
    {
        canvas = GameObject.Find("Canvas");
        canvas.SetActive(false);
	}
	
	void Update ()
    {
	    
	}

    public GameObject getCanvas
    {
        get { return canvas; }
    }

    public Text getText
    {
        get { return uiText; }
    }
}
