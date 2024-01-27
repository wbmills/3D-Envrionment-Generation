using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class checkpoint : MonoBehaviour
{
    public Color startColor;
    public Color endColor;
    public string state;
    public float duration = 2f; // Change duration in seconds
    public GameObject ob;

    private List<string> states;
    private float elapsedTime = 0f;
    private Renderer checkpointRenderer;
    private float clampedTime;
    private bool colourChangeDirection = false;
    private Color tempColour;

    // Start is called before the first frame update
    void Start()
    {
        if (ob == null)
        {
            ob = gameObject;
        }
        states = new List<string> { "active", "neutral", "deactivated" };
        state = states[0];
        checkpointRenderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (state == "active")
        {
            gameObject.SetActive(true);
            transform.position = ob.transform.position;
            Bounds tempSize = ob.GetComponent<Renderer>().bounds;
            checkpointRenderer.bounds = tempSize;
            ChangeColour();
        }
        else if (state == "neutral")
        {
            tempColour = Color.white;
            tempColour.a = 0.2f;
            checkpointRenderer.material.color = tempColour;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && state == "active")
        {
            state = "neutral";
        }
    }

    private void ChangeColour()
    {
        clampedTime = Mathf.Clamp01(elapsedTime / duration);
        if (elapsedTime < duration && colourChangeDirection)
        {
            elapsedTime += Time.deltaTime;
            checkpointRenderer.material.color = Color.Lerp(startColor, endColor, clampedTime);
        }
        else if (elapsedTime < duration && !colourChangeDirection)
        {
            elapsedTime += Time.deltaTime;
            checkpointRenderer.material.color = Color.Lerp(endColor, startColor, clampedTime);
        }
        else
        {
            colourChangeDirection = !colourChangeDirection;
            elapsedTime = 0;
        }
    }
}
