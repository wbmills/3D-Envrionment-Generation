using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public class playerMovement : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject player;
    public GameObject[] players;
    public GameObject[] cams;
    public GameObject menuObject;
    public float speed = 10f;
    public float mouseSensitivity = 3f;
    private int curPlayerObject = 0;
    private Dictionary<KeyCode, string> buttons;
    public bool isMenuActive = false;
    private bool freezePosition = false;
    public float scrollSensitivity;

    public float rotMult = 4f;

    float yaw = 0f;
    float pitch = 0f;

    public float maxY = -65;
    public float minY = 50;

    void Start()
    {
        scrollSensitivity = 80f;
        freezePosition = false;
        menuObject = GameObject.Find("Menu");
        menuObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        buttons = new Dictionary<KeyCode, string>(){
            {KeyCode.Q, "changePlayer"},
            {KeyCode.Space, "jump" },
            {KeyCode.Equals, "zoomIn" },
            {KeyCode.Minus, "zoomOut" },
        };

        players = GameObject.FindGameObjectsWithTag("Player");
        cams = GameObject.FindGameObjectsWithTag("MainCamera");

        foreach(GameObject cam in cams)
        {
            cam.SetActive(false);
        }

        curPlayerObject = 1;
        player = players[curPlayerObject];
        cams[curPlayerObject].SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        menuObject.SetActive(isMenuActive);
        if (player.gameObject.name == "MenuPlayer")
        {
            setLock();
            isMenuActive = true;
        }
        else if (player.gameObject.name == "Edit Mode")
        {
            setLock();
            isMenuActive = false;
            player.transform.Translate(Input.GetAxis("Horizontal") * Vector3.right);
            player.transform.Translate(Input.GetAxis("Vertical") * Vector3.up);
            player.transform.Translate(Input.GetAxis("Mouse ScrollWheel") * Vector3.forward * scrollSensitivity);
        }
        else
        {
            freezePosition = false;
            isMenuActive = false;
            Cursor.lockState = CursorLockMode.Locked;
            isMenuActive = false;
            Cursor.visible = false;
        }

        if (!freezePosition)
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
        }

        foreach (var buttonMethodDict in buttons)
        {
            if (Input.GetKeyDown(buttonMethodDict.Key))
            {
                MethodInfo mi = this.GetType().GetMethod(buttonMethodDict.Value, BindingFlags.NonPublic | BindingFlags.Instance);
                mi.Invoke(this, null);
            }
        }
    }

    private void zoomIn()
    {
        if (player.gameObject.name == "Edit Mode")
        {
            player.transform.Translate(Vector3.forward * 10);
        }
    }

    private void zoomOut()
    {
        if (player.gameObject.name == "Edit Mode")
        {
            player.transform.Translate(-Vector3.forward * 10);
        }
    }

    private void setLock()
    {
        freezePosition = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void invokeChangePlayer()
    {
        changePlayer();
    }
    private void changePlayer()
    {
        EventSystem.current.SetSelectedGameObject(null);
            cams[curPlayerObject].SetActive(false);

            if (curPlayerObject == cams.Length - 1)
            {
                curPlayerObject = 0;
            }
            else
            {
                curPlayerObject++;
            }
            player = players[curPlayerObject];
            cams[curPlayerObject].SetActive(true);
    }

    private void jump()
    {
        if (!isMenuActive)
        {
            player.GetComponent<Rigidbody>().AddForce(Vector3.up * 10, ForceMode.Impulse);
        }
    }

    public GameObject getCurrentPlayer()
    {
        return player;
    }
}
