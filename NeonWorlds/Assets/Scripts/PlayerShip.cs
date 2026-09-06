using UnityEngine;

/// <summary>Keeps the ship and its hitbox in world units on uniformly scaled planets.</summary>
[DisallowMultipleComponent]
public class PlayerShip : MonoBehaviour
{
    [Header("Appearance (world units)")]
    public Transform visual;
    [Min(0.01f)] public float visualScale = 0.8f;
    [Min(0.01f)] public float colliderRadius = 0.4f;
    [Min(0.02f)] public float colliderHeight = 0.8f;
    [Min(0f)] public float surfaceOffset = 0.5f;

    void Awake()
    {
        Configure();
    }

    void OnTransformParentChanged()
    {
        ApplyWorldScale();
    }

    void LateUpdate()
    {
        // Also handles a planet's scale changing after a teleport.
        ApplyWorldScale();
    }

    public void Configure()
    {
        if (visual == null)
        {
            visual = transform.Find("ShipVisual");
            if (visual == null)
            {
                MeshFilter sourceMesh = GetComponent<MeshFilter>();
                MeshRenderer sourceRenderer = GetComponent<MeshRenderer>();
                if (sourceMesh != null && sourceMesh.sharedMesh != null && sourceRenderer != null)
                {
                    GameObject ship = new GameObject("ShipVisual");
                    visual = ship.transform;
                    visual.SetParent(transform, false);
                    ship.AddComponent<MeshFilter>().sharedMesh = sourceMesh.sharedMesh;
                    ship.AddComponent<MeshRenderer>().sharedMaterials = sourceRenderer.sharedMaterials;
                }
            }
        }

        if (visual != null)
        {
            MeshRenderer oldRenderer = GetComponent<MeshRenderer>();
            if (oldRenderer != null) oldRenderer.enabled = false;
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one * visualScale;

            // Preserve previous art for reference, but render only the arrow ship.
            string[] oldVisuals = { "UfoBody", "Cockpit", "ShipBody", "LeftWing", "RightWing" };
            foreach (string childName in oldVisuals)
            {
                Transform child = transform.Find(childName);
                if (child != null && child != visual) child.gameObject.SetActive(false);
            }
        }

        ApplyWorldScale();
        CapsuleCollider hitbox = GetComponent<CapsuleCollider>();
        if (hitbox != null)
        {
            hitbox.center = Vector3.zero;
            hitbox.direction = 1;
            hitbox.radius = colliderRadius;
            hitbox.height = Mathf.Max(colliderHeight, colliderRadius * 2f);
        }

        GravityBody gravityBody = GetComponent<GravityBody>();
        if (gravityBody != null) gravityBody.surfaceOffset = surfaceOffset;
    }

    public void ApplyWorldScale()
    {
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        if (Mathf.Abs(parentScale.x) < 0.0001f || Mathf.Abs(parentScale.y) < 0.0001f || Mathf.Abs(parentScale.z) < 0.0001f)
            return;

        Vector3 localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);
        if (transform.localScale != localScale) transform.localScale = localScale;
    }
}
