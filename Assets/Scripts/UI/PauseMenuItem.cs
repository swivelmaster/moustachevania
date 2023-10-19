using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuItem : MonoBehaviour
{
    public enum PauseMenuActions
    {
        Continue,
        RestartDemo,
        QuitToMenu,
        QuitToDesktop,
        CameraModeEnable,
        GrantAllAbilities,
        ShowControlOptions,
    }

    public Image SelectedImage;

    public Animator teacupAnimator;

    public PauseMenuActions MyAction;
    
    void Start()
    {
        SelectedImage.enabled = false;
    }

    public void SetSelected(bool selected)
    {
        SelectedImage.enabled = selected;
    }

    private void OnEnable()
    {
        //AnimatorClipInfo[] info = teacupAnimator.GetCurrentAnimatorClipInfo(0);
    }
}
