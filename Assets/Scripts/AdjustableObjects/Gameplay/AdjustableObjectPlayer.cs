using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustableObjectPlayer : MonoBehaviour
{
    const float RESTORABLE_CHECK_INCREMENT = .5f;
    const float RESTORABLE_CHECK_WIDTH = 15f;
    const float RESTORATBLE_CHECK_HEIGHT = 9f;

    void Start()
    {
        StartCoroutine(CheckForRestorables());
    }

    IEnumerator CheckForRestorables()
    {
        bool found = false;

        while (true)
        {
            yield return new WaitForSeconds(RESTORABLE_CHECK_INCREMENT);

            found = false;

            foreach (var id in InventoryManager.Instance.GetEquippedModifiers())
            {
                var inventoryItem = InventoryManager.Instance.GetModifierById(id);
                if (inventoryItem.modifierType == AdjustableObjectModifierType.Restore)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                continue;
            }

            var result = Physics2D.OverlapBoxAll(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(RESTORABLE_CHECK_WIDTH, RESTORATBLE_CHECK_HEIGHT),
                0f, AdjustableObjectManager.Instance.AdjustableObjectLayerMask);

            foreach (Collider2D collider in result)
            {
                AdjustableObject o = collider.GetComponent<AdjustableObject>();
                if (o == null)
                    continue;

                if (o.IsBroken())
                {
                    o.Restore();
                }
            }
        }
    }
}
