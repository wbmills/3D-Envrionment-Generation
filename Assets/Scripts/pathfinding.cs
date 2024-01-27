using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector3 pos;
    public Node parent;
    public float distanceFromStart = 0;
    public float distanceFromGoal = 0;
    public float totalDistance = 0;
    public bool alive = true;
}

public class pathfinding : MonoBehaviour
{
    public float step = 5f;
    public float maxDistanceUp = 3;
    public List<Node> nodes;
    public Node curNode;
    public Vector3 start;
    public Vector3 goal;
    public bool debug = false;
    public Node kingNode;
    private List<Vector3> dirs;
    public GameObject debugObject;
    private List<Node> usableNodes;
    private int iterations;

    // Start is called before the first frame update
    void Start()
    {
        ResetSearch();
    }

    public void ResetSearch()
    {
        dirs = new List<Vector3>() {
            Vector3.right, Vector3.left,
            Vector3.forward, Vector3.back,
            Vector3.up, Vector3.down,
        };

        kingNode = null;
        curNode = new Node();
        curNode.pos = start;
        curNode.parent = null;
        curNode.distanceFromGoal = Vector3.Distance(curNode.pos, goal);
        curNode.distanceFromStart = 0;
        nodes = new List<Node>() { curNode };
        usableNodes = new List<Node>() { curNode };
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && debug)
        {
            StartCoroutine("SearchLog");
            List<Vector3> f = Search();
            if (f != null)
            {
                foreach (Vector3 pos in f)
                {
                    Instantiate(debugObject, pos, Quaternion.identity);
                }
            }
            else
            {
                print("Failed.");
            }
            print($"i: {iterations}\nNodes: {nodes.Count}\nUsable Nodes: {usableNodes.Count}");
            ResetSearch();
        }
    }

    public IEnumerator SearchLog()
    {
        print("Processing...");
        return null;
    }

    public List<Vector3> Search()
    {
        iterations = 0;
        List<Vector3> final = new List<Vector3>();
        while (iterations < 1000000 && nodes.Count < 50000 && kingNode == null && usableNodes.Count > 0)
        {
            curNode = GetShortestPath();
            if (AddNeighbors() == 0)
            {
                curNode.alive = false;
                usableNodes.Remove(curNode);
            }

            if (curNode.distanceFromGoal <= 2)
            {
                kingNode = curNode;
                while (kingNode.parent != null)
                {
                    final.Add(kingNode.pos);
                    kingNode = kingNode.parent;
                }
                print("done");
                return final;
            }
            iterations++;
        }
        return null;
    }

    // returns node with shortest total path from A to B
    private Node GetShortestPath()
    {
        Node min = null;
        foreach(Node cur in usableNodes)
        {
            if ((min == null && cur.alive) | (min != null && cur.totalDistance <= min.totalDistance && cur.alive))
            {
                min = cur;
            }
        }
        return min;
    }

    private List<Vector3> possibleNeighbors(Node n)
    {
        List<Vector3> possiblePositions = new List<Vector3>();
        Ray r;
        bool inArr = false;
        Physics.Raycast(n.pos, Vector3.down, out RaycastHit distanceConstraintRayhit, 10000);
        
        foreach (Vector3 dir in dirs)
        {
            inArr = false;

            r = new Ray(origin: n.pos, direction: dir);
            Physics.Raycast(origin: n.pos, direction: dir, out RaycastHit hit, step);
            Vector3 tempdir = new Vector3(dir.x, dir.y + Terrain.activeTerrain.terrainData.GetHeight(Mathf.RoundToInt(r.GetPoint(step).x), Mathf.RoundToInt(r.GetPoint(step).y)), dir.z);
            r.direction = tempdir;
            if ((distanceConstraintRayhit.collider != null && distanceConstraintRayhit.distance > maxDistanceUp))
            {
                inArr = true;
            }
            else if (hit.collider != null && hit.collider.tag != "Terrain")
            {
                inArr = true;
            }
            else
            {
                foreach (Node node in nodes)
                {
                    if (node.pos == r.GetPoint(step))
                    {
                        inArr = true;
                        break;
                    }
                }
            }

            if (!inArr)
            {
                possiblePositions.Add(r.GetPoint(step));
            }
        }

        return possiblePositions; 
    }

    public int AddNeighbors()
    {
        List<Vector3> neighbors = possibleNeighbors(curNode);
        foreach(Vector3 pos in neighbors)
        {
            Node newNode = new Node();
            newNode.parent = curNode;
            newNode.pos = new Vector3(
                pos.x, 
                pos.y, 
                pos.z);
            //Instantiate(point, newNode.pos, Quaternion.identity);
            newNode.distanceFromStart = curNode.distanceFromStart + step;
            newNode.distanceFromGoal = Vector3.Distance(newNode.pos, goal);
            newNode.totalDistance = newNode.distanceFromGoal + newNode.distanceFromStart;
            nodes.Add(newNode);
            usableNodes.Add(newNode);
        }
        return neighbors.Count;
    }
}
