using UnityEngine;

public class Orbit : MonoBehaviour
{
    public Transform center;
    public float speed = 10f; // Graus por segundo
    
    [Header("Linha Visual da Órbita")]
    public bool showOrbitLine = true;
    public float lineWidth = 1.2f;
    public Color orbitColor = Color.clear;

    private float currentAngle = 0f;
    private float radius = 0f;
    private GameObject orbitLineObj;
    private LineRenderer lineRenderer;

    public static bool globalOrbitsVisible = true;

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

            // Desativa as linhas antigas estaticas com escala/raio incorretos
            CleanOldEditorOrbitLines();

            if (showOrbitLine)
            {
                CreateOrbitLine();
            }
        }
    }

    void Update()
    {
        // Tecla de atalho 'O' para alternar a visualizacao das orbitas
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.oKey.wasPressedThisFrame)
        {
            ToggleAllOrbits();
        }
    }

    public static void ToggleAllOrbits()
    {
        globalOrbitsVisible = !globalOrbitsVisible;
        Orbit[] orbits = FindObjectsByType<Orbit>(FindObjectsInactive.Include);
        foreach (var orb in orbits)
        {
            if (orb.orbitLineObj != null)
            {
                orb.orbitLineObj.SetActive(globalOrbitsVisible);
            }
        }
        Debug.Log("Visibilidade das orbitas: " + (globalOrbitsVisible ? "LIGADA" : "DESLIGADA"));
    }

    void CleanOldEditorOrbitLines()
    {
        if (center != null)
        {
            for (int i = center.childCount - 1; i >= 0; i--)
            {
                Transform child = center.GetChild(i);
                if (child.name.StartsWith("OrbitLine_"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    void CreateOrbitLine()
    {
        if (radius <= 0.1f || center == null) return;

        Color ringColor = orbitColor;
        if (ringColor.a <= 0.01f)
        {
            ringColor = GetPlanetDefaultColor();
        }

        orbitLineObj = new GameObject("DynamicOrbit_" + gameObject.name);
        orbitLineObj.transform.position = Vector3.zero;
        orbitLineObj.transform.rotation = Quaternion.identity;

        lineRenderer = orbitLineObj.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.positionCount = 128;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.castShadows = false;
        lineRenderer.receiveShadows = false;

        // Material Neon Holografico Aditivo
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader != null)
        {
            Material mat = new Material(unlitShader);
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive neon glow
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.SetColor("_BaseColor", ringColor);
            lineRenderer.sharedMaterial = mat;
        }

        // Calcula os 128 pontos do circulo perfeito no plano orbital (XZ)
        Vector3 centerPos = center.position;
        float yPos = centerPos.y;
        for (int i = 0; i < 128; i++)
        {
            float angle = i * Mathf.PI * 2f / 128f;
            Vector3 point = new Vector3(centerPos.x + Mathf.Cos(angle) * radius, yPos, centerPos.z + Mathf.Sin(angle) * radius);
            lineRenderer.SetPosition(i, point);
        }

        orbitLineObj.SetActive(globalOrbitsVisible);
    }

    Color GetPlanetDefaultColor()
    {
        // Paleta neon correspondente a cada planeta
        if (name.Contains("1")) return new Color(0.15f, 0.85f, 1f, 0.45f);   // Ciano Neon
        if (name.Contains("2")) return new Color(1f, 0.35f, 0.75f, 0.45f);  // Magenta / Rosa Choque
        if (name.Contains("3")) return new Color(0.25f, 1f, 0.5f, 0.45f);   // Esmeralda / Verde Neon
        if (name.Contains("4")) return new Color(1f, 0.65f, 0.15f, 0.45f);  // Ambar / Laranja Plasma
        if (name.Contains("5")) return new Color(0.55f, 0.4f, 1f, 0.45f);   // Violeta Eletrico
        if (name.Contains("6")) return new Color(1f, 0.9f, 0.25f, 0.45f);   // Ouro Solar
        return new Color(0.2f, 0.8f, 1f, 0.45f);
    }

    void OnDrawGizmos()
    {
        if (center != null)
        {
            Vector3 offset = transform.position - center.position;
            float r = new Vector2(offset.x, offset.z).magnitude;
            if (r > 0.5f)
            {
                Gizmos.color = GetPlanetDefaultColor();
                Vector3 centerPos = center.position;
                Vector3 prev = centerPos + new Vector3(r, 0, 0);
                for (int i = 1; i <= 72; i++)
                {
                    float a = i * Mathf.PI * 2f / 72f;
                    Vector3 next = centerPos + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                    Gizmos.DrawLine(prev, next);
                    prev = next;
                }
            }
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
