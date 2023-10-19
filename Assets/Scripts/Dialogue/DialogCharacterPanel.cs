using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DialogCharacterPanel : MonoBehaviour
{
    [SerializeField]
    private Image CharacterImage = null;
    [SerializeField]
    private RawImage Background = null;

    [SerializeField]
    private GameObject[] AllPanels;

    Tween scaleRepeatTween;
    Tween scaleReturnTween;

    bool speaking = false;

    DialogCharacterConfig currentCharacter;

    public void SetCharacterConfig(DialogCharacterConfig config)
    {
        currentCharacter = config;

        CharacterImage.sprite = config.CharacterImage;
        Background.material = config.CharacterBackgroundMaterial;
        Background.texture = config.CharacterBackgroundImageTile.texture;

        // If this is the left panel, ignore everything else, we're done
        if (config.Character == DialogCharacter.John)
            return;

        // todo: this is extremely hack but needded for the demo
        AllPanels[0].SetActive(config.Character != DialogCharacter.Portalmaw1);
        AllPanels[1].SetActive(config.Character == DialogCharacter.Portalmaw1);

    }

    public void CharacterSpeaking()
    {
        if (speaking)
            return;

        scaleReturnTween.Kill();

        speaking = true;

        scaleRepeatTween = getCurrentDialogPanelTransform().DOScale(
            new Vector3(1.1f, 1.1f, 1f), 1f).
            SetUpdate(UpdateType.Manual).
            SetLoops(-1).
            SetEase(Ease.InOutSine);
    }

    Transform getCurrentDialogPanelTransform()
    {
        return currentCharacter.Character == DialogCharacter.Portalmaw1 ?
            AllPanels[1].transform : CharacterImage.transform;
    }

    public void CharacterStopSpeaking()
    {
        if (!speaking)
            return;

        speaking = false;

        scaleRepeatTween.Kill();

        scaleReturnTween = getCurrentDialogPanelTransform().DOScale(
            new Vector3(1f, 1f, 1f), .5f).
            SetUpdate(UpdateType.Manual).
            SetEase(Ease.OutQuad);
    }

}
