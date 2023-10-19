using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class Dooberator : MonoBehaviour
{
    public static Dooberator Instance;
    Vector2 max;

    private void Start()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one dooberator! No!");
            return;
        }
        
        Instance = this;
        max = GetComponent<RectTransform>().rect.max;
    }

    public GameObject Dooberate(GameObject prefab, Vector2 screenLocation)
    {
        GameObject NewObject = Instantiate(prefab, transform);

        // The * 2f accounts for an issue with screen position reading out
        // when Pixel Perfect Camera is on. If this breaks later, it's because
        // it was fixed. Just set it back to 1 and -1 respectively and see
        // what happens.
        Vector2 newPosition = new Vector2(screenLocation.x * max.x * 2f, (1f - screenLocation.y) * max.y * -2f);

        NewObject.GetComponent<RectTransform>().anchoredPosition = newPosition;
        return NewObject;
    }
}
