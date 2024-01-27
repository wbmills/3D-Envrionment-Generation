using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class checkpointManager : MonoBehaviour
{
    private GameObject[] checkpoints;
    private Dictionary<GameObject, bool> checkpointDict;

    void Start()
    {
        checkpoints = GameObject.FindGameObjectsWithTag("Checkpoint");
        checkpointDict = new Dictionary<GameObject, bool>();
        foreach(GameObject g in checkpoints)
        {
            checkpointDict.Add(g, false);
        }
    }

    private void Update()
    {
        foreach(GameObject g in checkpoints)
        {
            if (g.GetComponent<checkpoint>().state != "active")
            {
                checkpointDict[g] = true;
            }
        }
    }

    public void SetCheckpointStatus(GameObject checkpoint, bool status)
    {
        if (checkpointDict.ContainsKey(checkpoint))
        {
            checkpointDict[checkpoint] = status;
        }
    }

    public bool GetCheckpointStatus(GameObject g)
    {
        if (checkpointDict.ContainsKey(g)){
            return checkpointDict[g];
        }
        else
        {
            return false;
        }
    }

    public GameObject[] GetCheckpoints()
    {
        return checkpoints;
    }
}
