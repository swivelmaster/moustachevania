using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuSelector : MonoBehaviour {

	public struct MenuSubmitAction {
		public int selectedOption;
		public string menuName;
	}

	ControllerInputManager inputManager;

	public MonoBehaviour receiver;

	public string menuName;

	public Text[] MenuItems;
	public Image[] SelectorImages;

	int currentSelection = 0;

	int optionsCount;

	bool shouldUpdate = false;
	bool showNoOptions = false;

	List<int> unavailableOptions = new List<int>();

	public Color availableOptionColor;
	public Color unavailableOptionColor;

	void Start () {
		GlobalPersistentObjectManager.Instance.GetGlobalManagers(out inputManager);

		optionsCount = MenuItems.Length;

		if (MenuItems.Length != SelectorImages.Length)
		{
			Debug.LogError ("Error: MenuSelector has non-matching number of menu items and selector images");
		}

		DrawCorrectSelectorImage ();
	}

	public void SetAvailable(int option)
	{
		if (unavailableOptions.Contains(option))
		{
			unavailableOptions.Remove (option);
		}

		shouldUpdate = true;
	}

	public void SetUnavailable(int option)
	{
		if (unavailableOptions.Contains(option))
		{
			return;
		}
		else 
		{
			unavailableOptions.Add (option);
		}

		shouldUpdate = true;
	}

	void DrawCorrectSelectorImage()
	{
		for (int i=0;i<MenuItems.Length;i++)
		{
			if (showNoOptions)
			{
				SelectorImages [i].enabled = false;
			}
			else 
			{
				SelectorImages [i].enabled = i == currentSelection;	
			}
		}
	}

	void Update () {
		var input = inputManager.GetCurrentInput(true);

		if (input.Jumping == ControlInputFrame.ButtonState.Down){
			shouldUpdate = true;
			MenuSubmitAction action = new MenuSubmitAction ();
			action.menuName = menuName;
			action.selectedOption = currentSelection;
			receiver.SendMessage ("Submit", action);
		}

		HandleMenuItemChanges (input);

		if (shouldUpdate){
			ValidateSelectedOption ();
			DrawCorrectSelectorImage ();
			DrawMenuItems ();
		}

		shouldUpdate = false;
	}

	void DrawMenuItems()
	{
		for (int i=0;i<MenuItems.Length;i++)
		{
			if (unavailableOptions.Contains (i))
			{
				MenuItems [i].color = unavailableOptionColor;
			}
			else 
			{
				MenuItems [i].color = availableOptionColor;
			}
		}
	}

	void HandleMenuItemChanges(ControlInputFrame input)
	{
		if (!input.VerticalDownThisFrame)
			return;

		if (input.Vertical == VerticalInput.Up)
		{
			ChangeSelectedMenuItem (false);
		}
		else if (input.Vertical == VerticalInput.Down)
		{
			ChangeSelectedMenuItem (true);
		}
	}

	void ChangeSelectedMenuItem(bool increment)
	{
		if (increment){
			currentSelection++;
		}
		else 
		{
			currentSelection--;
		}

		// Pass through which direction we were going so if we hit an invalid option, we can continue
		// in the right direction.
		ValidateSelectedOption (increment);

		receiver.SendMessage ("SelectedMenuItemChanged");

		shouldUpdate = true;
	}

	void ValidateSelectedOption(bool increment = true)
	{
		// Failsafe against all options being unavailable, which would make below infinite
		if (unavailableOptions.Count == optionsCount)
		{
			showNoOptions = true;
			return;
		}

		while (unavailableOptions.Contains(currentSelection) || currentSelection < 0 || currentSelection > optionsCount - 1)
		{
			if (currentSelection > optionsCount - 1)
				currentSelection = 0;

			if (currentSelection == -1)
				currentSelection = optionsCount - 1;

			if (unavailableOptions.Contains(currentSelection))
			{
				if (increment)
				{
					currentSelection++;
				}
				else 
				{
					currentSelection--;
				}
			}	
		}
	}

}
