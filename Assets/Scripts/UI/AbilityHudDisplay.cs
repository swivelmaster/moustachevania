using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilityHudDisplay : MonoBehaviour
{
    public static AbilityHudDisplay instance { private set; get; }

    [SerializeField]
    private Image RegularJump = null;
    [SerializeField]
    private Image SecondJump = null;
    [SerializeField]
    private Image Dash = null;

    [SerializeField]
    private Color FadedOut = Color.clear;

    public bool EnableFeature = false;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (!EnableFeature)
        {
            RegularJump.enabled = false;
            SecondJump.enabled = false;
            Dash.enabled = false;
        }
    }

    private void Update()
    {
        if (!EnableFeature)
            return;

        Player player = PlayerManager.Instance.currentPlayer;
        if (player == null)
        {
            RegularJump.enabled = false;
            SecondJump.enabled = false;
            Dash.enabled = false;
            return;
        }

        UpdateJumpsRemaining(player.JumpsRemaining(), player.playerAbilityInfo.maxJumps);
        UpdateDashState(player.CanDash(), player.playerAbilityInfo.hasDash);
    }

    public void UpdateJumpsRemaining(int remaining, int maxJumps)
    {
        SecondJump.enabled = maxJumps > 1;

        if (remaining == 0)
        {
            RegularJump.color = FadedOut;
        }

        if (remaining <= 1 && maxJumps >= 2)
        {
            SecondJump.color = FadedOut;
        }

        if (remaining >= 1)
        {
            RegularJump.color = Color.white;
        }

        if (remaining == 2)
        {
            SecondJump.color = Color.white;
        }
    }

    public void UpdateDashState(bool canDash, bool hasDash)
    {
        if (!hasDash)
        {
            Dash.enabled = false;
            return;
        }

        Dash.enabled = true;
        Dash.color = canDash ? Color.white : FadedOut;
    }
}
