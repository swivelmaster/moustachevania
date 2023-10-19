using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
	public string YarnConversationName;
	public string EncounteredVariableName;

	[Header("Call Destroyable.Destroyed() on this object when cutscene is over.")]
	public bool AutoDestroyObject = false;

	[Header("For testing, only works in editor")]
	public bool ForceShowCutscene = false;

	void OnTriggerEnter2D(Collider2D c)
	{
		bool encountered = DialogueVariableStorageBridge.GetInstance()
			.GetBool(EncounteredVariableName);

        if (c.gameObject.CompareTag("Player") && !encountered)
		{
			GameEventManager.Instance.CutsceneTriggered.Invoke(GetCutsceneName(), CutsceneComplete);
        }
#if UNITY_EDITOR
		else if (ForceShowCutscene)
		{
            GameEventManager.Instance.CutsceneTriggered.Invoke(GetCutsceneName(), CutsceneComplete);
        }
#endif
	}

	/// <summary>
    /// Virtual so we can override behavior for pickups to auto-generate
    /// cutscene names.
    /// </summary>
    /// <returns></returns>
	protected virtual string GetCutsceneName()
    {
		return YarnConversationName;
    }

	/// <summary>
    /// Same reasoning as above.
    /// </summary>
    /// <returns></returns>
	protected virtual string GetEncounteredVariableName()
	{
		return EncounteredVariableName;
	}

	public virtual void CutsceneComplete()
    {
		if (AutoDestroyObject)
        {
			var destroyable = GetComponent<Destroyable>();
			if (destroyable != null)
            {
				destroyable.Destroyed();
            }
        }
    }
}
