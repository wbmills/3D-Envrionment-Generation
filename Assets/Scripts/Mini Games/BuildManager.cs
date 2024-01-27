using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public GameObject demoObj;
    public int objsInScene = 10;
    public int variation = 3;
    public float maxReachingDistance = 5;
    public GameObject player;
    public int throwForce = 50;

    private Vector3 initPos;

    private List<GameObject> allObjs;
    private GameObject curObject;
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        initPos = player.transform.position;
        allObjs = new List<GameObject>();
    }

    public void ResetObjects()
    {
        foreach(GameObject ob in allObjs)
        {
            Destroy(ob);
        }
        player.transform.position = initPos;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.DrawRay(player.transform.position, player.transform.forward * maxReachingDistance);
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Setup();
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ResetObjects();
        }

        if (Input.GetKeyDown(KeyCode.Mouse0) && curObject == null)
        {
            Pickup();
        }
        else if (Input.GetKeyDown(KeyCode.Mouse0) && curObject != null)
        {
            Drop();
        }
        else if (Input.GetKeyDown(KeyCode.Mouse1) && curObject == null)
        {
            Punch();
        }
        else if (Input.GetKeyDown(KeyCode.Mouse1) && curObject != null)
        {
            Throw();
        }
    }

    // not working yet
    private void SetObjectLock()
    {
        Rigidbody r = demoObj.GetComponent<Rigidbody>();
        r.constraints = RigidbodyConstraints.FreezeRotationX;
        r.constraints = RigidbodyConstraints.FreezeRotationY;
        r.constraints = RigidbodyConstraints.FreezeRotationZ;
    }

    private void Throw()
    {
        Rigidbody r = curObject.GetComponent<Rigidbody>();
        Drop();
        r.AddForce(player.transform.forward * throwForce, ForceMode.Impulse);
    }
    private void Pickup()
    {
        Physics.Raycast(player.transform.position, player.transform.forward, out RaycastHit hit, maxReachingDistance);
        if (hit.collider && hit.collider.tag == "pickup")
        {
            curObject = hit.collider.gameObject;
            curObject.transform.SetParent(player.transform);
            curObject.GetComponent<Rigidbody>().useGravity = false;
            curObject.GetComponent<Collider>().enabled = false;
            curObject.transform.localPosition = Vector3.forward * 3;
            curObject = curObject.gameObject;
        }
    }

    private void Drop()
    {
        curObject.transform.SetParent(null);
        curObject.GetComponent<Rigidbody>().useGravity = true;
        curObject.GetComponent<Collider>().enabled = true;
        curObject = null;
    }

    private void Punch()
    {
        Physics.Raycast(player.transform.position, player.transform.forward, out RaycastHit hit, 30);
        if (hit.collider && hit.collider.tag == "pickup")
        {
            hit.collider.GetComponent<Rigidbody>().AddForce(player.transform.forward * throwForce, ForceMode.Impulse);
        }
    }

    private void KillObject()
    {
        Physics.Raycast(player.transform.position, player.transform.forward, out RaycastHit hit, maxReachingDistance);
        if (hit.collider && hit.collider.tag == "pickup")
        {
            Destroy(hit.collider.gameObject);
        }
    }

    private void Setup()
    {
        for (int i = 0; i < objsInScene; i++)
        {
            NewObject(demoObj, transform.position);
        }
    }

    private GameObject NewObject(GameObject obj, Vector3 pos)
    {
        pos = new Vector3(
            pos.x + Random.Range(-variation, variation), 
            pos.y + Random.Range(-variation, variation), 
            pos.z + Random.Range(-variation, variation));
        GameObject temp = Instantiate(obj, pos, Quaternion.identity);
        allObjs.Add(temp);
        return temp;
    }
}
