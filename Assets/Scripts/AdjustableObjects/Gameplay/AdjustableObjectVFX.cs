using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustableObjectVFX : MonoBehaviour
{
    [SerializeField]
    private AdjustableObjectDrawGrid MyGrid;

    public const string MORPH_PROPERTY_NAME = "RippleAmount";

    public void RotationStart()
    {
        var prefab = AdjustableObjectManager.Instance.VFX.GenericChangeVFXBox;
        foreach (var sprite in MyGrid.SpriteRenderers)
        {
            Instantiate(prefab, sprite.transform.position, prefab.transform.rotation, sprite.transform);
        }
    }

    public void SetMorphProgress(float progress)
    {
        float curveAmount = AdjustableObjectManager.Instance.VFX.morphmagnitudeCurve.Evaluate(progress);
        float scaleAmount = AdjustableObjectManager.Instance.VFX.scaleCurve.Evaluate(progress);
        Vector2 scale = new Vector2(scaleAmount, scaleAmount);
        foreach (var renderer in MyGrid.SpriteRenderers)
        {
            renderer.material.SetFloat(MORPH_PROPERTY_NAME, curveAmount);
            renderer.transform.localScale = scale;
        }
    }
}
