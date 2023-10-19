using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
	public static GameManager instance;

	public static int pendingAreaTransitionCheckpoint { private set; get; }

    void Awake(){
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (instance != this)
		{
			Destroy(gameObject);
			return;
		}
		
		Application.targetFrameRate = 60;

#if PLATFORM_STANDALONE && !UNITY_EDITOR
		Cursor.visible = false;
#endif

		pendingAreaTransitionCheckpoint = -1;
	}

    void Start () {

    }

	GameMode nextGameMode = GameMode.Classic;

	public void StartGame (bool ForceNewGame, GameMode mode = GameMode.Classic)
	{
		this.nextGameMode = mode;

		string sceneToLoad = mode == GameMode.Classic ? "Original Game Scene" : "RoF DEMO";

		// These accumulate on Start() so reset it when starting a new game
		CheesePickup.TotalCheesePickups = 0;

		if (ForceNewGame)
		{
			SavedGameState.DeleteCurrentSaveGame ();
		}
		else
        {
			bool isNew;
			var savedGame = SavedGameState.LoadGameState(out isNew);
			if (!isNew)
            {
				sceneToLoad = savedGame.CurrentScene;
            }
		}

		SceneManager.LoadScene (sceneToLoad);
    }

    public GameMode GetNextGameMode()
	{
		return nextGameMode;
	}

	public bool SavedGameAvailable()
	{
		bool isNew;
		SavedGameState.LoadGameState(out isNew);
		return !isNew;
	}

    bool paused = false;

    public bool isPaused()
    {
        return paused;
    }

	public void AreaTransition(string toScene, int toCheckpoint)
    {
		pendingAreaTransitionCheckpoint = toCheckpoint;

		// todo: Replace with loadsceneasync so we can have an actual loading
		// transition with animation or something.
		SceneManager.LoadScene(toScene, LoadSceneMode.Single);
    }

	public void ClearPendingAreaCheckpoint()
    {
		pendingAreaTransitionCheckpoint = -1;
    }


	// Not really using these game modes, todo: Re-map to new
	// game mode spec at some point.
	public enum GameMode
	{
		Classic, Fancy//TimeAttack, CheeseAttack, NoContinue, NoHat, TwoHat
	}

}
