using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using System.IO;
//using UnityEditor.Formats.Fbx.Exporter;
public class exportScene : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject sceneTemplate;

    public void ExportMapAsFBX(GameObject parentObj, string fileName = "default")
    {
        // get all objects attached to parent gameobject
        GameObject allObjs = GameObject.FindGameObjectWithTag("Object Parent");
        // get terrain and convert to gameobject (Terrain type does not automatically convert to FBX,
        // so you have to generate a mesh based on Terrain and create new GameObject with it
        Terrain tOb = FindAnyObjectByType<Terrain>();
        GameObject terrainGameObject = convertTerrain(tOb);
        terrainGameObject.transform.SetParent(allObjs.transform);

        // get default filepath for project and export Parent Gameobject (and all children) to FBX
        string filePath = Path.Combine($"{Application.dataPath}/Resources/Maps/", $"{fileName}.fbx");
        ModelExporter.ExportObject(filePath, allObjs);
        Debug.Log($"Successful FBX Export in {filePath}");
    }

    // not in use
    private void removeReadWriteError(GameObject parent)
    {
        GameObject deadParent = new GameObject();
        string path;
        ModelImporter importer;
        foreach(Transform child in parent.transform)
        {
            path = AssetDatabase.GetAssetOrScenePath(child.gameObject);
            print(path);
            importer = (ModelImporter)ModelImporter.GetAtPath(path);
            if (child.TryGetComponent<Mesh>(out Mesh m) && !m.isReadable)
            {
                child.SetParent(deadParent.transform);
            }
        }
        Destroy(deadParent);
    }

    // Add extras not already in parent object to parent, add player if chosen, export as scene if chosen
    public GameObject CompileIntoParent(GameObject parentObj, List<GameObject> objsToAdd, bool withPlayer)
    {
        foreach(GameObject obj in objsToAdd)
        {
            obj.transform.SetParent(parentObj.transform);
        }

        if (withPlayer)
        {
            // export with player controller too
            GameObject playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            playerInstance.transform.SetParent(parentObj.transform);
        }
        return parentObj;
    }

    private GameObject convertTerrain(Terrain t)
    {
        // create empty gameobject and assign it mesh generated from Terrain
        GameObject newTerrain = new GameObject();
        newTerrain.name = "Terrain";
        newTerrain.tag = "Terrain";
        newTerrain.transform.position = t.GetPosition();
        newTerrain.AddComponent<MeshFilter>().mesh = generateMeshFromTerrain(t, 100);
        newTerrain.AddComponent<MeshRenderer>();
        generateMaterial(newTerrain, t.terrainData.terrainLayers[0].diffuseTexture);

        return newTerrain;
    }

    private void generateMaterial(GameObject terrain, Texture texture)
    {
        Renderer r = terrain.GetComponent<Renderer>();
        Material m = new Material(Shader.Find("Standard"));
        m.mainTexture = texture;
        r.material = m;
    }

    // Method to generate mesh verticies and triangles from terrain gameobject
    // terrain t = terrain to 'copy', int sqrtVerticies = square root of the maximum verticies you want to use
    // (i.e. if sqrtVerticies = 10, maximum verticies will be 100)
    private Mesh generateMeshFromTerrain(Terrain t, int sqrtVerticies)
    {
        int totalVerticies = sqrtVerticies * sqrtVerticies;
        Mesh newMesh = new Mesh();

        //Set verticies array with number of verticies (was 62500 - 250 * 250 size of terrain)
        Vector3[] verticies = new Vector3[totalVerticies];

        // work out distance between each vertex given max number of verticies and terrain area
        // i.e. how many total verticies fit into a terrain surface area, then sqrt to get size of each individual 'square' 
        float distanceBetweenVerticies = Mathf.Sqrt((t.terrainData.size.x * t.terrainData.size.z) / totalVerticies);

        var index = 0;
        for (int col = 0; col < Mathf.Sqrt(verticies.Length); col++)
        {
            for (int row = 0; row < Mathf.Sqrt(verticies.Length); row++)
            {
                verticies[index] = new Vector3(row * distanceBetweenVerticies,
                    t.SampleHeight(new Vector3(row * distanceBetweenVerticies, 0, col * distanceBetweenVerticies)),
                    col * distanceBetweenVerticies);
                index++;
            }
        }

        // setting normals and uvs
        Vector3[] normals = new Vector3[totalVerticies];
        Vector2[] uvs = new Vector2[totalVerticies];
        for (index = 0; index < totalVerticies; index++)
        {
            normals[index] = Vector3.up;
            uvs[index] = new Vector2(verticies[index].x, verticies[index].z);
        }



        // setting triangles based on verticies
        int colLength = sqrtVerticies;
        // total number of triangles needed (colLength-1 ^2 * 2) * number of verticies needed per triangle (*3)
        // (colLength^2 * 2) * 3
        int[] tris = new int[((colLength - 1) * (colLength - 1) * 2 * 3) + ((colLength - 2) * 2 * 3)];

        int i = 0;
        for (int triIndex = 0; triIndex < tris.Length; triIndex += 6)
        {
            tris[triIndex] = i;
            tris[triIndex + 1] = i + colLength;
            tris[triIndex + 2] = i + 1;

            tris[triIndex + 3] = i + colLength;
            tris[triIndex + 4] = i + colLength + 1;
            tris[triIndex + 5] = i + 1;
            i++;
        }

        // set values of new mesh to calculated values and return mesh
        newMesh.vertices = verticies;
        newMesh.normals = normals;
        newMesh.uv = uvs;
        newMesh.triangles = tris;
        return newMesh;
    }
}
