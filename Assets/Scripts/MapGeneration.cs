using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;


// Map Gen 2.0 
public class Road
{
    public int id;
    public Vector3 centre;
    public List<GameObject> buildings = new List<GameObject>();
    public GameObject[] walls;
    public GameObject[] details;
    public bool hasGround;

    public Vector3 pointA;
    public Vector3 pointB;
    public Vector3 direction;
    public Road prev;
    public Road pointer;

    // width and length of road, where 
    public float width;
    public float length;
    
    public Vector3 road;
}

public class Connection
{
    public Connection prevCon = null;
    public Connection nextCon = null;
    public int[] index = new int[2];
    public Vector3 position;
    public float centralPullForce = 0;
}

public class MapConfig
{
    public string mapName;
    public Terrain terrain;
    public float density;
    public float uniformity;
    public string theme;
    public float length;
    public float width;
    public float minRoadLength;
    public float minRoadWidth;
    public GameObject[] obsOfTheme;
    public float gravWeight;
    public float angleWeight;
    public Vector3 initPoint;
}

public class MapGeneration : MonoBehaviour
{
    //for testing, remove later 
    public GameObject tree;
    public GameObject debugOb;
    private Terrain mapTerrain;
    private Vector3 terrainCentre;
    private List<GameObject> allObjectsInMap;
    private List<Road> allRoads;
    private GameObject[] obsOfTheme;
    public MapConfig curConfig;
    private List<Vector3> unusedPoints;
    public bool debug = false;
    private GameObject[] objectPrefabs;
    Vector3[,] allPoints;

    void Start()
    {
        debug = false;
        SetMapTerrain();
        SetDefaults();
        if (debug)
        {
            NewMap();
        }
    }
    private MapConfig SetCurrentMapConfig()
    {
        MapConfig tempConfig = new MapConfig();
        tempConfig.width = 10f;
        tempConfig.length = 10;
        tempConfig.theme = "default";
        tempConfig.uniformity = 1;
        tempConfig.density = 0.3f;
        tempConfig.angleWeight = 5;
        tempConfig.gravWeight = 3;
        tempConfig.initPoint = new Vector3(60f, 0f, 60f);
        tempConfig.terrain = GameObject.Find("Terrain").GetComponent<Terrain>();
        return tempConfig;
    }

    private void updatePrefabs()
    {
        var all = Resources.LoadAll<GameObject>("Prefabs");

        // remove prefabs that do not have sufficient mesh import settings to be exported later as FBX (read/write = true)
        List<GameObject> allList = new List<GameObject>();
        foreach (var p in all)
        {
            var potentialMesh = p.TryGetComponent<MeshFilter>(out MeshFilter m);
            if ((potentialMesh && m.sharedMesh && m.sharedMesh.isReadable) |
                (p.GetComponentInChildren<MeshFilter>().sharedMesh && p.GetComponentInChildren<MeshFilter>().sharedMesh.isReadable))
            {
                allList.Add(p);
            }
        }
        objectPrefabs = new GameObject[allList.Count];
        int i = 0;
        foreach (var x in allList)
        {
            try
            {
                objectPrefabs[i] = (GameObject)x;
            }
            catch (Exception)
            {
                print($"Object {x} of type {x.GetType()} failed to convert to GameObject");
            }
            i++;
        }
    }

    public GameObject[] GetAllPrefabs()
    {
        updatePrefabs();
        return objectPrefabs;
    }

    public GameObject[] GetAllObjects()
    {
        if (allObjectsInMap == null)
        {
            print("Object not initialised");
            return null;
        }
        else
        {
            return allObjectsInMap.ToArray();
        }
    }
    public void NewMap(MapConfig m = null)
    {
        m = null;
        ResetTerrainPaint();
        if (m == null)
        {
            curConfig = SetCurrentMapConfig();
        }
        else
        {
            curConfig = m;
        }

        obsOfTheme = GetObjectsOfTheme(curConfig.theme);
        GenerateMap();
        //curveRoad();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            regenerateMap();
        }
    }

    private void ResetTerrainPaint()
    {
        foreach (Road r in allRoads)
        {
            PaintTerrain(r.road, r.direction, true);
        }

    }

    public void resetRoads()
    {
        allRoads.Clear();
        foreach (GameObject ob in allObjectsInMap)
        {
            Destroy(ob);
        }
        allObjectsInMap.Clear();
    }

    public void regenerateMap()
    {
        ResetTerrainPaint();
        resetRoads();
        curConfig = SetCurrentMapConfig();
        obsOfTheme = GetObjectsOfTheme(curConfig.theme);
        //GenerateMap();
        GenerateMapV3();
    }

    private void SetDefaults()
    {
        allRoads = new List<Road>();
        allObjectsInMap = new List<GameObject>();
    }

    private void GenerateFromArray(Connection[,] arr)
    {
        int roads = 0;
        foreach (Connection c in arr)
        {
            if (c != null && c.nextCon != null)
            {
                Vector3 dir = (c.nextCon.position - c.position).normalized;
                GenerateRoad(c.position, c.nextCon.position, dir);
                roads += 1;
            }
        }
        print($"Roads: {roads}");
    }

    private Connection[,] GenerateMapTemplate()
    {
        float maxRoadLength = curConfig.length;
        float maxRoadWidth = curConfig.width;
        int arrWidth = Mathf.FloorToInt((mapTerrain.terrainData.size.x - maxRoadLength) / maxRoadLength);
        int arrHeight = Mathf.FloorToInt((mapTerrain.terrainData.size.z - maxRoadLength) / maxRoadLength);
        allPoints = new Vector3[arrWidth, arrHeight];
        Connection[,] mapPoints = new Connection[arrWidth, arrHeight];
        int w = 0;
        int h = 0;
        for (float x = maxRoadLength; x < mapTerrain.terrainData.size.x - maxRoadLength; x += maxRoadLength)
        {
            for (float z = maxRoadLength; z < mapTerrain.terrainData.size.z - maxRoadLength; z += maxRoadLength)
            {
                Vector3 newPos = new Vector3(x, 0, z);
                allPoints[w, h] = newPos;
                Connection newCon = new Connection();
                newCon.index = new int[2] { w, h };
                newCon.nextCon = null;
                newCon.prevCon = null;
                newCon.position = new Vector3(x, 0, z);
                mapPoints[w, h] = newCon;
                w += 1;
            }
            h += 1;
            w = 0;
        }
        return mapPoints;
    }

    private void curveRoad()
    {
        Connection[,] map = GenerateMapTemplate();
        int[] curIndex = new int[2] { 0, 0 };
        Connection curCon = null;
        Connection prevCon = map[0, 0];
        for (int y = 0; y < map.GetLength(0) - 1; y++)
        {
            for (int x = 0; x < map.GetLength(1) - 1; x++)
            {

                if ((x == map.GetLength(0) - 2 && y % 2 == 0) | (x == 0 && y % 2 != 0))
                {
                    curCon = map[x, y + 1];
                }
                else
                {
                    curCon = map[x, y];
                }

                if ((curCon != null && curCon.nextCon == null && x != map.GetLength(0) - 1))
                {
                    prevCon.nextCon = curCon;
                    curCon.prevCon = prevCon;
                    prevCon = curCon;
                }
            }
        }
        GenerateFromArray(map);
    }


    private Connection[,] FillArray(int xin, int yin)
    {
        Connection temp;
        Connection[,] full = new Connection[xin, yin];
        for (int x = 0; x < full.GetLength(0); x++)
        {
            for (int y = 0; y < full.GetLength(1); y++)
            {
                temp = new Connection();
                temp.index = new int[] { x, y };
                full[x, y] = temp;

            }
        }
        return full;
    }

    private static float LogisticSigmoid(float x, float bias)
    {
        return (float)(1.0 / (1.0 + (Math.Exp(-x * bias))));
    }

    private void GenerateMapV3()
    {
        // generate array of nodes (positions relative to terrain)
        Connection[,] map = GenerateMapTemplate();
        Connection curNode = map[0, 0];//map[Random.Range(0, map.GetLength(0)), Random.Range(0, map.GetLength(1))];
        Connection candidate = null;
        Connection[] neighborNodes = new Connection[8];
        List<float[]> directions = GenerateCombinations(2);
        List<Connection> usedCons = new List<Connection>();
        Vector3 centre = new Vector3(500, 0, 500);
        Dictionary<Connection, float> neighborWeights = new Dictionary<Connection, float>();
        float gravWeight = curConfig.gravWeight;
        float angleWeight = curConfig.angleWeight;
        // arbratrary number for now, just for testing
        int i = 0;
        while (i < 1000 && curNode != null)
        {
            neighborNodes = GetNeighbors(curCon: curNode, map: map, dirs: directions, usedCons: usedCons);

            //print($"{curNode} {map.Length} {directions.Count} {usedCons.Count}");
            neighborWeights = CalculateNeighborWeights(cur: curNode, cons: neighborNodes, centre: centre, gravWeight, angleWeight);
            if (neighborNodes.Length != 0 && neighborWeights.Count != 0)
            {
                int index = GetNumberInArray(neighborWeights);
                //int index = GetBestNeighbor2(neighborWeights);
                if (index != -1)
                {
                    // get Connection from dictionary
                    Connection[] keysArray = GetKeysArray(neighborWeights);
                    candidate = keysArray[index];
                    //usedCons.Add(candidate);

                    curNode.nextCon = candidate;
                    candidate.prevCon = curNode;
                    curNode = candidate;
                }
            }
            else
            {
                curNode = map[Random.Range(0, map.GetLength(0) - 1), Random.Range(0, map.GetLength(1) - 1)];
            }

            i++;
        }
        print($"{i} - {curNode}");
        GenerateFromArray(map);
    }

    static TKey[] GetKeysArray<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
    {
        TKey[] keysArray = new TKey[dictionary.Count];
        int index = 0;
        foreach (var key in dictionary.Keys)
        {
            keysArray[index++] = key;
        }
        return keysArray;
    }

    // return all possible neighbors of given node
    private static Connection[] GetNeighbors(Connection curCon, Connection[,] map, List<float[]> dirs, List<Connection> usedCons)
    {
        List<Connection> neighborNodes = new List<Connection>();
        Connection tempCon;
        int[] i = curCon.index;
        Vector3 curVector;
        int indexX = 0;
        int indexY = 0;
        foreach (float[] dir in dirs)
        {
            curVector = new Vector3(dir[0], 0, dir[1]);
            indexX = Convert.ToInt32(curCon.index[0] + curVector.x);
            indexY = Convert.ToInt32(curCon.index[1] + curVector.z);
            if (indexX >= 0 && indexX < map.GetLength(0) && indexY >= 0 && indexY < map.GetLength(1))
            {
                tempCon = map[indexX, indexY];
                // might cause issues?? - no connections can be made so there will always be space
                if (!usedCons.Contains(tempCon))
                {
                    neighborNodes.Add(tempCon);
                }
            }
        }
        return neighborNodes.ToArray();
    }

    // again, the magic work of ChatGPT (might not work, check later
    // recursive
    static List<float[]> GenerateCombinations(int length)
    {
        List<float[]> combinationsList = new List<float[]>();
        float[] currentCombination = new float[length];
        GenerateCombinationsRecursive(combinationsList, currentCombination, 0);
        return combinationsList;
    }

    static void GenerateCombinationsRecursive(List<float[]> combinationsList, float[] currentCombination, int index)
    {
        if (index >= currentCombination.Length)
        {
            // Store the generated combination
            combinationsList.Add((float[])currentCombination.Clone());
            return;
        }

        foreach (float value in new float[] { 0, 1, -1 })
        {
            currentCombination[index] = value;
            GenerateCombinationsRecursive(combinationsList, currentCombination, index + 1);
        }
    }

    // the magical work of ChatGPT (adapted for this purpose)
    static int GetNumberInArray(Dictionary<Connection, float> dict)
    {
        Connection[] array = GetKeysArray(dict);

        // Step 1: Normalize the array
        float[] probabilities = new float[array.Length];
        float sum = 0;

        for (int i = 0; i < array.Length; i++)
        {
            sum += dict[array[i]];
        }

        for (int i = 0; i < array.Length; i++)
        {
            probabilities[i] = dict[array[i]] / sum;
        }

        // Step 2: Generate a random number between 0 and 1
        float randomValue = Random.Range(0f, 1f);

        // Step 3: Iterate through the normalized array
        float cumulativeProbability = 0;

        for (int i = 0; i < probabilities.Length - 1; i++)
        {
            cumulativeProbability += probabilities[i];
            //print(cumulativeProbability);
            // Step 4: Return the corresponding integer when random number falls within the range
            if (randomValue <= cumulativeProbability)
            {
                return i;
            }
        }

        // This should not happen, but return -1 if something goes wrong
        return Random.Range(0, dict.Count - 1);
    }

    // literally pointless, maybe use later just in case I change my mind
    // returns greatest value in dictionary (like SoftMax for Connection probabilities)
    private int GetBestNeighbor(Dictionary<Connection, float> dict)
    {
        Connection[] arr = GetKeysArray(dict);
        if (dict.Count == 0) {
            Debug.Log("Empty Connection Dictionary :/");
            return Random.Range(0, 3);
        }
        Connection node;
        List<Connection> winners = new List<Connection>() { arr[0] };
        for (int i = 0; i < dict.Count; i++)
        {
            node = arr[i];
            if (dict[node] < dict[winners[0]])
            {
                winners.Clear();
                winners.Add(arr[i]);
            }
            else if (arr[i] == winners[0])
            {
                winners.Add(arr[i]);
            }
        }

        return Random.Range(0, winners.Count - 1);
    }

    private int GetBestNeighbor2(Dictionary<Connection, float> dict)
    {
        Connection[] arr = GetKeysArray(dict);
        if (dict.Count == 0)
        {
            Debug.Log("Empty Connection Dictionary :/");
            return Random.Range(0, 3);
        }

        List<Connection> winners = new List<Connection>() { arr[0] };
        float minDistance = dict[arr[0]];

        for (int i = 1; i < arr.Length; i++)
        {
            float distance = dict[arr[i]];
            if (distance < minDistance)
            {
                // Found a new minimum distance
                winners.Clear();
                winners.Add(arr[i]);
                minDistance = distance;
            }
            else if (distance == minDistance)
            {
                // Another winner with the same distance
                winners.Add(arr[i]);
            }
        }
        return Random.Range(0, winners.Count);
    }


        private static Dictionary<Connection, float> CalculateNeighborWeights(Connection cur, Connection[] cons, Vector3 centre, float gravWeight, float angleWeight)
    {
        Vector3 dir;
        Dictionary<Connection, float> neighborWeights = new Dictionary<Connection, float>();
        foreach(Connection c in cons)
        {
            if (c != null && !neighborWeights.ContainsKey(c))
            {
                dir = (cur.position - c.position).normalized;
                neighborWeights.Add(c, GetAttractorWeight(c.position, dir, centre, gravWeight, angleWeight));
            }
        }

        return neighborWeights;
    }

    // gravity and angle should be constant for any given map, weight can change. 
    private static float GetAttractorWeight(Vector3 position, Vector3 direction, Vector3 centre, float gravityWeight, float angleWeight)
    {
        // the further away, the higher the sigmoid will be
        float distanceFromCentre = Vector3.Distance(position, centre)/100;
        // the further from 90 degrees, the larger the values
        float angle = Vector3.Angle(Vector3.forward, direction) * Mathf.Deg2Rad;
        float angleFrom90 = Mathf.Sin(angle) * Mathf.Rad2Deg / 100;
        // average sigmoid from two values with biases towards map weights.
        return (LogisticSigmoid(distanceFromCentre, gravityWeight) + LogisticSigmoid(angleFrom90, angleWeight)) / 2;
    }

    // bottom-up map generation
    private void GenerateMapV2()
    {
        Connection[,] arr = FillArray(25, 25);
        float[] weights = new float[] { 0, 0, 0, 0 };
        float[] directions = new float[] { 1, -1 };
        arr[0, 0].position = new Vector3(0, 0, 0);
        Vector3 endPos;
        Connection prevC = arr[0, 0];
        int iterations = 0;
        float[] sigmoidWeights = new float[weights.Length];
        Vector3 centre = new Vector3(500, 0, 500);
        foreach (Connection c in arr)
        {
            c.prevCon = arr[0, 0];
            float d = SampleFromDistribution(mean: 400, sd: 100);
/*            for (int i = 0; i < weights.Length; i++)
            {
                sigmoidWeights[i] = LogisticSigmoid(weights[i], 2);
            }*/

            float radians = Random.Range(0,360) * Mathf.Deg2Rad;

            float dx = d * Mathf.Cos(radians);
            float dz = d * Mathf.Sin(radians);

            endPos = new Vector3(centre.x + dx, 0f, centre.z + dz);
            //Instantiate(debugOb, endPos, Quaternion.identity);
            c.position = endPos;
            prevC.nextCon = c;
            prevC = c;

            iterations++;
            if (iterations > 3000)
            {
                break;
            }
        }

        GenerateFromArray(arr);
    }

    private float[] ChangeWeights(float[] weights, float[] finalWeights)
    {
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] += Random.Range(0,10);
        }

        return weights;
    }

    private Connection[,] FitToTerrain(Connection[,] cons)
    {
        Vector3 t = curConfig.terrain.terrainData.size;
        foreach (Connection c in cons)
        {
            c.position = new Vector3(
                c.position.x/t.x,
                c.position.y / t.y * 0,
                c.position.z / t.z
                );
        }

        return cons;
    }


    // Get largest value from weights, if values are equal then coin toss between them.
    private float[] SoftMax(float[] weights)
    {
        int winner = 0;
        float[] final = new float[] {0, 0, 0, 0};

        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[winner] <= weights[i])
            {
                winner = i;
            }
        }

        final[winner] = 1;
        return final;
    }

    private Vector3 AggregateVectors(Vector3[] vectors)
    {
        float x = 0;
        float y = 0;
        float z = 0;
        foreach(Vector3 v in vectors)
        {
            x += v.x;
            y += v.y;
            z += v.z;
        }
        return new Vector3(x, y, z);
    }
    private float SampleFromDistribution(float mean, float sd)
    {
        
        return mean + (Random.Range(-sd, sd) * curConfig.uniformity);
    }

    // Generate map (in progress)
    /*private void GenerateMap(float mapDensity, float uniformity, float roadAngularity, float maxRoadWidth,
        float minRoadWidth, float maxRoadLength, float minRoadLength, Vector3 centre = default, float randomness = 0f, float angleResolution = 1f, string theme = "Default")*/
    private void GenerateMap()
    {
        string theme = curConfig.theme;

        List<int[]> freeIndexes = new List<int[]>();
        Connection[,] map = GenerateMapTemplate();
        foreach(Connection c in map)
        {
            if (c != null && c.index != null)
            {
                freeIndexes.Add(c.index);
            }
        }
        int[] mapSize = new int[2] { map.GetLength(0), map.GetLength(1) };
        // generate random maze 
        Connection curCon = null;
        Connection nextCon = null;
        int spaces = map.GetLength(0) * map.GetLength(1) * 2;
        int limit = 0;
        while (spaces > 100 && limit < 5)
        {
            Vector3Int newDir;
            int[] newIndex = new int[2] { 0, 0 };
            List<Vector3Int> dirOptions = new List<Vector3Int>() { Vector3Int.forward, Vector3Int.back, Vector3Int.left, Vector3Int.right };
            List<Vector3Int> dirOptionsCopy = new List<Vector3Int>() { Vector3Int.forward, Vector3Int.back, Vector3Int.left, Vector3Int.right };
            bool tr = true;
            limit = 0;
            Vector3Int curDir;
            while (tr && limit < 10)
            {
                // set initial connection in map. If previous connection hit another connection, pick random point in map.
                if ((curCon != null && curCon.nextCon != null) && curCon.nextCon != null)
                {
                    Vector3Int dir = Vector3Int.FloorToInt((curCon.nextCon.position - curCon.position).normalized);
                    dirOptions = new List<Vector3Int>() { dir };
                    //curCon = null;
                }

                if (curCon == null)
                {
                    bool isNull = true;
                    do
                    {
                        int[] i = freeIndexes[Random.Range(0, freeIndexes.Count)];
                        curCon = map[i[0], i[1]];
                        // curCon = map[Random.Range(0, map.GetLength(0)), Random.Range(0, map.GetLength(1))];
                        if (curCon != null && curCon.nextCon == null)
                        {
                            isNull = false;
                        }
                        dirOptions = new List<Vector3Int>() { Vector3Int.forward, Vector3Int.back, Vector3Int.left, Vector3Int.right };
                    }
                    while (isNull);
                }

                // prevent connection from going back on itself. 
                if (curCon.prevCon != null)
                {
                    curDir = Vector3Int.FloorToInt((curCon.position - curCon.prevCon.position).normalized);
                    if (dirOptions.Contains(curDir)){
                        dirOptions.Remove(curDir);
                    }
                }

                // remove possible directions that are unavalible
                foreach (Vector3Int dir in dirOptionsCopy)
                {
                    newIndex = new int[2] { curCon.index[0] + dir.x, curCon.index[1] + dir.z };
                    if (newIndex[0] >= map.GetLength(0) - 2 | newIndex[1] >= map.GetLength(1) - 2 | newIndex[0] <= 1 | newIndex[1] <= 1)
                    {
                        dirOptions.Remove(dir);
                    }
                }
                if (dirOptions.Count == 0)
                {
                    curCon = null;
                }
                else
                {
                    tr = false;
                }
                limit++;
            }

            if (dirOptions.Count > 0)
            {
                newDir = dirOptions[Random.Range(0, dirOptions.Count)];
                newIndex = new int[2] { curCon.index[0] + newDir.x, curCon.index[1] + newDir.z };
                freeIndexes.Remove(newIndex);
                nextCon = map[newIndex[0], newIndex[1]];

                nextCon.prevCon = curCon;
                curCon.nextCon = nextCon;
                spaces--;
                // if next connection has another connection, set up current connection to go to random point in map
                if (nextCon.nextCon != null)
                {
                    //curCon = null;
                    curCon = nextCon;
                }
                else
                {
                    curCon = nextCon;
                }
            }
            else
            {
                curCon = null;
            }
            
        }
        nextCon = null;
        spawnTrees(map);
        GenerateFromArray(map);
    }

    // for testing atm, not finished
    private void spawnTrees(Connection[,] map)
    {
        foreach (Connection c in map)
        {
            if (c != null && (c.nextCon == null | c.prevCon == null))
            {
                Vector3 newPos = map[c.index[0], c.index[1]].position;
                float scale = Random.Range(.5f, 2.5f);
                tree.transform.localScale = new Vector3(scale, scale, scale);
                //tree.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
                CreateNewObject(tree, newPos, Quaternion.identity, "Tree");
            }
        }
    }

    private Vector3 SetRoadVectors(Road road)
    {

        float angleFromCentre = Vector3.Angle(Vector3.forward, road.direction) * (Mathf.PI / 180);
        float addX = road.width * Mathf.Sin(angleFromCentre + (90 * (Mathf.PI / 180)));
        float addZ = road.width * Mathf.Cos(angleFromCentre + (90 * (Mathf.PI / 180)));
        Vector3 rotateDir = new Vector3(Mathf.Sin(angleFromCentre + (90 * (Mathf.PI / 180))), 0, Mathf.Cos(angleFromCentre + (90 * (Mathf.PI / 180))));
        Vector3 A = new Vector3(road.pointA.x - addX, road.pointA.y, road.pointA.z - addZ);
        Vector3 B = new Vector3(road.pointA.x + addX, road.pointA.y, road.pointA.z + addZ);
        Vector3 C = new Vector3(road.pointB.x - addX, road.pointB.y, road.pointB.z - addZ);
        Vector3 D = new Vector3(road.pointB.x + addX, road.pointB.y, road.pointB.z + addZ);
        Vector3 smallest = A;
        foreach (Vector3 i in new List<Vector3>() { A, B, C, D })
        {
            if (i.x < smallest.x | i.z < smallest.z)
            {
                smallest = i;
            }
        }
        return smallest;
    }

    private void OnApplicationQuit()
    {
        ResetTerrainPaint();
    }

    private GameObject[] GetObjectsOfTheme(string theme)
    {
        GameObject[] obsOfTheme = Resources.LoadAll<GameObject>($"Map Generation/{theme}");
/*        if (!CheckCompletePrefabSet(theme, obsOfTheme))
        { 
            throw new Exception("Not all the required prefabs are present");
        }*/
        return obsOfTheme;
    }

    public void PaintTerrain(Vector3 positionToPaint, Vector3 dir, bool reset=false)
    {
        Terrain terrain = mapTerrain;
        dir = new Vector3(MathF.Abs(dir.x), MathF.Abs(dir.y), MathF.Abs(dir.z));
        Vector3 tile = positionToPaint;

        Vector3 terrainPos = tile - terrain.transform.position;
        Vector3 mapPos = new Vector3(terrainPos.x / terrain.terrainData.size.x, 0, terrainPos.z / terrain.terrainData.size.z);
        float xCoord = mapPos.x * terrain.terrainData.alphamapWidth;
        float zCoord = mapPos.z * terrain.terrainData.alphamapHeight;
        int posX = (int)xCoord;
        int posZ = (int)zCoord;
        if (posX > 0 && posX < terrain.terrainData.alphamapWidth && posZ > 0 && posZ < terrain.terrainData.alphamapHeight)
        {
            //int c = Mathf.RoundToInt(curConfig.maxRoadWidth) * 2;
            int c = (int)curConfig.width;
            int b = (int)curConfig.length;
            int xMax = (int)(c * dir.z) * 2 + (int)(b * dir.x) + (int)(1 * dir.x);
            int yMax = (int)(c * dir.x) * 2 + (int)(b * dir.z) + (int)(1 * dir.z);
            float[,,] splatmapData = terrain.terrainData.GetAlphamaps(posX, posZ, xMax, yMax);
            for (int y = 0; y < xMax; y++)
            {
                for (int x = 0; x < yMax; x++)
                {
                    if (reset)
                    {
                        splatmapData[x, y, 0] = 1;
                        splatmapData[x, y, 1] = 0;
                    }
                    else
                    {
                        splatmapData[x, y, 0] = 0;
                        splatmapData[x, y, 1] = 1;
                    }
                }
            }
            terrain.terrainData.SetAlphamaps(posX, posZ, splatmapData);
        }
    }

    private Road GenerateRoad(Vector3 pointA, Vector3 pointB, Vector3 relativeDirection, bool blank=false)
    {
        Road tempRoad = new Road();
        Vector3 newDir = relativeDirection;
        Road prevRoad = null;
        if (allRoads.Count > 0)
        {
            prevRoad = allRoads[allRoads.Count - 1];
            prevRoad.pointer = tempRoad;
        }

        tempRoad.pointA = pointA;
        tempRoad.pointB = pointB;
        tempRoad.length = Vector3.Distance(pointA, pointB);
        tempRoad.width = curConfig.width;
        tempRoad.direction = (tempRoad.pointB - tempRoad.pointA).normalized;
        tempRoad.pointer = null;
        tempRoad.prev = prevRoad;
        tempRoad.road = SetRoadVectors(tempRoad);

        if (!blank && checkInTerrain(tempRoad))
        {
            //SetRoadObjects(tempRoad, "Building", obsOfTheme, false);
            PaintTerrain(tempRoad.road, tempRoad.direction);
            allRoads.Add(tempRoad);
        }
        else if (blank)
        {
            allRoads.Add(tempRoad);
        }
        else
        {
            tempRoad = null;
        }

        return tempRoad;
    }

    public Terrain GetCurrentTerrain()
    {
        return mapTerrain;
    }

    private bool checkInTerrain(Road road)
    {
        Terrain t = GameObject.FindFirstObjectByType<Terrain>();
        Bounds bounds = t.terrainData.bounds;
        Vector3 roadDir = (road.pointB - road.pointA).normalized;
        List<Vector3> allPoints = new List<Vector3>(){ road.pointA, road.pointB, road.pointA + (roadDir * road.width/2),
        road.pointA - (roadDir * road.width/2), road.pointB + (roadDir * road.width / 2), road.pointB - (roadDir * road.width / 2)};
        
        foreach (Vector3 v in allPoints)
        {
            if (!bounds.Contains(new Vector3(v.x, 0, v.z)))
            {
                return false;
            }
        }
        return true;
    }

    // still deciding how 'modular' this approach would be 
    private GameObject[] GetGameObjectOfType(string objectType)
    {
        if (obsOfTheme == null)
        {
            return null;
        }
        else
        {
            return null;
        }
    }

    // the lower uniformity, the more equal distance objects will be from each other
    // the lower density, the further away objects will be 
    private void SetRoadObjects(Road road, string objectTag, GameObject[] allPrefabs, bool generateWalls=false)
    {
        float density = 1 - curConfig.density;
        float uniformity = 1 - curConfig.uniformity;
        GameObject wall = null;
        List<GameObject> prefabsOfTag = new List<GameObject>();
        foreach (GameObject ob in allPrefabs)
        {
            if (ob.tag == objectTag)
            {
                prefabsOfTag.Add(ob);
            }
            else if (ob.tag == "Walls")
            {
                wall = ob;
            }
        }
        Vector3 direction = (road.pointB - road.pointA).normalized;
        float angleFromCentre = Vector3.Angle(Vector3.forward, direction) * (Mathf.PI / 180);
        float addX = road.width * Mathf.Sin(angleFromCentre + (90 * (Mathf.PI / 180)));
        float addY = road.width * Mathf.Cos(angleFromCentre + (90 * (Mathf.PI / 180)));
        Vector3 pointALeft = new Vector3(road.pointA.x + addX, road.pointA.y, road.pointA.z + addY);
        Vector3 pointARight = new Vector3(road.pointA.x - addX, road.pointA.y, road.pointA.z - addY);

        Vector3[] sides = new Vector3[2] {pointALeft, pointARight };
        foreach(Vector3 side in sides)
        {
            Vector3 relativeSideA = side;
            Vector3 relativeSideB = relativeSideA + (direction * road.length);
            Vector3 initPoint = relativeSideA; // set initial position to the beginning of the first road in the array
            float distanceBetweenObjects = (road.length * density);
            //Vector3 furthestBoundLeft = new Vector3(road.pointA.x, road.pointA.y, road.pointA.z + distanceBetweenObjects); // last point is the furthest point where there is an object placed on the road
            Vector3 furthestBoundLeft = relativeSideA + (distanceBetweenObjects * direction);
            if (density == 1 && generateWalls)
            {
                BuildWallSimple(wall, relativeSideA, relativeSideB);
            }
            else
            {
                int i = 0; // iterate to prevent accidental 'forever loop'
                GameObject lastOb = null;
                while (i < 5 && (relativeSideB - furthestBoundLeft).normalized == direction)
                {
                    RaycastHit furthestPointInfo;
                    Physics.Raycast(relativeSideB, -direction, out furthestPointInfo, maxDistance: road.length);
                    if (furthestPointInfo.point == Vector3.zero)
                    {
                        Vector3 point = relativeSideA;
                        furthestBoundLeft = point + (distanceBetweenObjects * direction);
                    }
                    else
                    {
                        Vector3 point = furthestPointInfo.point;
                        furthestBoundLeft = point + (distanceBetweenObjects * direction); ;
                        if ((relativeSideB - furthestBoundLeft).normalized != direction)
                        {
                            break;
                        }
                    }
                    // set new object and its position
                    GameObject newOb = prefabsOfTag[Random.Range(0, prefabsOfTag.Count)];
                    float newYPos = mapTerrain.SampleHeight(furthestBoundLeft);
                    newOb.transform.position = furthestBoundLeft;
                    Vector3 relDir = (side - road.pointA).normalized;
                    newOb.transform.rotation = Quaternion.LookRotation(relDir, Vector3.up);
                    //newOb.transform.Rotate(new Vector3(0, 90, 0));
                    if (lastOb == null | (lastOb && lastOb.transform.position != newOb.transform.position))
                    {
                        road.buildings.Add(newOb);
                        CreateNewObject(newOb, newOb.transform.position, newOb.transform.rotation, "Building");
                        //Instantiate(newOb);
                        if (generateWalls)
                        {
                            BuildWall(wall, newOb, road);
                        }
                        lastOb = newOb;
                    }
                    i++;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        RoadDebug();
        //pointsDebug();
    }

    private void pointsDebug()
    {
        if (allPoints != null)
        {
            foreach (Vector3 point in allPoints)
            {
                Gizmos.DrawWireCube(new Vector3(point.x, 10, point.z), Vector3.one * 2);
            }
        }
    }

    private void RoadDebug()
    {
        if (allRoads != null)
        {
            foreach (Road r in allRoads)
            {
                Gizmos.DrawCube(r.pointA, Vector3.one);
                //Gizmos.DrawCube(r.centre, Vector3.one);
                Gizmos.DrawSphere(r.pointB, 1f);

                // x = r(cos(degrees°)), y = r(sin(degrees°)).
                Vector3 direction = (r.pointB - r.pointA).normalized;
                float angleFromCentre = Vector3.Angle(Vector3.forward, direction) * (Mathf.PI / 180);
                float addX = r.width * Mathf.Sin(angleFromCentre + (90 * (Mathf.PI / 180)));
                float addY = r.width * Mathf.Cos(angleFromCentre + (90 * (Mathf.PI / 180)));
                Vector3 pointALeft = new Vector3(r.pointA.x + addX, r.pointA.y, r.pointA.z + addY);
                Vector3 pointARight = new Vector3(r.pointA.x - addX, r.pointA.y, r.pointA.z - addY);
                Gizmos.DrawWireCube(pointALeft, Vector3.one * 2);
                Gizmos.DrawWireCube(pointARight, Vector3.one * 2);
            }
        }
    }
    
    private bool CheckCompletePrefabSet(string theme, GameObject[] prefabsOfTheme)
    {
        Dictionary<string, int> requiredPrefabs = new Dictionary<string, int>() {
            {"Building", 0 },
            {"Walls", 0 } 
        };

        foreach(GameObject ob in prefabsOfTheme)
        {
            if (requiredPrefabs.ContainsKey(ob.tag))
            {
                requiredPrefabs[ob.tag] += 1;
            }
        }

        foreach (KeyValuePair<string, int> pair in requiredPrefabs)
        {
            if (pair.Value == 0)
            {
                return false;
            }
        }

        return true;
    }

    // empty scene, including terrain if killTerrain = true
    private void KillMap(bool killTerrain = false)
    {
        ResetTerrainPaint();
        foreach(GameObject ob in allObjectsInMap)
        {
            Destroy(ob);
            allObjectsInMap.Remove(ob);
        }

        if (killTerrain)
        {
            Destroy(mapTerrain.gameObject);
            SetMapTerrain(null);
        }
    }

    // set terrain to first active terrain in scene, specified terrain, or nothing.
    private void SetMapTerrain(GameObject newTerrain=null)
    {
        GameObject[] allTerrains = GameObject.FindGameObjectsWithTag("Terrain");
        if (newTerrain == null && allTerrains.Length > 0)
        {
            mapTerrain = allTerrains[0].GetComponent<Terrain>();
            terrainCentre = new Vector3(mapTerrain.transform.position.x + (mapTerrain.terrainData.size.x / 2),
                mapTerrain.transform.position.y, mapTerrain.transform.position.z + (mapTerrain.terrainData.size.z / 2));
        }
        else if (newTerrain != null)
        {
            mapTerrain = newTerrain.GetComponent<Terrain>();
            terrainCentre = new Vector3(mapTerrain.transform.position.x + (mapTerrain.terrainData.size.x / 2),
                mapTerrain.transform.position.y, mapTerrain.transform.position.z + (mapTerrain.terrainData.size.z / 2));
        }
        else
        {
            mapTerrain = null;
        }
    }

    // Instantiate new GameObject, applying necessary extra steps for organisation and function
    public GameObject CreateNewObject(GameObject obj, Vector3 pos, Quaternion rot, string tagTemp)
    {
        if (tagTemp == null)
        {
            tagTemp = obj.tag;
        }
        GameObject tempObj = Instantiate(obj, pos, rot);
        tempObj.tag = tagTemp;
        allObjectsInMap.Add(tempObj);
        return tempObj;
    }


    private void BuildWallSimple(GameObject wallObject, Vector3 a, Vector3 b)
    {
        Vector3 leftBounds;
        Vector3 rightBounds;
        leftBounds = a;
        rightBounds = b;

        GameObject tempObj = CreateNewObject(wallObject, Vector3.zero, Quaternion.Euler(0, 0, 0), null);
        tempObj.transform.Translate(Vector3.up * mapTerrain.SampleHeight(tempObj.transform.position));

        // sqrt((x1 - x2)^2 + (z1 - z2)^2) = length of wall
        float tempX = Mathf.Sqrt(squareNum(rightBounds.x - leftBounds.x) + squareNum(rightBounds.z - leftBounds.z));

        tempObj.transform.localScale = new Vector3(tempX, tempObj.transform.localScale.y, tempObj.transform.localScale.z);

        Vector3 difference = (leftBounds - rightBounds) / 2;
        Vector3 newPos = new Vector3(rightBounds.x + difference.x, rightBounds.y, rightBounds.z + difference.z);
        tempObj.transform.position = newPos;
        Vector3 dir = leftBounds - rightBounds;
        var rot = Quaternion.LookRotation(dir, Vector3.up);
        tempObj.transform.rotation = rot;
        tempObj.transform.Rotate(0, 90, 0);
    }

    // instantiate wall between two points, setting wall scale and angle to fit exactly between points
    private void BuildWall(GameObject wallObject, GameObject ob, Road road)
    {
        // set left and right bound values
        Vector3 leftBounds;
        Vector3 rightBounds;
        RaycastHit check = new RaycastHit();
        Vector3 direction = (road.pointB - road.pointA).normalized;
        Physics.Raycast(ob.transform.position, -direction, out check, maxDistance:Vector3.Distance(road.pointA, road.pointB));
        if (isCollisionAChild(check.collider, ob) | (road.pointA - check.point).normalized == direction)
        {
            return;
        }
        else if (check.collider && check.collider.gameObject != ob.gameObject && check.collider.transform.rotation == ob.transform.rotation)
        {
            leftBounds = check.point;
        }
        else
        {
            leftBounds = road.pointA;
            //leftBounds = defaultPoint;
        }

        Physics.Raycast(leftBounds, direction, out check);
        if (check.collider && check.collider.transform.rotation == ob.transform.rotation)
        {
            rightBounds = check.point;
        }
        else
        {
            return;
        }

        GameObject tempObj = CreateNewObject(wallObject, Vector3.zero, Quaternion.Euler(0, 0, 0), null);
        tempObj.transform.Translate(Vector3.up * mapTerrain.SampleHeight(tempObj.transform.position));

        // sqrt((x1 - x2)^2 + (z1 - z2)^2) = length of wall
        float tempX = Mathf.Sqrt(squareNum(rightBounds.x - leftBounds.x) + squareNum(rightBounds.z - leftBounds.z));

        tempObj.transform.localScale = new Vector3(tempX, tempObj.transform.localScale.y, tempObj.transform.localScale.z);

        Vector3 difference = (leftBounds - rightBounds) / 2;
        Vector3 newPos = new Vector3(rightBounds.x + difference.x, rightBounds.y, rightBounds.z + difference.z);
        tempObj.transform.position = newPos;
        Vector3 dir = leftBounds - rightBounds;
        var rot = Quaternion.LookRotation(dir, Vector3.up);
        tempObj.transform.rotation = rot;
        tempObj.transform.Rotate(0, 90, 0);
    }

    // method to square a given number, returning num * num
    private float squareNum(float num)
    {
        return num * num;
    }

    private bool isCollisionAChild(Collider c, GameObject ob)
    {
        if (!c)
        {
            return true;
        }
        foreach(Transform t in ob.GetComponentInChildren<Transform>())
        {
            if (t.gameObject == c.gameObject)
            {
                return true;
            }
        }
        return false;
    }
}


/*while (allRoads.Count < maxRoads && i < maxRoads && spaces > 2)
        {
            temp = null;
            float tempWidth = maxRoadWidth;

            Vector3 newPointA = Vector3.one;
            Vector3 newPointB = Vector3.one;
            List<Vector3> directionOptions = new List<Vector3>() { Vector3.forward, Vector3.right, Vector3.left };
            while (temp == null && directionOptions.Count > 0)
            {
                newDir = directionOptions[Random.Range(0, directionOptions.Count)];
                directionOptions.Remove(newDir);
                Road prevRoad;
                if (allRoads.Count == 0)
                {
                    newPointA = curConfig.initPoint;
                    prevRoad = null;
                }
                else
                {
                    prevRoad = allRoads[allRoads.Count-1];
                    newPointA = prevRoad.pointB;
                }

                int times = 0;
                directionOptions = new List<Vector3>() { Vector3.forward, Vector3.right, Vector3.left , Vector3.down};
                
                if (prevRoad != null)
                {
                    directionOptions.Remove(-prevRoad.direction);
                }
                do
                {
                    newDir = directionOptions[Random.Range(0, directionOptions.Count)];
                    directionOptions.Remove(newDir);
                    nextIndex[0] = (int)(curIndex[0] + newDir.x);
                    nextIndex[1] = (int)(curIndex[1] + newDir.z);

                    times += 1;
                    if (directionOptions.Count == 0)
                    {
                        print("no direction");
                        curIndex = new int[2] { (int)Random.Range(0, unusedPoints2D.GetLength(0)), Random.Range(0, unusedPoints2D.GetLength(1)) };
                        directionOptions = new List<Vector3>() { Vector3.forward, Vector3.right, Vector3.left };
                    }
                }
                while (unusedPoints2D[nextIndex[0], nextIndex[1]] == Vector3.zero * 99 && times < 50);
                
                newPointB = unusedPoints2D[nextIndex[0], nextIndex[1]];
                prevIndex = curIndex;
                curIndex = nextIndex;
                unusedPoints2D[nextIndex[0], nextIndex[1]] = Vector3.zero * 99;
                temp = GenerateRoad(newPointA, newPointB, newDir);
                */
/*                if (temp == null && directionOptions.Count == 0)
                {
                    newPointA = unusedPoints[Random.Range(0, unusedPoints.Count)];
                    newPointB = newPointA + (newDir * maxRoadLength);
                    temp = GenerateRoad(newPointA, newPointB, newDir);
                }*/