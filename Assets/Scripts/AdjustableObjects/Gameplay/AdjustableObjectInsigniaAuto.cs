using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AdjustableObjectInsigniaAuto : MonoBehaviour
{
    public SpriteRenderer Holder;
    public SpriteRenderer Insignia;
    public SpriteRenderer InsigniaOverlay1;
    public SpriteRenderer InsigniaOverlay2;

    public void SetState(AdjustableObjectTargetType type, bool isOn)
    {
        var item = InventoryManager.Instance.GetTargetByTargetType(type);
        Insignia.sprite = item.alternateInWorldSprite;

        InsigniaOverlay1.gameObject.SetActive(isOn);
        InsigniaOverlay1.sprite = item.activatedSprite;
        InsigniaOverlay2.gameObject.SetActive(isOn);
    }

#if UNITY_EDITOR
    // Set this from the editor only
    public void SetSprites(AdjustableObjectTargetType type, bool showHolder=true)
    {
        try { SetState(type, false); }
        catch (NullReferenceException)
        { Debug.LogWarning("Couldn't SetState on AO Insignia because InventoryManager instance isn't available."); }
        
        EditorUtility.SetDirty(Insignia.gameObject);

        Holder.enabled = showHolder;

        if (Holder != null)
            EditorUtility.SetDirty(Holder);

        if (InsigniaOverlay1 != null)
            EditorUtility.SetDirty(InsigniaOverlay1.gameObject);

        if (InsigniaOverlay1 != null)
            EditorUtility.SetDirty(InsigniaOverlay2.gameObject);
    }
#endif
}
