using UnityEngine;

public class GravityBody : MonoBehaviour
{
    public PlanetGravity planet;
    [Min(0f)] public float surfaceOffset = 0.5f;

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    void Update()
    {
        if (planet != null)
        {
            SnapToSurface();

            // Orientacao
            Vector3 localUp = transform.localPosition.normalized;
            Vector3 worldUp = transform.parent.TransformDirection(localUp);
            
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, worldUp) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 50f * Time.deltaTime);
        }
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
    }
}

