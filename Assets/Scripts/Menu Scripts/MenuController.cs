using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// menu part 2
public class MenuController : MonoBehaviour
{
    public List<GameObject> allMenuCanvases;
    public GameObject curMenu;
    public Camera playerCamera;
    public Camera editorCamera;
    public Camera menuCamera;
    private Dictionary<string, Camera> cameraDict;

    public MapGeneration mapGen;
    private string curCam;

    public TMP_InputField widthInputValue;
    public TMP_InputField heightInputValue;
    public TMP_InputField mapNameValue;
    public Slider densityValueInput;
    public Dropdown dropdownValue;


    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        cameraDict = new Dictionary<string, Camera>
        {
            {"EditorMenu", editorCamera },
            {"PlayerMenu", playerCamera },
            {"SettingsMenu", menuCamera },
            {"WelcomeMenu", menuCamera },
            {"NewMapMenu", menuCamera },
        };
        allMenuCanvases = new List<GameObject>();
        UpdateMenuCanvases();
        SetCurMenu(menuName:"WelcomeMenu");
    }

    public void NewMap()
    {
        MapConfig m = GetNewMapStats();
        mapGen.NewMap(m);
    }

    public MapConfig GetNewMapStats()
    {
        GameObject menu = GetCanvas("NewMapMenu");
        /*Vector2 terrainSize = new Vector2(Int32.Parse(GameObject.Find("WidthInput").GetComponent<TMP_InputField>().text),
            Int32.Parse(GameObject.Find("HeightInput").GetComponent<TMP_InputField>().text));
        float densityValue = GameObject.Find("DensityBar").GetComponent<Slider>().normalizedValue;
        Dropdown d = GameObject.Find("ThemeDropdown").GetComponent<Dropdown>();
        string themeValue = d.itemText.text;
        string mapName = GameObject.Find("MapNameInput").GetComponent<TMP_InputField>().text;*/

        Vector2 terrainSize = new Vector2(Int32.Parse(widthInputValue.text), Int32.Parse(heightInputValue.text));
        float densityValue = densityValueInput.normalizedValue;
        string themeValue = dropdownValue.itemText.text;
        string mapName = mapNameValue.text;

        MapConfig config = new MapConfig();
        //config.terrain.terrainData.size = new Vector3(terrainSize.x, 1, terrainSize.y);
        config.density = densityValue;
        config.theme = themeValue;
        config.mapName = mapName;

        return config;
    }

    private void Update()
    {
        curCam = ((Camera)FindFirstObjectByType(typeof(Camera))).gameObject.name;
        if (curCam != "MenuCamera" && curCam != "EditModeCamera")
        {
            HideMenus();
        }
    }

    private void setupPlayerPrefs()
    {
        PlayerPrefs.SetInt("houseNum", 0);
        PlayerPrefs.SetInt("treeNum", 0);
        PlayerPrefs.SetInt("extrasNum", 0);
        PlayerPrefs.SetInt("roadSize", 0);
        PlayerPrefs.Save();
    }

    private void UpdateMenuCanvases()
    {
        allMenuCanvases.Clear();
        GameObject[] allCans = GameObject.FindGameObjectsWithTag("Menu");
        foreach (GameObject c in allCans)
        {
            allMenuCanvases.Add(c);
        }
    }

    public void HideMenus()
    {
        foreach(GameObject m in allMenuCanvases)
        {
            m.SetActive(false);
        }
    }

    public void ShowCurMenu()
    {
        HideMenus();
        curMenu.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public GameObject GetCanvas(string menuName=null)
    {
        GameObject menu = null;
        foreach (GameObject c in allMenuCanvases)
        {
            if (c.name == "NewMapMenu")
            {
                menu = c;
            }
        }

        return menu;
    }

    public void SetCurMenu(string menuName=null)
    {
        if (menuName != null)
        {
            foreach(GameObject m in allMenuCanvases)
            {
                if (m.name == menuName && curMenu == null)
                {
                    curMenu = m;
                }
                else if (m.name == menuName)
                {
                    curMenu.gameObject.SetActive(false);
                    curMenu = m;
                    curMenu.gameObject.SetActive(true);
                    SetCamera(curMenu.name);
                }
            }
        }

        if (curMenu == null | menuName == null)
        {
            curMenu = null;
        }
        ShowCurMenu();
/*        else if (menu != null && allMenuCanvases.Contains(menu))
        {
            curMenu = menu;
        }*/
    }

    // really shit function - make something better at some point
    private void SetCamera(string menuName)
    {
        if (menuName == "EditorMenu")
        {
            menuCamera.gameObject.SetActive(false);
            editorCamera.gameObject.SetActive(true);
        }
        else
        {
            menuCamera.gameObject.SetActive(true);
            editorCamera.gameObject.SetActive(false);
        }
    }
}
