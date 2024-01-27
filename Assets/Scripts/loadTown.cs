using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using TMPro;
using UnityEditor;

public class loadTown : MonoBehaviour
{
    public utils utilsScript;
    public bool autosave;
    private townGeneration tgScript;
    public string file;
    public string FBXFile;
    private List<Vector3> allTilePositions;
    private List<string> saveOptions;
    public Dropdown saveOptionsDropdown;
    public TMP_InputField fbxNameInputField;
    public class ObjectInformation
    {
        // all columns for file
        public string name;

        public float positionX;
        public float positionY;
        public float positionZ;

        public float rotationX;
        public float rotationY;
        public float rotationZ;
    }

    void Start()
    {
        utilsScript = GameObject.Find("SceneManager").GetComponent<utils>();
        autosave = false;
        saveOptions = new List<string>() { "Save 1", "Save 2", "Save 3" };
        file = saveOptions[0];
        FBXFile = fbxNameInputField.text;
        tgScript = GameObject.Find("EditModeController").GetComponent<townGeneration>();
    }

    private void OnApplicationQuit()
    {
        if (autosave)
        {
            saveObjects();
        }
    }
    private void writeCSV(string data)
    {
        setFile();
        string path = $"Assets/Files/{file}.csv";
        using (StreamWriter writer = File.AppendText(path))
        {
            writer.WriteLine(data);
        }
    }
    private ObjectInformation[] readCSV()
    {
        ObjectInformation[] objectInformation;
        setFile();
        string path = $"Assets/Files/{file}.csv";
        string[] data = File.ReadAllLines(path);
        if (data.Length != 0)
        {
            // table size is data length divided by number of columns, then -1 to ignore the first row of headings
            int tableSize = data.Length;
            objectInformation = new ObjectInformation[tableSize-1];

            for (int i = 1; i < tableSize; i++)
            {
                string[] dataSubset = data[i].Split(",");
                objectInformation[i-1] = new ObjectInformation();
                objectInformation[i-1].name = dataSubset[0];
                objectInformation[i-1].positionX = float.Parse(dataSubset[1]);
                objectInformation[i-1].positionY = float.Parse(dataSubset[2]);
                objectInformation[i-1].positionZ = float.Parse(dataSubset[3]);

                objectInformation[i-1].rotationX = float.Parse(dataSubset[4]);
                objectInformation[i-1].rotationY = float.Parse(dataSubset[5]);
                objectInformation[i-1].rotationZ = float.Parse(dataSubset[6]);
            }
            return objectInformation;
        }
        return null;
    }
    private void clearCSV()
    {
        print("Clearing Previous Save...");
        string path = $"Assets/Files/{file}.csv";
        File.WriteAllText(path, "");
        print("Save Cleared.");
    }

    public void setFile()
    {
        file = saveOptions[saveOptionsDropdown.value];
    }
    public void saveObjects()
    {
        print("Saving...");
        setFile();
        clearCSV();

        List<GameObject> allObjs = new List<GameObject>();
        GameObject parentOb = GameObject.FindGameObjectWithTag("Object Parent");

        for (int i = 0; i < parentOb.transform.childCount; i++)
        {
            allObjs.Add(parentOb.transform.GetChild(i).gameObject);
        }

        writeCSV("name,positionX,positionY,positionZ,rotationX,rotationY,rotationZ");

        foreach (GameObject obj in allObjs)
        {
            Vector3 pos = obj.transform.position;
            Vector3 rot = obj.transform.rotation.eulerAngles;
            string objInfoString = $"{obj.name},{pos.x},{pos.y},{pos.z},{rot.x},{rot.y},{rot.z}";
            writeCSV(objInfoString);
        }

        Terrain t = Terrain.activeTerrain;
        
        foreach (Vector3 tilePos in tgScript.getAllTilePositions())
        {
            string namePosString = $"Floor,{tilePos.x},{tilePos.y},{tilePos.z},{0},{0},{0}";
            writeCSV(namePosString);
        }
        print("Save Complete.");
    }

    public void loadObjects()
    {
        print("loading...");
        utilsScript.killMap();
        GameObject[] objectPrefabs = tgScript.objectPrefabs;
        Dictionary<string, GameObject> obDict = new Dictionary<string, GameObject>();
        allTilePositions = new List<Vector3>();
        foreach (GameObject obj in objectPrefabs)
        {
            obDict.Add(obj.name, obj);
        }

        ObjectInformation[] objectInformation = readCSV();
        if (objectInformation == null)
        {
            print("Read CSV Error");
        }
        foreach(var ob in objectInformation)
        {
            string n = ob.name.Replace("(Clone)", "");
            if (n == "Floor")
            {
                allTilePositions.Add(new Vector3(ob.positionX, ob.positionY, ob.positionZ));
                tgScript.allTilePositions.Add(new Vector3(ob.positionX, ob.positionY, ob.positionZ));
            }
            else if (obDict.ContainsKey(n))
            {
                GameObject tempOb = obDict[n];
                Vector3 tempPos = new Vector3(ob.positionX, ob.positionY, ob.positionZ);
                Quaternion tempRot = Quaternion.Euler(ob.rotationX, ob.rotationY, ob.rotationZ);
                GameObject tempInstance = tgScript.instantiateObject(tempOb, tempPos, tempRot, null, true);
            }
            else
            {
                print($"{n} object not in object prefabs");
            }
        }
        tgScript.terrainPainter(allTilePositions);
        print("Load Complete.");
    }

    public void loadFBX()
    {
        utilsScript.killMap();
        FBXFile = fbxNameInputField.text;
        PlayerPrefs.SetString("lastFbxFile", FBXFile);

        // import map with proper Material settings to preserve Albedo colours
        ModelImporter importer = (ModelImporter)ModelImporter.GetAtPath($"Assets/Resources/Maps/{FBXFile}.fbx");
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;

        // load resource
        GameObject parent = (GameObject)Resources.Load($"Maps/{FBXFile}");

        parent = Instantiate(parent, Vector3.zero, Quaternion.identity);

        parent.name = "Object Parent";
        parent.tag = "Object Parent";
        Destroy(parent.transform.Find("Terrain").gameObject);
        tgScript.spawnEmptyMap();

        setComponents(parent);
    }

    private void setComponents(GameObject parentOb)
    {
        foreach(Transform ob in parentOb.transform)
        {
            // add box colliders to each new object so they can be selected
            if (!ob.gameObject.name.Contains("Terrain"))
            {
                addBoxCollider(ob.gameObject);
            }
        }
    }

    private void addBoxCollider(GameObject ob)
    {
        ob.AddComponent<MeshRenderer>();
        Vector3 size = ob.GetComponent<Renderer>().bounds.size;
        ob.AddComponent<BoxCollider>();
        BoxCollider bc = ob.GetComponent<BoxCollider>();
        bc.size = size;
    }
}
