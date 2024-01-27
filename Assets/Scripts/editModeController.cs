using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.UI;
using System.Reflection;

public class editModeController : MonoBehaviour
{
    private Canvas c;
    public Camera cam;
    public GameObject spotlight;
    private Vector3 mousePosinWorld;
    private Terrain terrain;
    private GameObject curSceneObject;
    public Dropdown prefabList;
    private GameObject sceneController;
    private GameObject tempCollision;
    private GameObject lastObject;
    private Vector3 terrainBounds;
    public GameObject wall;
    public TMP_InputField fileNameInput;

    private float scrollSensitivity = 80f;
    private playerMovement pmScript;
    private exportScene exportSceneScript;
    private Dictionary<KeyCode, string> buttons;
    private GameObject[] objectPrefabs;
    private Vector3 curMousePos;
    private cameraController camCon;
    private Vector3 spotlightPosition;
    private MapGeneration MGScript;

    void Start()
    {
        curSceneObject = null;
        terrain = null;
        sceneController = GameObject.Find("SceneManager");
        pmScript = sceneController.GetComponent<playerMovement>();
        camCon = sceneController.GetComponent<cameraController>();
        MGScript = gameObject.GetComponent<MapGeneration>();
        setButtons();
    }

    public void updateTerrainObject()
    {
        terrain = MGScript.GetCurrentTerrain();
        if (terrain)
        {
            terrainBounds = terrain.terrainData.size;
        }
    }

    public void callUpdatePrefabs()
    {
        updatePrefabs();
    }

    private void updatePrefabs()
    {
        objectPrefabs = MGScript.GetAllPrefabs();
        var tempList = new List<string>();
        foreach (GameObject ob in objectPrefabs)
        {
            tempList.Add(ob.name);
        }
        prefabList.ClearOptions();
        prefabList.AddOptions(tempList);
    }

    private void retrieveLastObject()
    {
        if (lastObject != null)
        {
            curSceneObject = lastObject;
        }
    }

    private void setButtons()
    {
        buttons = new Dictionary<KeyCode, string>(){
            {KeyCode.Equals, "makeObjectBigger" },
            {KeyCode.Minus, "makeObjectSmaller" },
            {KeyCode.R, "rotateObject" },
            {KeyCode.Q, "deleteCurrentObject" },
            {KeyCode.Mouse1, "outputCurHit" },
            {KeyCode.E, "spawnObject" },
            {KeyCode.Slash, "updatePrefabs" },
            {KeyCode.Alpha1, "changePrefabSelection" },
            {KeyCode.Backspace, "playerSwitch" },
            {KeyCode.Z, "retrieveLastObject" },
            { KeyCode.LeftShift, "connectWalls" },
        };
    }

    private void playerSwitch()
    {
        if (camCon.currentCamera.name == "EditModeCamera")
        {
            GameObject.Find("FirstPersonController").GetComponent<playerController>().setCanMove(true);
            camCon.setCamera("Main Camera");
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else if (camCon.currentCamera.name == "Main Camera")
        {
            camCon.setCamera("EditModeCamera");
            GameObject.Find("FirstPersonController").GetComponent<playerController>().setCanMove(false);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
    }

    private void makeObjectBigger()
    {
        var scale = curSceneObject.transform.localScale;
        if (curSceneObject.name.Contains("Wall") | curSceneObject.name.Contains("Floor"))
        {
            scale = new Vector3(scale.x + 2, scale.y, scale.z);
        }
        else if (curSceneObject != null)
        {
            scale = new Vector3(scale.x + .5f, scale.y + .5f, scale.z + .5f);
        }
        curSceneObject.transform.localScale = scale;
        moveObject();
    }

    private void makeObjectSmaller()
    {
        var scale = curSceneObject.transform.localScale;
        if (curSceneObject.name.Contains("Wall") | curSceneObject.name.Contains("Floor"))
        {
            scale = new Vector3(scale.x - 2, scale.y, scale.z);
        }
        else if (curSceneObject != null)
        {
            scale = new Vector3(scale.x - .5f, scale.y - .5f, scale.z - .5f);
        }
        if (scale.x > 0 && scale.y > 0 && scale.z > 0)
        {
            curSceneObject.transform.localScale = scale;
        }

        moveObject();
    }

    private void changePrefabSelection()
    {
        if (prefabList.value < prefabList.options.Count)
        {
            prefabList.value = prefabList.value + 1;
        }
        else
        {
            prefabList.value = 0;
        }
    }

    private bool checkInTerrain(GameObject ob=null)
    {
        bool check;
        Vector3 obPos;
        if (ob == null)
        {
            obPos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.position.y));
        }
        else
        {
            obPos = ob.transform.position;
        }

        
        if (obPos.x < terrainBounds.x && obPos.x > 0 && obPos.z < terrainBounds.z && obPos.z > 0)
        {
            check = true;
        }
        else
        {
            check = false;
        }

        return check;
    }

    private void runEditMode()
    {
        if (prefabList.options.Count == 0)
        {
            updatePrefabs();
        }

        foreach (var buttonMethodDict in buttons)
        {
            if (Input.GetKeyDown(buttonMethodDict.Key))
            {
                MethodInfo mi = this.GetType().GetMethod(buttonMethodDict.Value, BindingFlags.NonPublic | BindingFlags.Instance);
                mi.Invoke(this, null);
            }
        }

        spotlightPosition = spotlight.transform.position;
        spotlight.SetActive(true);
        updateTerrainObject();
        transform.Translate(Input.GetAxis("Horizontal") * Vector3.right);
        transform.Translate(Input.GetAxis("Vertical") * Vector3.up);
        transform.Translate(Input.GetAxis("Mouse ScrollWheel") * Vector3.forward * scrollSensitivity);

/*        if (camCon.getCurrentCamera() == "EditModeCamera" && checkInTerrain(spotlight))
        {
            spotlight.SetActive(true);
            updateTerrainObject();
            transform.Translate(Input.GetAxis("Horizontal") * Vector3.right);
            transform.Translate(Input.GetAxis("Vertical") * Vector3.up);
            transform.Translate(Input.GetAxis("Mouse ScrollWheel") * Vector3.forward * scrollSensitivity);
        }
        else
        {
            spotlight.SetActive(false);
        }*/

        // spotlight follows mouse
        curMousePos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.transform.position.y));
        spotlight.transform.position = new Vector3(curMousePos.x,
            terrain.SampleHeight(new Vector3(curMousePos.x, 0, curMousePos.z)),
            curMousePos.z);

        // select scene object to move it
        if (Input.GetKeyDown(KeyCode.Mouse0) && curSceneObject == null)
        {
            setCurrentObject();
        }

        // place currently holding object
        else if ((Input.GetKeyDown(KeyCode.Mouse0) | Input.GetKeyDown(KeyCode.F)) && curSceneObject != null)
        {
            lastObject = curSceneObject;
            curSceneObject.transform.position = new Vector3(curSceneObject.transform.position.x,
                terrain.SampleHeight(curSceneObject.transform.position),
                curSceneObject.transform.position.z);
            curSceneObject = null;
            tempCollision = null;
        }

        if (curSceneObject != null)
        {
            moveObject();
        }
    }

    void Update()
    {
        if (!terrain)
        {
            updateTerrainObject();
        }

        if (checkInTerrain() && ((Camera)FindFirstObjectByType(typeof(Camera))).gameObject.name == "EditModeCamera")
        {
            runEditMode();
        }

/*        if (terrain && ((Camera)FindFirstObjectByType(typeof(Camera))).gameObject.name == "EditModeCamera")
        {
            runEditMode();
        }*/
    }

    private void outputCurHit()
    {
        Debug.Log($"curOb {curSceneObject}, temp{tempCollision}");
    }

    private void connectWalls()
    {
        if (Input.GetKey(KeyCode.LeftShift) && camCon.currentCamera.gameObject.name == "EditModeCamera")
        {
            GameObject con1 = null;
            GameObject con2 = null;
            List<GameObject> conList = new List<GameObject>();
            foreach(Transform child in lastObject.transform)
            {
                if (child.name.Contains("con"))
                {
                    conList.Add(child.gameObject);
                }
            }

            foreach (Transform child in curSceneObject.transform)
            {
                if (child.name.Contains("con"))
                {
                    conList.Add(child.gameObject);
                }
            }

            float leastDistance = 0;
            float curDistance;
            foreach (GameObject curPos1 in conList)
            {
                foreach (GameObject curPos2 in conList)
                {
                    curDistance = Vector3.Distance(curPos1.transform.position, curPos2.transform.position);
                    if ((leastDistance == 0 | curDistance < leastDistance) && (curPos1.transform.parent != curPos2.transform.parent))
                    {
                        leastDistance = curDistance;
                        con1 = curPos1;
                        con2 = curPos2;
                    }
                }
            }
            if (con1 && con2)
            {
                //tgScript.drawWall(wall, con1, con2);
            }
        }
    }

    private int getSelection()
    {
        int selection = prefabList.value;
        return selection;
    }

    private void rotateObject()
    {
        if (curSceneObject != null)
        {
            curSceneObject.transform.Rotate(new Vector3(0, 90, 0));
        }
    }

    private void moveObject()
    {
        mousePosinWorld = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.position.y));
        curSceneObject.transform.position = new Vector3(mousePosinWorld.x,
            terrain.SampleHeight(curSceneObject.transform.position), mousePosinWorld.z);
    }

    public void callSpawn()
    {
        spawnObject();
    }

    private void spawnObject()
    {
        int selection = getSelection();
        //GameObject tempOb = transform.GetComponent<townGeneration>().instantiateObject(objectPrefabs[selection], Vector3.zero, Quaternion.identity, "Details", true);
        GameObject tempOb = MGScript.CreateNewObject(objectPrefabs[selection], Vector3.zero, Quaternion.identity, "Details");
        curSceneObject = tempOb;
    }

    private void setCurrentObject()
    {
        curSceneObject = tempCollision;
    }

    private void deleteCurrentObject()
    {
        if (curSceneObject != null)
        {
            Destroy(curSceneObject);
            curSceneObject = null;
        }
    }

    public void setCurrentCollisionObject(GameObject ob)
    {
        if (ob && terrain && ob.gameObject != terrain.gameObject)
        {
            tempCollision = ob;
        }
    }

    public void callExporttoFBX()
    {
        GameObject obParent = GameObject.FindGameObjectWithTag("Object Parent");
        string fileName = fileNameInput.text;
        if (fileName == null)
        {
            fileName = PlayerPrefs.GetString("lastFbxFile");
            if (fileName == "")
            {
                fileName = "default";
            }
        }
        exportSceneScript.ExportMapAsFBX(obParent, fileName);

    }


}
