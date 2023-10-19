using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SingleCutsceneObject : MonoBehaviour
{
    public const string CHAUNCEY_NOTE = "CHAUNCEY_NOTE";
    public const string INVENTORY_PICKUP = "INVENTORY_PICKUP";

    [SerializeField]
    private GameObject ChaunceyNote = null;
    [SerializeField]
    private GameObject InventoryPickup = null;

    Dictionary<string, GameObject> ObjectNameIndex;
    Dictionary<string, List<Tween>> ObjectNameTweens;

    [Header("Specific Elements")]
    [SerializeField]
    private TMP_Text InventoryPickupDescription = null;
    [SerializeField]
    private Image InventoryPickupImage = null;

    public enum State { Inactive, TransitionIn, Waiting, TransitionOut }
    public State CurrentState { private set; get; }

    string CurrentObjectName;
    Action onComplete;

    private void Start()
    {
        CurrentState = State.Inactive;

        ObjectNameIndex = new Dictionary<string, GameObject>();
        ObjectNameTweens = new Dictionary<string, List<Tween>>();

        ObjectNameIndex[CHAUNCEY_NOTE] = ChaunceyNote;
        ObjectNameIndex[INVENTORY_PICKUP] = InventoryPickup;

        foreach (var kv in ObjectNameIndex)
            kv.Value.SetActive(false);            
    }

    public void Begin(string ObjectName, Action onComplete, string additionalArgument)
    {
        switch (ObjectName)
        {
            case INVENTORY_PICKUP:
                int id = int.Parse(additionalArgument);
                var item = InventoryManager.Instance.GetInventoryItemDataById(id);
                InventoryPickupDescription.text = item.description;
                InventoryPickupImage.sprite = item.sprite;
                break;
        }

        this.Begin(ObjectName, onComplete);
    }

    public void Begin(string ObjectName, Action onComplete)
    {
        if (!ObjectNameIndex.ContainsKey(ObjectName))
            throw new Exception(
                "Error: Tried to start SingleCutsceneObject scene for objectName "
                + ObjectName + " that doesn't have one.");

        CurrentState = State.TransitionIn;
        this.onComplete = onComplete;
        CurrentObjectName = ObjectName;

        ObjectNameIndex[ObjectName].SetActive(true);

        var tweenComponent = ObjectNameIndex[ObjectName].GetComponent<DOTweenAnimation>();
        tweenComponent.DORestart();
        
        ObjectNameTweens[ObjectName] =
            GameUtils.ConfigureTweensForDialog(tweenComponent.GetTweens());

        foreach (Tween tween in ObjectNameTweens[ObjectName])
            tween.Play();

        ObjectNameTweens[ObjectName][0].onComplete = OnTransitionInComplete;
    }

    void OnTransitionInComplete()
    {
        CurrentState = State.Waiting;
    }

    public void AdvanceFrame(bool shouldContinue)
    {
        if (CurrentState != State.Waiting)
            return;

        if (shouldContinue)
        {
            BeginTransitionOut();
        }
    }

    void BeginTransitionOut()
    {
        foreach (Tween tween in ObjectNameTweens[CurrentObjectName])
            tween.SmoothRewind();

        ObjectNameTweens[CurrentObjectName][0].onRewind = OnTransitionOutComplete;
    }

    void OnTransitionOutComplete()
    {
        CurrentState = State.Inactive;
        onComplete.Invoke();
    }

}
