using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class isCollidingScript : MonoBehaviour
{
    private GameObject EditMode;
    private editModeController emcScript;
    public Terrain t;
    public GameObject cur;
    private void Start()
    {
        Physics.IgnoreCollision(GetComponent<BoxCollider>(), t.GetComponent<TerrainCollider>(), true);
        EditMode = GameObject.Find("EditModeController");
        emcScript = EditMode.GetComponent<editModeController>();
    }
    private void OnTriggerEnter(Collider other)
    {
        emcScript.setCurrentCollisionObject(other.gameObject);
        cur = other.gameObject;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == cur)
        {
            emcScript.setCurrentCollisionObject(null);
        }
    }
}
