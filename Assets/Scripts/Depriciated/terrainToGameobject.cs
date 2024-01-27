using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class terrainToGameobject : MonoBehaviour
{
    public Texture defaultTexture;

    private GameObject convertTerrain(Terrain t)
    {
        GameObject newTerrain = new GameObject();
        newTerrain.AddComponent<MeshFilter>().mesh = generateMeshFromTerrain(t, 100);
        newTerrain.AddComponent<MeshRenderer>();
        setMaterialAndTexture(newTerrain);

        return newTerrain;
    }

    private void setMaterialAndTexture(GameObject terrain)
    {
        Renderer r = terrain.GetComponent<Renderer>();
        Material m = new Material(Shader.Find("Standard"));
        m.mainTexture = defaultTexture;
        r.material = m;
    }

    // Method to generate mesh verticies and triangles from terrain gameobject
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
