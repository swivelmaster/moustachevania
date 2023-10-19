using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Used only in FPS counter. FPS counter should probably just be
// rewritten to use DebugOutput, since it does the same thing.
public class UIDebugInfo : MonoBehaviour {

	public static UIDebugInfo instance;

	public Text debugText;

	// Use this for initialization
	void Start () {
		UIDebugInfo.instance = this;
	}

	public void Info(string info) {
		debugText.text = info;
	}
}
