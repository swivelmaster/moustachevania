using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// todo: Rewrite this to use the framework correctly
// Currently using DOTween without managing pause/resume
public class CollectibleManager : MonoBehaviour {

	public int currentScore { private set; get; }
    public static CollectibleManager Instance = null;

	public int maximumScore { get
		{
			return CheesePickup.TotalCheesePickups;
		} 
	}

	CheeseCountHud hud;
	public GameObject gameCompleteScreen;
	public TMP_Text gameCompleteText;

	public void Init(CheeseCountHud hud)
    {
		this.hud = hud;
		hud.Init(this);
    }

    private void Awake()
    {
		// Don't do null check, just replace the instance.
		// When loading a new scene, we need the references to the
		// specific scene's stuff to make this work.
        Instance = this;
    }

	public void IncrementScore()
	{
		currentScore++;
		UpdateScoreText();
		CheckForEndgame();

    }

	public void DecrementScore()
	{
		currentScore--;
		UpdateScoreText();
	}

	public void DoCheeseDoober(Vector3 worldPosition)
	{
		hud.DoCheeseDoober(worldPosition);
	}

	void UpdateScoreText()
	{
		hud.UpdateScore();
	}

	void CheckForEndgame()
	{
        if (currentScore == maximumScore)
		{
			ShowEndgameScreen();
        }
    }
	
	// This shouldn't be here, but because I'm hacking everything together
	// to release the game for free, this is what we get for now.
	void ShowEndgameScreen()
	{
		var save = PersistenceManager.Instance.savedGame;
		var timeString = Mathf.Floor((save.totalGameTime / 60)).ToString() + " minutes and "
			+ Mathf.Floor((save.totalGameTime % 60)).ToString() + " seconds ";
		gameCompleteText.text = gameCompleteText.text.Replace("{t}", timeString)
			.Replace("{d}", save.deaths.ToString())
			.Replace("{l}", save.deathsByLava.ToString())
			.Replace("{b}", save.deathByBrownEnemy.ToString())
			.Replace("{bl}", save.deathByBlueEnemy.ToString())
			.Replace("{s}", save.deathBySquish.ToString());

        gameCompleteScreen.SetActive(true);
    }

}
