using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class InventoryTargetSelectCloseHud : MonoBehaviour
{
    const float MODIFIER_LIST_OFFSET = 24f;
    const float SELECTED_MODIFIER_SCALE = 2f;

    const float HUD_START_OPACITY = .7f;

    [SerializeField]
    private Image TargetImage = null;

    [SerializeField]
    private RectTransform ListContainer = null;
    List<TargetModifierIcon> targetModifierIcons = new List<TargetModifierIcon>();

    [SerializeField]
    private CanvasGroup ModifierListCanvasGroup = null;

    [SerializeField]
    private TargetModifierIcon IconPrefab = null;

    const float MODIFIER_FADE_TIME = .25f;
    const float MODIFIER_FADE_DELAY = .75f;
    float lastUpdated = 0f;

    const string PUNCH_TWEEN_ID = "punch_tween";

    private void Start()
    {
        TargetImage.gameObject.SetActive(false);
    }

    string GetTweenIdForTarget(InventoryItem target)
    {
        return PUNCH_TWEEN_ID + "_" + target.targetType.ToString();
    }

    public void SetState(InventoryItem target, List<InventoryItem> modifiers, int currentlySelected, bool pulseCurrentlySelected)
    {
        DOTween.Kill(GetTweenIdForTarget(target));
        GameUtils.DestroyAllChildren(ListContainer);
        targetModifierIcons.Clear();

        // Don't show target icon or modifier list if there are no modifiers!
        // Count should be 1 even if user has none because
        // the empty inventory item modifier is always added anyway
        TargetImage.gameObject.SetActive(modifiers.Count > 1);
        if (modifiers.Count <= 1)
            return;

        for (int i = 0; i < modifiers.Count; i++)
        {
            var temp = Instantiate(IconPrefab, ListContainer);
            temp.Init(modifiers[i]);
            temp.rectTransform.anchoredPosition = new Vector2(0f, MODIFIER_LIST_OFFSET * i * -1f);
            if (modifiers[i].itemId == currentlySelected)
            {
                temp.rectTransform.localScale = new Vector3(SELECTED_MODIFIER_SCALE, SELECTED_MODIFIER_SCALE, 1f);

                temp.rectTransform.DOPunchScale(Vector3.one * SELECTED_MODIFIER_SCALE * 1.25f, .2f)
                       .SetId(GetTweenIdForTarget(target));
            }
                
            targetModifierIcons.Add(temp);
        }

        lastUpdated = GameplayManager.Instance.GameTime;

        SetCanvasAlpha();
    }

    public void AdvanceFrame()
    {
        SetCanvasAlpha();
    }

    void SetCanvasAlpha()
    {
        float fadeStartTime = lastUpdated + MODIFIER_FADE_DELAY;

        // I don't know a better way to format this to make it readable
        // Basically, alpha is 1 for the duration of MODIFIER_FADE_DELAY
        // and THEN lerp from 1 to 0 over the course of MODIFIER_FADE_TIME
        ModifierListCanvasGroup.alpha =
            GameplayManager.Instance.GameTime <= fadeStartTime
            ? 1f
            : Mathf.Lerp(HUD_START_OPACITY, 0f,
                Mathf.Min(1f, (GameplayManager.Instance.GameTime - fadeStartTime) / MODIFIER_FADE_TIME)
            );
    }

}
