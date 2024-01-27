using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class tutorial : MonoBehaviour
{
    private List<KeyValuePair<string, KeyCode>> instructions;
    private float initTime;
    private float timeElapsed;
    private bool timeUp;
    private bool playTutorial;
    private bool tutorialComplete;
    private int i;

    public TextMeshProUGUI continueInstruction;
    public checkpointManager checkpointManager;
    public float maxDuration = 180; // maximum time for tutorial in seconds
    public TextMeshProUGUI text;
    // Start is called before the first frame update
    void Start()
    {
        checkpointManager = GameObject.Find("Checkpoint Manager").GetComponent<checkpointManager>();
        tutorialComplete = false;
        initTime = Time.time;
        instructions = new List<KeyValuePair<string, KeyCode>>()
        {
            new KeyValuePair<string, KeyCode>("Welcome! Press Enter to start the tutorial", KeyCode.Return),
            new KeyValuePair<string, KeyCode>("Press W to move forward", KeyCode.W),
            new KeyValuePair<string, KeyCode>("Press A to move left", KeyCode.A),
            new KeyValuePair<string, KeyCode>("Press S to move backwards", KeyCode.S),
            new KeyValuePair<string, KeyCode>("Press D to move right", KeyCode.D),
            new KeyValuePair<string, KeyCode>("Press Space to jump", KeyCode.Space),
            new KeyValuePair<string, KeyCode>("Now lets move on to the gameplay!", KeyCode.Return)
        };

        timeUp = false;
        StartCoroutine("Timer");
        i = 0;
        playTutorial = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (playTutorial && !tutorialComplete)
        {
            ControlInstructions();
        }
        else if (playTutorial && tutorialComplete)
        {
            GameplayInstructions();
        }
        else
        {
            SceneManager.LoadScene("Map Editor", LoadSceneMode.Single);
        }
    }

    public void LoadTutorial()
    {
        SceneManager.LoadScene("Tutorial", LoadSceneMode.Single);
    }

    private void GameplayInstructions()
    {
        continueInstruction.text = "Complete the task ->";
        text.text = "Go to the checkpoint in front of you.";
        foreach(GameObject checkpoint in checkpointManager.GetCheckpoints())
        {
            if (checkpointManager.GetCheckpointStatus(checkpoint))
            {
                text.text = "Tutorial Complete!";
                continueInstruction.text = "Press Enter ->";
                playTutorial = false;
                return;
            }
        }
        
    }

    private void ControlInstructions()
    {

        if (i == instructions.Count)
        {
            text.text = "tutorial complete! :)";
            tutorialComplete = true;
            return;
        }

        text.text = instructions[i].Key;
        continueInstruction.text = $"Press {instructions[i].Value.ToString()} ->";

        if (Input.GetKeyDown(instructions[i].Value) && !timeUp)
        {
            i += 1;
        }
        else if (timeUp)
        {
            text.text = "The maximum time taken for the tutorial has elapsed. Goodbye <3";
            playTutorial = false;
        }
    }

    private IEnumerable Timer()
    {
        timeElapsed = Time.time - initTime;
        if (timeElapsed > maxDuration)
        {
            timeUp = true;
            
        }
        else
        {
            timeUp = false;
        }

        yield return timeUp;
    }
}
