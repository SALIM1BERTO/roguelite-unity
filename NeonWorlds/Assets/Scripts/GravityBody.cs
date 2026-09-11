using UnityEngine;

public class GravityBody : MonoBehaviour
{
    public PlanetGravity planet;
    [Min(0f)] public float surfaceOffset = 0.5f;

    // Snap throttle: enemies set localPosition directly, so we only need orientation
    private bool needsSnap = true;
    private Vector3 lastLocalPos;

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
        if (planet != null) SnapToSurface();
    }

    void Update()
    {
        if (planet == null) return;

        // Only re-snap if localPosition hasn't been set externally (e.g. by Enemy.Update)
        // Check if position drifted from surface — avoids redundant transform writes
        if (needsSnap || transform.localPosition != lastLocalPos)
        {
            needsSnap = false;
            SnapToSurface();
        }

        // Orientation only — very cheap
        Vector3 localUp = transform.localPosition.normalized;
        if (localUp.sqrMagnitude < 0.01f) return;
        Vector3 worldUp = transform.parent != null ? transform.parent.TransformDirection(localUp) : localUp;
        
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, worldUp) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 50f * Time.deltaTime);
        lastLocalPos = transform.localPosition;
    }

    public void SnapToSurface()
    {
        if (planet == null) return;
        Transform planetTransform = planet.transform;
        if (transform.parent != planetTransform) transform.SetParent(planetTransform, true);

        float scale = Mathf.Abs(planetTransform.lossyScale.x);
        if (scale < 0.0001f) return;
        Vector3 normal = (transform.position - planetTransform.position).normalized;
        if (normal.sqrMagnitude < 0.01f) normal = planetTransform.up;
        // The planets are uniformly scaled primitive spheres (local radius 0.5).
        transform.position = planetTransform.position + normal * (0.5f * scale + surfaceOffset);
        lastLocalPos = transform.localPosition;
        needsSnap = false;
    }
}

