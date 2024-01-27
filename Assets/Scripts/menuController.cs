using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MenuInput
{
    public int houseNum;
    public int treeNum;
    public int detailsNum;
    public int roadSize;
}

public class menuController : MonoBehaviour
{
    public TMP_InputField fbxInputFileName;
    private MenuInput menuInput;
    private GameObject[] menuArr;
    private bool menuStatus;
    GameObject[] allCams;

    public GameObject currentMenu;
    public Button new_button;
    public Button load_button;
    public Button save_button;
    public Dropdown saveOptions;

    public TMP_InputField houseNum;
    public TMP_InputField treeNum;
    public TMP_InputField extrasNum;
    public TMP_InputField roadSize;

    void Start()
    {
        if (PlayerPrefs.HasKey("lastFbxFile"))
        {
            fbxInputFileName.text = PlayerPrefs.GetString("lastFbxFile");
        }
        else
        {
            PlayerPrefs.SetString("lastFbxFile", "default.fbx");
        }

        currentMenu = null;
        menuArr = GameObject.FindGameObjectsWithTag("Menu");
        endAllMenus();
        setupMenuDefaults();
        setMenu("WelcomeCanvas");
    }

    public void setPlayerPrefs()
    {
        PlayerPrefs.SetInt("houseNum", Int32.Parse(houseNum.text));
        PlayerPrefs.SetInt("treeNum", Int32.Parse(treeNum.text));
        PlayerPrefs.SetInt("extrasNum", Int32.Parse(extrasNum.text));
        PlayerPrefs.SetInt("roadSize", Int32.Parse(roadSize.text));
        PlayerPrefs.Save();
    }

    private void setupPlayerPrefs()
    {
        PlayerPrefs.SetInt("houseNum", 0);
        PlayerPrefs.SetInt("treeNum", 0);
        PlayerPrefs.SetInt("extrasNum", 0);
        PlayerPrefs.SetInt("roadSize", 0);
        PlayerPrefs.Save();
    }

    public MenuInput getMenuInput()
    {
        return menuInput;
    }


    private void setupMenuDefaults()
    {
        saveOptions.AddOptions(new List<string>() { "Save 1", "Save 2", "Save 3" });
        saveOptions.value = 0;

        if (PlayerPrefs.HasKey("houseNum"))
        {
            int[] prefs = new int[] { PlayerPrefs.GetInt("houseNum"), PlayerPrefs.GetInt("treeNum"),
                PlayerPrefs.GetInt("extrasNum"), PlayerPrefs.GetInt("roadSize")};
            houseNum.text = prefs[0].ToString();
            treeNum.text = prefs[1].ToString();
            extrasNum.text = prefs[2].ToString();
            roadSize.text = prefs[3].ToString();
        }
        else
        {
            setupPlayerPrefs();
        }
    }

    public void altMenuStatus()
    {
        menuStatus = !menuStatus;
    }

    public void setMenuStatus(bool status)
    {
        menuStatus = status;
    }

    public void endAllMenus()
    {
        foreach (GameObject menu in menuArr)
        {
            menu.SetActive(false);
        }
    }

    public void setMenu(string menuChoice)
    {
        if (!menuStatus)
        {
            altMenuStatus();
        }

        foreach(GameObject ob in menuArr)
        {
            if (ob.name == menuChoice)
            {
                if (currentMenu != null)
                {
                    currentMenu.SetActive(false);
                }
                currentMenu = ob;
                currentMenu.SetActive(true);
            }
        }
    }


    public bool menuExists(string menuChoice)
    {
        foreach(GameObject ob in menuArr)
        {
            if (ob.name == menuChoice)
            {
                return true;
            }
        }
        return false;
    }
}
