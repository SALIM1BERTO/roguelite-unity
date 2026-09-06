using UnityEngine;

public class Orbit : MonoBehaviour
{
    public Transform center;
    public float speed = 10f; // Graus por segundo
    
    private float currentAngle = 0f;
    private float radius = 0f;

    void Start()
    {
        // Certifica de nao ter Rigidbody para nao dar double-push (bug da fisica)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        if (center)
        {
            Vector3 offset = transform.position - center.position;
            radius = new Vector2(offset.x, offset.z).magnitude;
            currentAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        }
    }

    void FixedUpdate()
    {
        if (center && radius > 0f)
        {
            currentAngle += speed * Time.fixedDeltaTime;
            float rad = currentAngle * Mathf.Deg2Rad;
            
            // Move matematicamente
            transform.position = center.position + new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);
        }
    }
}
