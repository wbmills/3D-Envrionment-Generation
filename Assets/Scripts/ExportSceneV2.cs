using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExportSceneV2 : MonoBehaviour
{
    public MapGeneration mgScript;
    public GameObject[] allObjects;
    // Start is called before the first frame update
    void Start()
    {
        mgScript = GameObject.Find("EditModeController").GetComponent<MapGeneration>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CollectObjects()
    {
        allObjects = mgScript.GetAllObjects();
    }

    public void SaveToScriptableObject()
    {

    }
}
