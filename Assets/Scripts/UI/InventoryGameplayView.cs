using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryGameplayView : MonoBehaviour
{
    [SerializeField]
    private GameObject LeftTargetView = null;
    [SerializeField]
    private GameObject RightTargetView = null;

    InventoryItem LeftTarget;
    InventoryItem RightTarget;
    List<InventoryItem> Modifiers;

    InventoryItem SwapTarget;

    [SerializeField]
    private InventoryTargetSelectView LeftView = null;
    [SerializeField]
    private InventoryTargetSelectView RightView = null;
    [SerializeField]
    private InventoryTargetSwapDisplay SwapView = null;

    [SerializeField]
    private InventoryTargetSelectCloseHud LeftHudView = null;

    [SerializeField]
    private InventoryTargetSelectCloseHud RightHudView = null;

    private void Awake()
    {
        Modifiers = new List<InventoryItem>();
    }

    private void Start()
    {
        InventoryManager.Instance.onCharmsUpdated.AddListener(UpdateView);
        InventoryManager.Instance.onSwapTargetUpdatedOnly.AddListener(UpdateSwapTargetOnly);
    }

    void InitializeValues()
    {
        LeftTarget = null;
        RightTarget = null;
        Modifiers.Clear();
        SwapTarget = null;
        SwapView.SetEnabled(false);
    }

    public void UpdateView(CharmChange charmChange)
    {
        InitializeValues();

        LeftTarget = InventoryManager.Instance.CharmSlots[0].Target;
        RightTarget = InventoryManager.Instance.CharmSlots[1].Target;

        Modifiers = InventoryManager.Instance.GetPlayerModifierInventory();

        LeftTargetView.SetActive(LeftTarget != null);
        RightTargetView.SetActive(RightTarget != null);

        if (LeftTarget != null)
        {
            LeftView.Enable();
            LeftView.SetState(LeftTarget, Modifiers, InventoryManager.Instance.CharmSlots[0].ModifierId);
            LeftHudView.SetState(LeftTarget, Modifiers, InventoryManager.Instance.CharmSlots[0].ModifierId, charmChange.LeftUpdate);
        }
        else
            LeftView.Disable();

        if (RightTarget != null)
        {
            RightView.Enable();
            RightView.SetState(RightTarget, Modifiers, InventoryManager.Instance.CharmSlots[1].ModifierId);
            RightHudView.SetState(RightTarget, Modifiers, InventoryManager.Instance.CharmSlots[1].ModifierId, charmChange.RghtUpdate);
        }
        else
            RightView.Disable();

        if (charmChange.SquareUPdate)
            UpdateSwapTargetOnly();     
    }

    public void UpdateSwapTargetOnly()
    {
        SwapTarget = InventoryManager.Instance.CharmSlots[2].Target;
        SwapView.SetEnabled(SwapTarget != null);

        if (SwapTarget != null)
            SwapView.SetTarget(InventoryManager.Instance.CharmSlots[2].Activated
                ? AdjustableObjectTargetType.SquareA
                : AdjustableObjectTargetType.SquareB);
    }

    public void AdvanceFrame()
    {
        LeftHudView.AdvanceFrame();
        RightHudView.AdvanceFrame();
    }


}
