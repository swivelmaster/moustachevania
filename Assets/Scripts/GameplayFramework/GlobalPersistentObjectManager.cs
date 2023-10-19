using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalPersistentObjectManager : MonoBehaviour
{
    public static GlobalPersistentObjectManager Instance { private set; get; }

    //[SerializeField]
    //private AudioManager audioManager = null;
    [SerializeField]
    private ControllerInputManager inputManager = null;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(this.gameObject);
    }

    public void GetGlobalManagers(
        out ControllerInputManager inputManager)
    {
        inputManager = this.inputManager;
    }
}
