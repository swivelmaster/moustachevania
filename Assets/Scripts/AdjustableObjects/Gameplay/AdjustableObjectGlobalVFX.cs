using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustableObjectGlobalVFX : MonoBehaviour
{
    [SerializeField]
    private AnimationCurve RotationScaleCurve;
    public AnimationCurve rotationScaleCurve { get { return RotationScaleCurve; } }

    [SerializeField]
    private GameObject _GenericChangeVFXBox;
    public GameObject GenericChangeVFXBox { get { return _GenericChangeVFXBox; } }

    [SerializeField]
    private AnimationCurve MorphMagnitudeCurve;
    public AnimationCurve morphmagnitudeCurve { get { return MorphMagnitudeCurve; } }

    [SerializeField]
    private AnimationCurve ScaleCurve;
    public AnimationCurve scaleCurve { get { return ScaleCurve; } }


    [SerializeField]
    private AnimationCurve BoucnceCurve;
    public AnimationCurve bounceCurve {get {return BoucnceCurve; }}
}
