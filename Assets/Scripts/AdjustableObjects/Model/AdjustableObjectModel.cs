using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

[System.Serializable]
public class AdjustableObjectTarget
{
    public int id;
    public AdjustableObjectTargetType TargetType;

    public AdjustableObjectTarget(int id, AdjustableObjectTargetType targetType)
    {
        if (id == 0)
            throw new InvalidEnumArgumentException("Object id cannot be 0.");

        this.id = id;
        this.TargetType = targetType;
    }
}

[System.Serializable]
public class AdjustableObjectModifier
{
    public int id;
    public AdjustableObjectModifierType ModifierType;

    public AdjustableObjectModifier(int id, AdjustableObjectModifierType modiferType)
    {
        if (id == 0)
            throw new InvalidEnumArgumentException("Object id cannot be 0.");

        this.id = id;
        this.ModifierType = modiferType;
    }
}

[System.Serializable]
public class AdjustableObjectEnhancer
{
    public int id;
    public AdjustableObjectEnhancerType EnhancerType;

    public AdjustableObjectEnhancer(int id, AdjustableObjectEnhancerType enhancerType)
    {
        if (id == 0)
            throw new InvalidEnumArgumentException("Object id cannot be 0.");

        this.id = id;
        this.EnhancerType = enhancerType;
    }
}

public enum AdjustableObjectTargetType
{
    Circle, Diamond, Triangle, SquareA, SquareB
}

public enum AdjustableObjectModifierType
{
    RotateRight, RotateLeft,
    Stop, Faster, Slower,
    Bouncy, NotBouncy,
    NoClip,
    Breakable,
    Restore,
    Harmless,
    ReClip
}

public enum AdjustableObjectEnhancerType
{
    KeepRotating,

    // Affects bouncy and speed
    Double
}