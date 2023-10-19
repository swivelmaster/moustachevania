using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseScreenController : MonoBehaviour
{
    public PauseMenuItem[] PauseMenuItems;
    public GameObject PauseMenu;

    void Init()
    {
        PauseMenu.SetActive(false);
    }

    public int CurrentSelectedItem { private set; get; }

    public void ShowPauseMenu()
    {
        // Reset selected item when showing the pause menu
        CurrentSelectedItem = 0;
        PauseMenu.SetActive(true);

        // Not entirely sure why this is necessary
        StartCoroutine(AndThenUpdateSelection());
    }

    IEnumerator AndThenUpdateSelection()
    {
        yield return new WaitForEndOfFrame();
        UpdateSelection();
    }

    void UpdateSelection()
    {
        for (int i = 0; i < PauseMenuItems.Length; i++)
        {
            PauseMenuItems[i].SetSelected(i == CurrentSelectedItem);
        }
    }

    public void HidePauseMenu()
    {
        PauseMenu.SetActive(false);
    }

    public void IncrementSelection()
    {
        CurrentSelectedItem++;
        if (CurrentSelectedItem >= PauseMenuItems.Length)
            CurrentSelectedItem = 0;

        UpdateSelection();
    }

    public void DecrementSelection()
    {
        CurrentSelectedItem--;
        if (CurrentSelectedItem < 0)
            CurrentSelectedItem = PauseMenuItems.Length - 1;

        UpdateSelection();
    }
}
