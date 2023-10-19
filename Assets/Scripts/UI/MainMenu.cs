using Rewired.UI.ControlMapper;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour 
{
	public GameObject MainMenuObject;
	public GameObject ModeSelectorObject;

	// Menu components aren't necessarily on the objects representing them
	public MenuSelector mainMenu;
	public MenuSelector modeSelectMenu;

	bool savedGameAvailable = false;

	// Placeholders
	bool extraGameModesUnlocked = false;

	public AudioSource menuSound;
	public AudioSource itemSelectSound;

	public ControlMapper controlMapper;

	void Start () {
		savedGameAvailable = GameManager.instance.SavedGameAvailable ();

		if (!savedGameAvailable)
		{
			mainMenu.SetUnavailable (0);	
		}

		if (!extraGameModesUnlocked)
		{
			modeSelectMenu.SetUnavailable (1);
			modeSelectMenu.SetUnavailable (2);
			modeSelectMenu.SetUnavailable (3);
			modeSelectMenu.SetUnavailable (4);
			modeSelectMenu.SetUnavailable (5);
		}

		SetActiveMenu (WhichMenu.MainMenu);
	}

	enum WhichMenu {
		MainMenu, GameModeMenu, ControlRemapping
	}

	void SetActiveMenu(WhichMenu menu)
	{
		MainMenuObject.SetActive (menu == WhichMenu.MainMenu);
		mainMenu.enabled = menu == WhichMenu.MainMenu;

		ModeSelectorObject.SetActive (menu == WhichMenu.GameModeMenu);
		modeSelectMenu.enabled = menu == WhichMenu.GameModeMenu;
	}

	// Hack to give DemoIntroUI access to know which scene to go to next
	public static GameManager.GameMode LastGameModeSelected { private set; get; }

	public void Submit(MenuSelector.MenuSubmitAction action)
	{
		itemSelectSound.Play ();

		if (action.menuName == "MainMenu")
		{
			switch (action.selectedOption)
			{
			case 0:
				GameManager.instance.StartGame (false);
				break;
			case 1:
					LastGameModeSelected = GameManager.GameMode.Classic;
                    SceneManager.LoadScene("IntroTransitionScene");
                    // Moving this to the demo intro UI
                    //GameManager.instance.StartGame(true, GameManager.GameMode.Classic);
                    // No game mode select anymore
                    //SetActiveMenu (WhichMenu.GameModeMenu);
                    break;
                case 2:
					LastGameModeSelected = GameManager.GameMode.Fancy;
                    SceneManager.LoadScene("IntroTransitionScene");
                    // Moving this to the demo intro UI
                    //GameManager.instance.StartGame(true, GameManager.GameMode.Fancy);
                    // No game mode select anymore
                    //SetActiveMenu (WhichMenu.GameModeMenu);
                    break;
                case 3:
					controlMapper.Open();
					SetActiveMenu(WhichMenu.ControlRemapping);
                    break;
			case 4:
				Application.Quit ();
				return;
			}	
		}
		/**
		else if (action.menuName == "ModeSelectionMenu")
		{
			switch(action.selectedOption)
			{
			case 0:
				GameManager.instance.StartGame (true, GameManager.GameMode.Classic);
				break;
			case 1:
				GameManager.instance.StartGame (true, GameManager.GameMode.TimeAttack);
				break;
			case 2:
				GameManager.instance.StartGame (true, GameManager.GameMode.CheeseAttack);
				break;
			case 3:
				GameManager.instance.StartGame (true, GameManager.GameMode.NoContinue);
				break;
			case 4:
				GameManager.instance.StartGame (true, GameManager.GameMode.NoHat);
				break;
			case 5:
				GameManager.instance.StartGame (true, GameManager.GameMode.TwoHat);
				break;
			case 6:
				SetActiveMenu (WhichMenu.MainMenu);
				break;
			}
		} **/
	}

	public void ExitControlRemapping()
	{
		SetActiveMenu(WhichMenu.MainMenu);
	}
	public void SelectedMenuItemChanged()
	{
		menuSound.Play ();
	}
}
