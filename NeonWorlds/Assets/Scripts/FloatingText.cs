using UnityEngine;

public class FloatingText : MonoBehaviour
{
    private TextMesh textMesh;
    private float lifetime = 1f;
    private float timer = 0f;

    public void Setup(string text)
    {
        Setup(text, Color.white, 1f);
    }

    public void Setup(string text, Color color, float sizeMultiplier = 1f)
    {
        timer = 0f;
        textMesh = gameObject.GetComponent<TextMesh>();
        if (textMesh == null) {
            textMesh = gameObject.AddComponent<TextMesh>();
        }
        
        textMesh.text = text;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.5f * sizeMultiplier;
        textMesh.fontSize = Mathf.RoundToInt(24 * sizeMultiplier);
        textMesh.color = color;
        textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        if (mr != null && textMesh.font != null) {
            mr.sharedMaterial = textMesh.font.material;
        }

        FaceCamera();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
        else
        {
            Transform planet = transform.parent;
            Vector3 surfaceNormal = planet != null
                ? (transform.position - planet.position).normalized : Vector3.up;
            transform.position += surfaceNormal * 2f * Time.deltaTime;
            if (textMesh != null)
            {
                Color c = textMesh.color;
                c.a = 1f - (timer / lifetime);
                textMesh.color = c;
            }
        }
    }

    void LateUpdate()
    {
        FaceCamera();
    }

    void FaceCamera()
    {
        Camera camera = Camera.main;
        if (camera != null) transform.rotation = camera.transform.rotation;
    }
}
