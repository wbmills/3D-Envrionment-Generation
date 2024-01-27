using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class setPositionOfFloor : MonoBehaviour
{
    Terrain terrain;

    void Start()
    {
        terrain = GameObject.FindAnyObjectByType<Terrain>();
        transform.position = new Vector3(terrain.transform.position.x, .003f, terrain.transform.position.z);
        // set y pos
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.tag == "Wall")
        {
            Destroy(collision.collider.gameObject);
        }
    }
}
