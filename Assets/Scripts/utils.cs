using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class utils : MonoBehaviour
{

    void Start() {

    }

    public void quitGame()
    {
        print("Quitting...");
        Application.Quit();
    }

    public void killMap()
    {
        GameObject objectParent = GameObject.FindGameObjectWithTag("Object Parent");

        foreach(Transform t in objectParent.transform)
        {
            Destroy(t.gameObject);
        }

        GameObject[] activeTerrains = GameObject.FindGameObjectsWithTag("Terrain");
        foreach (GameObject terrain in activeTerrains)
        {
            Destroy(terrain);
        }
    }

    private void OnApplicationQuit()
    {
        killMap();
    }
}
