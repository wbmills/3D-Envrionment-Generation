using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraController : MonoBehaviour
{
    public Camera[] camArr;
    private Dictionary<string, Camera> camDict;
    public GameObject currentCamera;
    void Start()
    {
        currentCamera = null;
        camDict = new Dictionary<string, Camera>();
        camArr = GameObject.FindObjectsOfType<Camera>();
        foreach (Camera cam in camArr)
        {
            GameObject camParent = cam.gameObject;
            camDict.Add(camParent.name, cam);
            cam.gameObject.SetActive(false);
        }

        setCamera("MenuCamera");
    }

    public void setCamera(string camParentName)
    {
        if (currentCamera != null)
        {
            currentCamera.SetActive(false);
        }
        currentCamera = camDict[camParentName].gameObject;
        currentCamera.SetActive(true);
    }

    public string getCurrentCamera()
    {
        if (currentCamera)
        {
            return currentCamera.name;
        }
        else
        {
            return "None";
        }
        
    }
    private void setMouseLocked()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void setMouseFree()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }
}
