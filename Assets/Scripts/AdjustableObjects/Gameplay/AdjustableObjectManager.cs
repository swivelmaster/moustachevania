using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustableObjectManager : MonoBehaviour
{
    public const float VerySlowSpeed = 1f;
    public const float SlowSpeed = 2f;
    public const float MediumSpeed = 3f;
    public const float FastSpeed = 4f;
    public const float VeryFastSpeed = 5f;

    public const float BounceVelocity = 15f;

    public static AdjustableObjectManager Instance;

    [SerializeField]
    private AdjustableObjectGlobalVFX _GlobalVFX;
    public AdjustableObjectGlobalVFX VFX { get { return _GlobalVFX; } }

    [Header("false = SquareA, true = SquareB")]
    public bool ActiveSquare = false;

    public Color NoClipColor;

    [SerializeField]
    private ParticleSystem ExplodingSolidAdjustableParticlePrefab = null;

    public bool DebugModifiersOn = false;
    public AdjustableObjectModifierConfiguration DebugModifiers;

    [SerializeField]
    private InventoryManager inventoryManager = null;

    public LayerMask AdjustableObjectLayerMask;

    // Need empty so we can populate adjustable objects with no modifiers
    // with empty modifiers to override just in case a charm was unequipped
    AdjustableObjectModifierConfiguration empty = new AdjustableObjectModifierConfiguration();

    public int PlatformLayer { private set; get; }
    public int InactiveLayer { private set; get; }

    private Dictionary<MovementSpeedConfig, int> MovementSpeedIntLookup = new Dictionary<MovementSpeedConfig, int>
    {
        { MovementSpeedConfig.VerySlow, 1 },
        { MovementSpeedConfig.Slow, 2 },
        { MovementSpeedConfig.Medium, 3 },
        { MovementSpeedConfig.Fast, 4 },
        { MovementSpeedConfig.VeryFast, 5 },
    };

    private Dictionary<GameObject, AdjustableObject> GOToAOLookup = new Dictionary<GameObject, AdjustableObject>();

    private List<AdjustableObject> AdjustableObjects;

    private void Awake()
    {
        AdjustableObjects = new List<AdjustableObject>();
        Instance = this;

        PlatformLayer = LayerMask.NameToLayer("Platforms");
        InactiveLayer = LayerMask.NameToLayer("InactivePhsyics");
    }

    private void Start()
    {
        inventoryManager.onCharmsUpdated.AddListener(InitTargetsWithModifiers);
        StartCoroutine(SetAdjustableObjectsModifiers());
    }

    IEnumerator SetAdjustableObjectsModifiers()
    {
        yield return new WaitForEndOfFrame();

        if (DebugModifiersOn)
        {
            foreach (AdjustableObject o in AdjustableObjects)
            {
                o.InitConfiguation(DebugModifiers, true);
            }
        }
        else
        {
            InitTargetsWithModifiers(new CharmChange(false, false, false));
        }
    }

    void InitTargetsWithModifiers(CharmChange charmChange)
    {
        InitTargetsWithModifiers(false);
    }

    void InitTargetsWithModifiers(bool squaresOnly)
    {
        if (InventoryManager.Instance.CharmSlots[0] == null)
        {
            Debug.Log("Charm slots not ready yet.");
            return;
        }

        var activeSquare = GetActiveSquare();

        // Create a dict of all used target types and then collapse modifier configuration into them
        // This is because the user can collect duplicate targets and the modifiers need to stack.
        var targetConfigurations = new Dictionary<AdjustableObjectTargetType, AdjustableObjectModifierConfiguration>();
        foreach (var slot in inventoryManager.CharmSlots)
        {
            if (slot.TargetId == 0 || slot.ModifierId == 0)
                continue;

            var target = inventoryManager.GetTargetById(slot.TargetId);
            if (!targetConfigurations.ContainsKey(target.targetType))
                targetConfigurations[target.targetType] = new AdjustableObjectModifierConfiguration();

            targetConfigurations[target.targetType].AddModifier(inventoryManager.GetModifierById(slot.ModifierId).modifierType);
        }

        foreach (AdjustableObject o in AdjustableObjects)
        {
            if (squaresOnly && !o.IsSquareType())
                continue;

            if (targetConfigurations.ContainsKey(o.TargetType))
                o.InitConfiguation(targetConfigurations[o.TargetType], true);
            else
                o.InitConfiguation(empty, o.TargetType == activeSquare);
        }
    }

    public void AdvanceFrame()
    {
        foreach (var ao in AdjustableObjects)
            ao.AdvanceFrame();

        HandleBouncedAOAnimations();
    }

    public void PhysicsStep()
    {
        foreach (var ao in AdjustableObjects)
            ao.PhysicsStep();
    }

    public AdjustableObjectTargetType GetActiveSquare()
    {
        return ActiveSquare ? AdjustableObjectTargetType.SquareB : AdjustableObjectTargetType.SquareA;
    }

    public MovementSpeedConfig GetMovementSpeedFromInt(int speed)
    {
        if (speed <= 1)
            return MovementSpeedConfig.VerySlow;

        switch (speed)
        {
            case 2:
                return MovementSpeedConfig.Slow;
            case 3:
                return MovementSpeedConfig.Medium;
            case 4:
                return MovementSpeedConfig.Fast;
            case 5:
                return MovementSpeedConfig.VeryFast;
        }

        return MovementSpeedConfig.VeryFast;
    }

    public int GetIntFromMovementSpeed(MovementSpeedConfig config)
    {
        return MovementSpeedIntLookup[config];
    }

    public void RegisterAdjustableObject(AdjustableObject o)
    {
        AdjustableObjects.Add(o);

        RegisterGameObjectsRecursive(o, o.transform);
    }

    // Need to do this for children of AO's, which wil be tagged as AO's
    // but won't have the component, so we need to track the parent component
    // for each one. Also this will speed up the lookup at run-time so hey.
    void RegisterGameObjectsRecursive(AdjustableObject o, Transform transform)
    {
        //Debug.Log("register recursive called with go " + o.gameObject.name);
        GOToAOLookup[transform.gameObject] = o;

        for (int i=0;i<transform.childCount;i++)
        {
            RegisterGameObjectsRecursive(o, transform.GetChild(i));
        }

        // Need to register INDIVIDUAL BLOCKS because we're not using
        // composite collider... temporarily.
        // todo: not this
        foreach (var block in o.temp_GetGridItems())
            GOToAOLookup[block.gameObject] = o;
    }

    public void AdustableObjectConfigUpdated()
    {
        InitTargetsWithModifiers(new CharmChange(true, true, true));
    }

    public void SpawnSolidBreakableSystem(Vector3 sourcePosition, Quaternion sourceRotation, SpriteRenderer sourceSprite, Mesh sourceMesh)
    {
        ParticleSystem system = Instantiate(
            ExplodingSolidAdjustableParticlePrefab,
            sourcePosition,
            ExplodingSolidAdjustableParticlePrefab.transform.rotation
        );

        var color = system.main.startColor;
        color.color = SpriteUtil.GetAverageColorForSprite(sourceSprite.sprite);

        var main = system.main;
        main.startColor = color;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Mesh;
        shape.mesh = sourceMesh;
    }

    public AdjustableObject GetAdjustableObjectFromGameObject(GameObject go)
    {
        // This should never happen... theoretically
        // ... but until we have the best tools for managing this,
        // some game objects with no ao associated mijght slip through the
        // cracks. (Also if something with a collider gets mis-tagged)
        if (!GOToAOLookup.ContainsKey(go))
            return null;

        return GOToAOLookup[go];
    }

    public void FlipSquares()
    {
        ActiveSquare = !ActiveSquare;
        InitTargetsWithModifiers(true);
    }

    Dictionary<Transform, float> BouncedAOAnimations = new Dictionary<Transform, float>();
    const float BOUNCED_AO_BOUNCE_DURATION = .25f;

    /// <summary>
    /// Call when player bounces off a bounceable AdjustableObject
    /// Currently just handles bounce animation (player sound effect controller
    /// handles sound effect)
    /// </summary>
    /// <param name="BouncedCollider"></param>
    public void ColliderBounced(BoxCollider2D BouncedCollider)
    {
        BouncedAOAnimations[BouncedCollider.transform] = GameplayManager.Instance.GameTime;
    }

    public void HandleBouncedAOAnimations()
    {
        List<Transform> toRemove = new List<Transform>();
        foreach (var kv in BouncedAOAnimations)
        {
            if (HandleBouncedAOAnimation(kv))
                toRemove.Add(kv.Key);
        }

        foreach (var t in toRemove)
            BouncedAOAnimations.Remove(t);
    }

    public bool HandleBouncedAOAnimation(KeyValuePair<Transform, float> animation)
    {
        if (animation.Value + BOUNCED_AO_BOUNCE_DURATION < GameplayManager.Instance.GameTime)
        {
            // Animation complete
            animation.Key.localScale = Vector2.one;
            return true;
        }

        animation.Key.localScale = new Vector2(1f,
            VFX.bounceCurve.Evaluate(
                (GameplayManager.Instance.GameTime - animation.Value)
                    / BOUNCED_AO_BOUNCE_DURATION));

        return false;
    }
}

[System.Serializable]
public class AdjustableObjectConfiguration : IEquatable<AdjustableObjectConfiguration>
{
    public bool Clip = true;
    public bool Bounce = false;
    public bool Breakable = false;
    public bool Damages = false;

    public int Rotation = 0;

    public MovementConfig Movement;
    public MovementSpeedConfig Speed;

    public override bool Equals(object obj)
    {
        return Equals(obj as AdjustableObjectConfiguration);
    }

    public bool Equals(AdjustableObjectConfiguration other)
    {
        return other != null &&
               Clip == other.Clip &&
               Bounce == other.Bounce &&
               Breakable == other.Breakable &&
               Damages == other.Damages &&
               Movement == other.Movement &&
               Speed == other.Speed &&
               Rotation == other.Rotation;
    }

    public AdjustableObjectConfiguration GetDupe()
    {
        AdjustableObjectConfiguration config = new AdjustableObjectConfiguration();

        config.Clip = Clip;
        config.Bounce = Bounce;
        config.Breakable = Breakable;
        config.Damages = Damages;

        config.Movement = Movement;
        config.Speed = Speed;

        config.Rotation = Rotation;

        return config;
    }

    public override int GetHashCode()
    {
        int hashCode = -1190086831;
        hashCode = hashCode * -1521134295 + Clip.GetHashCode();
        hashCode = hashCode * -1521134295 + Bounce.GetHashCode();
        hashCode = hashCode * -1521134295 + Breakable.GetHashCode();
        hashCode = hashCode * -1521134295 + Damages.GetHashCode();
        hashCode = hashCode * -1521134295 + Movement.GetHashCode();
        hashCode = hashCode * -1521134295 + Speed.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(AdjustableObjectConfiguration left, AdjustableObjectConfiguration right)
    {
        return EqualityComparer<AdjustableObjectConfiguration>.Default.Equals(left, right);
    }

    public static bool operator !=(AdjustableObjectConfiguration left, AdjustableObjectConfiguration right)
    {
        return !(left == right);
    }
}

[System.Serializable]
public class AdjustableObjectModifierConfiguration
{
    public int RotationAdjust = 0;
    public bool RotationAdjustContinuous = false;
    public bool Stop = false;

    // ints instead of bool because stacking works
    public int Faster = 0;
    public int Slower = 0;

    public bool Bouncy = false;
    public bool AntiBounce = false;

    public int RangeModifier = 0;

    public bool NoClip = false;
    public bool ReClip = false;
    public bool Breakable = false;
    public bool AntiBreakable = false;
    public bool Restore = false;
    public bool Harmless = false;

    // Chain together modifiers, cool.
    public AdjustableObjectModifierConfiguration AddModifier(AdjustableObjectModifierType type)
    {
        switch (type)
        {
            case AdjustableObjectModifierType.Bouncy:
                Bouncy = true;
                break;
            case AdjustableObjectModifierType.Breakable:
                Breakable = true;
                break;
            case AdjustableObjectModifierType.Faster:
                Faster += 1;
                break;
            case AdjustableObjectModifierType.Harmless:
                Harmless = true;
                break;
            case AdjustableObjectModifierType.NoClip:
                NoClip = true;
                break;
            case AdjustableObjectModifierType.NotBouncy:
                AntiBounce = true;
                break;
            case AdjustableObjectModifierType.Restore:
                Restore = true;
                break;
            case AdjustableObjectModifierType.RotateLeft:
                RotationAdjust += 1;
                break;
            case AdjustableObjectModifierType.RotateRight:
                RotationAdjust -= 1;
                break;
            case AdjustableObjectModifierType.Slower:
                Faster -= 1;
                break;
            case AdjustableObjectModifierType.ReClip:
                ReClip = true;
                break;
        }

        return this;
    }

    public AdjustableObjectModifierConfiguration AddModifier(AdjustableObjectModifierConfiguration config)
    {
        RotationAdjust += config.RotationAdjust;
        RotationAdjustContinuous = RotationAdjustContinuous || config.RotationAdjustContinuous;
        Stop = Stop || config.Stop;

        Faster += config.Faster;
        Slower += config.Slower;

        Bouncy = Bouncy || config.Bouncy;
        AntiBounce = AntiBounce || config.AntiBounce;

        RangeModifier += config.RangeModifier;

        NoClip = NoClip || config.NoClip;
        ReClip = ReClip || config.ReClip;

        Breakable = Breakable || config.Breakable;
        AntiBreakable = AntiBreakable || config.AntiBreakable;
        Restore = Restore || config.Restore;
        Harmless = Harmless || config.Harmless;


        return this;
    }
}

public enum MovementConfig
{
    None, HorizontalStartLeft, HorizontalStartRight, VerticalStartUp, VerticalStartDown
}

public enum MovementSpeedConfig
{
    VerySlow, Slow, Medium, Fast, VeryFast
}
