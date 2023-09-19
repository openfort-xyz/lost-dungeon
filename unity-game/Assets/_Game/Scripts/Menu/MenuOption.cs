using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class MenuOption : MonoBehaviour
{
    [Header("UI elements")]
    [SerializeField] GameObject selector;
    [SerializeField] Text selector_text, main_text;
    [SerializeField] Color slectore_color;
    public UnityEvent OnSelected;

    public bool isSelected { get; private set; }
    [HideInInspector] public MenuOption nextOption { get; private set; }
    [HideInInspector] public MenuOption prevOption { get; private set; }

    public void initOption(MenuOption prev, MenuOption next)
    {
        prevOption = prev;
        nextOption = next;
    }

    public void Select()
    {
        isSelected = true;
        selector.SetActive(true);
        selector_text.color = slectore_color;
        main_text.color = slectore_color;
    }

    public void DeSelect()
    {
        isSelected = false;
        selector.SetActive(false);
        selector_text.color = Color.white;
        main_text.color = Color.white;
    }
}
