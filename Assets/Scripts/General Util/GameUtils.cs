using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GameUtils : MonoBehaviour
{
    // This was for when a bunch of Z values somehow ended up BEHIND the camera
    // and stopped appearing anywhere. Commented out the menu item because it hasn't 
    // happened in a while and I have no idea how it happened in the first place.
    //[MenuItem("Moustachevania/Fix Z Values")]
    public static void FixZValues()
    {
        int found = 0;
        GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject gameObject in gameObjects)
        {
            if (Mathf.Abs(gameObject.transform.position.z) > 100f)
            {
                Debug.Log(gameObject.name);
                gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, 0f);
                found++;
            }
        }

        Debug.Log("Found and fixed " + found.ToString() + " objects out of place");
    }

    public static int UpdateCollectionCursor(ICollection collection, int newCursor, bool preventWrap=false)
    {
        return UpdateCollectionCursor(collection.Count, newCursor, preventWrap);
    }

    public static int UpdateCollectionCursor(int count, int newCursor, bool preventWrap = false)
    {
        // If cursor is in range, return it
        if (newCursor >= 0 && newCursor < count)
            return newCursor;

        // If less than 0 wrap to end
        if (newCursor < 0)
            return preventWrap ? 0 : count - 1;

        // if more than end wrap to 0
        return preventWrap ? count - 1 : 0;
    }

    public static List<Tween> ConfigureTweensForDialog(List<Tween> tweensList)
    {
        return ConfigureTweensForDialog(tweensList.ToArray());
    }

    public static List<Tween> ConfigureTweensForDialog(Tween[] tweensArray)
    {
        var tweens = new List<Tween>();
        foreach (var tween in tweensArray)
        {
            tweens.Add(tween);
            tween.SetUpdate(UpdateType.Manual);
            tween.SetAutoKill(false);
        }
        return tweens;
    }

    public static List<Tween> ConfigureTweensForDialog(DOTweenAnimation[] animations)
    {
        var tweens = new List<Tween>();

        foreach (var animation in animations)
        {
            tweens.AddRange(ConfigureTweensForDialog(animation));
        }

        return tweens;
    }

    public static List<Tween> ConfigureTweensForDialog(DOTweenAnimation dOTweenAnimation)
    {
        var tweens = dOTweenAnimation.GetTweens();
        foreach (var tween in tweens)
        {
            tween.SetUpdate(UpdateType.Manual);
            tween.SetAutoKill(false);
        }
            
        return tweens;
    }

    // I'm pretty sure this is from StackOverflow, I should have saved the source.
    public static bool ContainsPoint(Vector2[] polyPoints, Vector2 p)
    {
        var j = polyPoints.Length - 1;
        var inside = false;
        for (int i = 0; i < polyPoints.Length; j = i++)
        {
            var pi = polyPoints[i];
            var pj = polyPoints[j];
            if (((pi.y <= p.y && p.y < pj.y) || (pj.y <= p.y && p.y < pi.y)) &&
                (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x))
                inside = !inside;
        }
        return inside;
    }

    // Not to be googled :)
    public static void DestroyAllChildren(Transform transform)
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }

    public static float CountupPct(float startTime, float currentTime, float duration)
    {
        if (currentTime == startTime)
            return 0f;

        return Mathf.Min(1f, (currentTime - startTime) / duration);
    }

    public static void RotatePointsAroundPivot(Vector3[] points, Vector3 pivot, Vector3 angles)
    {
        for (int i = 0; i < points.Length; i++)
            points[i] = RotatePointAroundPivot(points[i], pivot, angles);
    }

    // From https://answers.unity.com/questions/532297/rotate-a-vector-around-a-certain-point.html
    public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
        Vector3 dir = point - pivot; // get point direction relative to pivot
        dir = Quaternion.Euler(angles) * dir; // rotate it
        point = dir + pivot; // calculate rotated point
        return point; // return it
    }

    public static float DualMaxDistanceCheck(Vector2 reference, Vector2 compare1, Vector2 compare2)
    {
        var d1 = Vector2.Distance(reference, compare1);
        var d2 = Vector2.Distance(reference, compare2);
        //Debug.Log("d1 is " + d1.ToString() + " and d2 is " + d2.ToString());
        return Mathf.Max(d1, d2);
    }

    const float PHYSICS_ENABLE_DISTANCE = 25f;
    public static bool ShouldEnablePhysics(Vector2 reference, Vector2 compare1, Vector2 compare2)
    {
        var check = DualMaxDistanceCheck(reference, compare1, compare2);
        var should = check < PHYSICS_ENABLE_DISTANCE;
        //Debug.Log("Should enable: " + should.ToString());
        return should;
    }


}
