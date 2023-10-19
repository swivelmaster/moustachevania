using System.Collections;
using System.Collections.Generic;
using Com.LuisPedroFonseca.ProCamera2D;
using UnityEngine;

public class GameCameraAdapter : MonoBehaviour {

	public static GameCameraAdapter instance;

	public ProCamera2D proCamera2D;
	ProCamera2DShake cameraShake;

	GameObject objectToFollow;

	[SerializeField]
	private Camera myCamera = null;

	void Awake() 
	{
		instance = this;

		myCamera.enabled = false;
		cameraShake = proCamera2D.GetComponent<ProCamera2DShake>();
		StartCoroutine(DelayedStart());
	}

	// Wait until end of frame (when everything has processed and the
	// camera has been moved to the appropriate place) to activate the camera.
	IEnumerator DelayedStart()
    {
		yield return new WaitForEndOfFrame();

		myCamera.enabled = true;
	}

	public void StartGame()
    {
		ForceLocationDuringPlay();	
    }

	float snapDuration = .3f;
	float snapStart = 0f;
	bool currentlySnapping = false;
	Vector3 snapStartPosition;

	public void SnapToPositionQuickly(){
		// If this is called twice in a row... do nothing?
		if (currentlySnapping)
        {
			Debug.LogWarning("SnapToPositionQuickly() called too quickly!");
			return;
		}

		currentlySnapping = true;
		snapStart = GameplayManager.Instance.GameTime;
		snapStartPosition = transform.position;
	}

	public void SetObjectToFollow(GameObject o)
	{
		objectToFollow = o;

		proCamera2D.RemoveAllCameraTargets();
		proCamera2D.AddCameraTarget(o.transform);
	}

    public void LateAdvanceFrame()
    {
		proCamera2D.enabled = GameStateManager.Instance.CurrentGameState == GameState.Play
			&& !currentlySnapping;

		if (currentlySnapping)
        {
			if (ContinueSnap())
				return;
			else
            {
				// Back at origin
				// todo: Use proCamera2D's ACTUAL API to do this instead of hacking it
				ForceLocationDuringPlay();
			}
		}

		proCamera2D.Move(Time.deltaTime);
    }

	bool ContinueSnap()
    {
		float gameTime = GameplayManager.Instance.GameTime;
		float pctComplete = (gameTime - snapStart) / snapDuration;
		var newPos = Vector2.Lerp(snapStartPosition,
			PlayerManager.Instance.currentPlayer.transform.position, pctComplete);

		transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);

		if (pctComplete >= 1f)
			currentlySnapping = false;

		MainCameraPostprocessingEffects.instance.SetMotionBlurEnabled(currentlySnapping);

		return currentlySnapping;
	}

	public GameObject GetObjectToFollow()
	{
		return objectToFollow;
	}

	public void ForceLocationDuringPlay()
    {
		proCamera2D.MoveCameraInstantlyToPosition(
			PlayerManager.Instance.currentPlayer.transform.position);
	}

	public void PlayReactToDestroyVFX(DestroyableVFXOptions option)
    {
		switch (option)
        {
			case DestroyableVFXOptions.Mild:
				MainCameraPostprocessingEffects.instance.Punch();
				Shake("Camera Shake Preset - Mild");
				break;

        }
    }

	public void Shake(float duration=.35f, float strength=1f)
	{
		cameraShake.Shake(duration, new Vector2(strength, strength));
	}

	private void Shake(string presetName)
    {
		cameraShake.Shake(presetName);
    }

	public void ShakeOn(float amount)
    {
		cameraShake.ConstantShake("Camera Shake Preset - Constant");
		Debug.LogWarning("If trying to constant shake and it " +
            "appears useless, try adding layers to the preset.");
	}

	public void ShakeOff()
    {
		cameraShake.StopConstantShaking();
    }
}
