using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.InteropServices;

[System.Serializable]
public class SavedGameState : object
{
    // Since webgl filesystem access is a wrapper around
    // IndexDB, we actually have to manually call out to Javascript
    // to load the IndexDB data into memory (startup) and flush
    // in-memory file operations to the persistent db (sync).
    // THE MORE YOU KNOW!
    // These methods are actually calling into a Javascript library
    // that gets compared with the project.
	// More information:
    // https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html
#if UNITY_WEBGL
    private static bool StartupCalled = false;

    [DllImport("__Internal")]
	private static extern void FSStartup();

	[DllImport("__Internal")]
	private static extern void FSSync();
#endif


    [System.Serializable]
	public class SerializableVector3 
	{
		float x;
		float y;
		float z;

		public SerializableVector3(Vector3 v)
		{
			x = v.x;
			y = v.y;
			z = v.z;
		}

		public Vector3 GetVector3()
		{
			return new Vector3 (x, y, z);
		}
	}

	public List<string> FlaggedIds = new List<string>();

    /// <summary>
    /// Default/fallback player state. 
	/// This used to be the value used by default when no saved state was found,
	/// but that is now defined PER SCENE in CheckpointSceneValues (in SceneSettings object)
    /// </summary>
	public SavedPlayerState PlayerState = new SavedPlayerState(1, true, false, false, false, false, false);

	public string CurrentCheckpoint = null;

    public int deaths = 0;

    public int deathsByLava = 0;
    public int deathByBrownEnemy = 0;
    public int deathByBlueEnemy = 0;
    public int deathBySquish = 0;

    public float totalGameTime = 0;

	public Dictionary<string, bool> DialogueVariables = new Dictionary<string, bool>();

	public string CurrentScene = "";

	public List<int> InventoryItems = new List<int>();

	public CharmSlot[] CharmSlots = new CharmSlot[2];

	public static void SaveGameState(SavedGameState state)
	{
		//DebugOutput.instance.DebugTransient("Attempting to save...", 10f);

		// todo: BinaryFormatter is deprecated, need to switch to something else
		BinaryFormatter bf = new BinaryFormatter ();
		MemoryStream stream = new MemoryStream();

		bf.Serialize (stream, state);

		File.WriteAllBytes(SavedGameState.GetSavedGamePath(), stream.ToArray());
#if UNITY_WEBGL && !UNITY_EDITOR
        FSSync();
#endif
	}

	public static SavedGameState LoadGameState(out bool isNewGame)
	{
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!StartupCalled)
		{
            FSStartup();
			StartupCalled = true;
        }
#endif

        //DebugOutput.instance.DebugTransient("Attempting to load saved game state.");

        if (File.Exists(SavedGameState.GetSavedGamePath()))
		{
			BinaryFormatter bf = new BinaryFormatter ();

			FileStream file = File.Open (SavedGameState.GetSavedGamePath (), FileMode.Open);

			try {
				SavedGameState save = (SavedGameState)bf.Deserialize (file);

				if (save.InventoryItems == null)
					save.InventoryItems = new List<int>();

				if (save.CharmSlots == null)
					save.CharmSlots = new CharmSlot[2];

                //Debug.Log("Number of deaths: " + save.deaths.ToString());
                //Debug.Log("Time elapsed: " + save.totalGameTime.ToString());

                //DebugOutput.instance.DebugTransient("Successfully loaded save game state.");
					//Debug.Log("Flagged ids - " + string.Join(", ", save.FlaggedIds.ToArray()) );
				file.Close();
				isNewGame = false;
				return save;
			}
			catch
			{
                //Debug.LogError ("Saved game format was fucked. Returning empty save.");
				DeleteCurrentSaveGame ();
                file.Close();
				isNewGame = true;
				return new SavedGameState ();
			}
		}
		else 
		{
            //DebugOutput.instance.DebugTransient("No saved game available / failed to load.");
			isNewGame = true;
			return new SavedGameState ();
		}
	}

	static string GetSavedGamePath()
	{
        // Workaround for itch.io uploads
        // Unity bases its save path on the URL the 
        // game was uploaded to. Each version of itch.io
        // webgl games are uploaded to a separate directory
        // (I assume for caching reasons) so we have to manually
        // pick our own save path here. It's in /idbfs/ because 
        // the scripting backend's filesystem access is actually
        // a wrapper for IndexDB. It's a gotcha on top of a gotcha!
        // And the other gotcha is the sync functions at the top
        // of this file, so that's THREE gotchas!
#if UNITY_WEBGL && !UNITY_EDITOR
        var path = Path.Combine("/idbfs/", "moustachevania_save.dat");
#else
        var path = Path.Combine(Application.persistentDataPath, "moustachevania_save.dat");

#endif
        //if (DebugOutput.instance != null)
		// DebugOutput.instance.DebugTransient("Save Game Path: " + path, 10);
		return path;
	}

#if UNITY_EDITOR
	[MenuItem("Moustachevania/Delete Saved Game")]
#endif
	public static void DeleteCurrentSaveGame()
	{
		Debug.Log ("Deleting current saved game.");
		File.Delete(SavedGameState.GetSavedGamePath());
	}
}

