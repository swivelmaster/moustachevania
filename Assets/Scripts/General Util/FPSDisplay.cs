using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// FPS script adapted from the Unity Wiki FPSDisplay.cs
public class FPSDisplay : MonoBehaviour {

	Text t;
	float deltaTime;

	void Start () {
		t = GetComponent<Text> ();
	}

	string template = "{0:0.0} ms ({1:0.} fps)";
	float msec, fps;

	void Update () {
		deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
		msec = deltaTime * 1000.0f;
		fps = 1.0f / deltaTime;
		t.text = string.Format(template, msec, fps);
	}
}
