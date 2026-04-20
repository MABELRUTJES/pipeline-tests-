using UnityEngine;
using UnityEngine.InputSystem;

public class PaintGun : MonoBehaviour
{
    [SerializeField] private Transform barrelTip;
    [SerializeField] private float range = 50f;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private LayerMask ignoreLayers = 0;
    [SerializeField] private string unpaintableTag = "Unpaintable";
    [SerializeField] private Color paintColor = Color.red;
    [SerializeField] private float aimRadius = 0.5f;
    [SerializeField] private bool drawGizmosRay = true;
    [SerializeField] private Color gizmoColor = Color.magenta;

    private void Awake()
    {
        if (barrelTip != null)
            return;

        // Try to auto-find a child named "BarrelTip".
        var all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == "BarrelTip")
            {
                barrelTip = all[i];
                break;
            }
        }
    }

    public void OnAttack(InputValue value)
    {
        if (!value.isPressed)
            return;

        if (barrelTip == null)
            return;

        var ray = new Ray(barrelTip.position, barrelTip.forward);

        Debug.DrawRay(ray.origin, ray.direction * range, gizmoColor, 0.15f);

        RaycastHit hit;
        bool hasHit = aimRadius > 0f
            ? Physics.SphereCast(ray, aimRadius, out hit, range, hitMask, QueryTriggerInteraction.Collide)
            : Physics.Raycast(ray, out hit, range, hitMask, QueryTriggerInteraction.Collide);

        if (!hasHit || hit.collider == null)
            return;

        if (hit.collider.transform.IsChildOf(transform))
            return;

        int layerBit = 1 << hit.collider.gameObject.layer;
        if ((ignoreLayers.value & layerBit) != 0)
            return;

        if (!string.IsNullOrEmpty(unpaintableTag) && HasTagInParents(hit.collider.transform, unpaintableTag))
            return;

        var r = hit.collider.GetComponent<Renderer>();
        if (r == null)
            r = hit.collider.GetComponentInParent<Renderer>();
        if (r == null)
            r = hit.collider.GetComponentInChildren<Renderer>();
        if (r == null)
            return;

        var mat = r.material;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", paintColor);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", paintColor);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmosRay || barrelTip == null)
            return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(barrelTip.position, barrelTip.position + barrelTip.forward * range);
    }

    private static bool HasTagInParents(Transform t, string tagToMatch)
    {
        while (t != null)
        {
            if (string.Equals(t.tag, tagToMatch, System.StringComparison.OrdinalIgnoreCase))
                return true;
            t = t.parent;
        }

        return false;
    }
}
