using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuOptions : MonoBehaviour
{
    [SerializeField] MenuOption[] menuOptions;
    
    private void Start()
    {
        InitOptions();
        DeactivateAll();
        menuOptions[0].Select();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            ShiftSelection(false);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            ShiftSelection(true);
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SelectOption();
        }
    }


    void InitOptions()
    {
        for (int i = 0; i < menuOptions.Length; i++)
        {
            MenuOption option = menuOptions[i];
            if (i == 0)
            {
                option.initOption(menuOptions[menuOptions.Length - 1], menuOptions[1]);
            }
            else if (i == menuOptions.Length - 1)
            {
                option.initOption(menuOptions[i - 1], menuOptions[0]);
            }
            else
            {
                option.initOption(menuOptions[i - 1], menuOptions[i + 1]);
            }
        }
    }

    void DeactivateAll()
    {
        foreach (var menuOption in menuOptions)
        {
            menuOption.DeSelect();
        }
    }

    void ShiftSelection(bool Up)
    {
        foreach (var menuOption in menuOptions)
        {
            if (menuOption.isSelected)
            {
                // play shift sound
                DeactivateAll();
                if (Up)
                {
                    menuOption.prevOption.Select();
                }
                else
                {
                    menuOption.nextOption.Select();
                }
                break;
            }
        }
    }

    void SelectOption()
    {
        foreach (var option in menuOptions)
        {
            if (option.isSelected)
            {
                option.OnSelected?.Invoke();
                break;
            }
        }
    }

}
