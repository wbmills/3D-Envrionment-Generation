using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerController : MonoBehaviour
{
    public GameObject player;
    public bool recordPosition = true;
    public bool canMove;
    public float speed;
    public float recordPositionAccuracy = 2f;
    private float rotMult = 4f;
    private float yaw = 0f;
    private float pitch = 0f;
    private float maxY = -65;
    private float minY = 50;
    private float sprintSpeed;
    private float baseSpeed;


    void Start()
    {
        baseSpeed = 10f;
        sprintSpeed = baseSpeed * 2f;
        speed = baseSpeed;
        player = GameObject.FindGameObjectWithTag("Player");
        if (GameObject.FindGameObjectsWithTag("MainCamera").Length == 1)
        {
            canMove = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            StartCoroutine("UpdatePosition");
        }
    }

    private void MoveAI(Vector3 x) { 
        
    }


    // Update is called once per frame
    void Update()
    {
        if (canMove)
        {
            // okay so sort out camera looking because you stole this from the internet somewhere 
            yaw += rotMult * Input.GetAxis("Mouse X");
            pitch -= rotMult * Input.GetAxis("Mouse Y");
            pitch = Mathf.Clamp(pitch, maxY, minY); // Clamp viewing up and down

            player.transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);

            float xa = Input.GetAxisRaw("Horizontal");
            float ya = Input.GetAxisRaw("Vertical");

            float t = Time.deltaTime;
            //player.GetComponent<Rigidbody>().transform.Translate(new Vector3(xa * speed * t, 0, ya * speed * t));
            player.transform.Translate(new Vector3(xa * speed * t, 0, ya * speed * t));

            if (Input.GetKeyDown(KeyCode.Space))
            {
                jump();
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                speed = sprintSpeed;
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                speed = baseSpeed;
            }
        }
    }

    private IEnumerator UpdatePosition()
    {
        while (true && recordPosition)
        {
            print(player.transform.position);
            yield return new WaitForSeconds(recordPositionAccuracy);
        }
    }
    public void setActive(bool setting)
    {
        player.gameObject.SetActive(setting);
    }

    private void jump()
    {
        player.GetComponent<Rigidbody>().AddForce(Vector3.up * 10, ForceMode.Impulse);
    }

    public GameObject getCurrentPlayer()
    {
        return player;
    }

    public void setCanMove(bool setting)
    {
        canMove = setting;
    }
}
