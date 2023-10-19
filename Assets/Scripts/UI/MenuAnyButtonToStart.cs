using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class MenuAnyButtonToStart : MonoBehaviour
{
    ControllerInputManager inputManager;

    public AudioSource SoundToPlay;
    public GameObject[] ObjectsToActivate;
    public GameObject[] ObjectsToDeActivate;
    public DOTweenAnimation[] TweensToKill;

    private void Start()
    {
        GlobalPersistentObjectManager.Instance.GetGlobalManagers(out inputManager);

        foreach (var go in ObjectsToActivate)
            go.SetActive(false);
    }

    void Update()
    {
        if (inputManager.GetCurrentInput(true).AnyButtonDown())
        {
            SoundToPlay.Play();

            foreach (var tween in TweensToKill)
                tween.DOKill(true);

            foreach (var go in ObjectsToActivate)
                go.SetActive(true);

            foreach (var go in ObjectsToDeActivate)
                go.SetActive(false);

            Destroy(this);
        }
        
    }
}
