using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OnScreenKeyPress : MonoBehaviour
{
    public KeyCode buttonKey;
    private Button button;
    private Image buttonImage;
    private Color initColour;
    public bool onPressHide;
    // Start is called before the first frame update
    void Start()
    {
        button = gameObject.GetComponent<Button>();
        buttonImage = button.GetComponent<Image>();
        initColour = buttonImage.color;
        onPressHide = false;
        print(buttonKey);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(buttonKey))
        {
            buttonImage.color = Color.green;
        }
        else if (Input.GetKeyUp(buttonKey) && onPressHide)
        {
            buttonImage.color = initColour;
            StartCoroutine("WaitAndHide");
        }
        else if (Input.GetKeyUp(buttonKey))
        {
            buttonImage.color = initColour;
        }
    }

    private IEnumerator WaitAndHide()
    {
        gameObject.SetActive(false);
        yield return new WaitForSeconds(1);
    }
}
