using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class CheeseCountHud : MonoBehaviour
{
    public TMP_Text scoreText;

    public RectTransform CheeseUITransform;

    public RectTransform CheeseBackgroundUITransform;

    public GameObject CheeseUIElementPrefab;

    public Transform UIOverlayCanvas;

    CollectibleManager collectibleManager;

    public void Init(CollectibleManager collectibleManager)
    {
        this.collectibleManager = collectibleManager;
    }

    bool ready = false;
    void Start()
    {
        StartCoroutine(LateStart());
    }

    IEnumerator LateStart()
    {
        yield return new WaitUntil(() => scoreText != null);

        ready = true;
        UpdateScore();
    }

    public void UpdateScore()
    {
        if (!ready)
            return;

        if (collectibleManager.currentScore == 0)
        {
            scoreText.gameObject.SetActive(false);
            CheeseUITransform.gameObject.SetActive(false);
            CheeseBackgroundUITransform.gameObject.SetActive(false);
            return;
        }

        scoreText.gameObject.SetActive(true);
        CheeseUITransform.gameObject.SetActive(true);
        CheeseBackgroundUITransform.gameObject.SetActive(true);

        scoreText.text = "X " + collectibleManager.currentScore.ToString() + " / " + collectibleManager.maximumScore.ToString();
    }

    public void DoCheeseDoober(Vector3 worldPosition)
    {
        Vector2 ScreenPosition = Camera.main.WorldToViewportPoint(worldPosition);
        GameObject NewCheese = Dooberator.Instance.Dooberate(CheeseUIElementPrefab, ScreenPosition);
        RectTransform rectTransform = NewCheese.transform.GetComponent<RectTransform>();
        StartCoroutine(MoveCheeseAfterOneFrame(rectTransform, NewCheese));
    }

    IEnumerator MoveCheeseAfterOneFrame(RectTransform rectTransform, GameObject NewCheese)
    {
        // Need to wait an extra frame (hence waiting once for end of current frame,
        // then again for end of next frame) because the tween is so fast that frame 1
        // of it will actually move the cheese significantly from its starting position.
        // We need to see it in its starting position otherwise it just looks... wrong.
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        rectTransform.DOAnchorPosX(CheeseUITransform.anchoredPosition.x, .45f).SetEase(Ease.InOutCirc).OnComplete(() => Destroy(NewCheese));
        rectTransform.DOAnchorPosY(CheeseUITransform.anchoredPosition.y, .45f).SetEase(Ease.InCubic);
    }
}
