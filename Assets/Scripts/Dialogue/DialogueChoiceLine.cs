using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueChoiceLine : MonoBehaviour
{
    [SerializeField]
    private Image SelectionImage = null;
    [SerializeField]
    private TMP_Text SelectionText = null;

    public void Start()
    {
        SelectionText.color = new Color(1f, 1f, 1f, 0f);
    }

    public void UpdateText(string newText, float delay)
    {
        SelectionText.text = newText;
        SelectionText.DOColor(new Color(1f, 1f, 1f, 1f), .5f).SetDelay(delay).SetUpdate(UpdateType.Manual);
    }

    public void SetIsCurrentSelection(bool isCurrentSelection)
    {
        SelectionImage.enabled = isCurrentSelection;
    }

    public string GetText()
    {
        return SelectionText.text;
    }
}
