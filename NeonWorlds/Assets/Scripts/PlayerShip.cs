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

        if (GetComponent<SurfaceRadar>() == null)
        {
            gameObject.AddComponent<SurfaceRadar>();
        }
    }

    public void ApplyWorldScale()
    {
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        if (Mathf.Abs(parentScale.x) < 0.0001f || Mathf.Abs(parentScale.y) < 0.0001f || Mathf.Abs(parentScale.z) < 0.0001f)
            return;

        Vector3 localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);
        if (transform.localScale != localScale) transform.localScale = localScale;
    }

    public void ApplyChassisVisual(MetaProgression.ShipChassis chassis)
    {
        if (visual == null) Configure();
        if (visual == null) visual = transform.Find("ShipVisual");
        if (visual == null) return;

        // Clean up any previously attached addon meshes
        Transform oldAddon = visual.Find("ChassisAddon");
        if (oldAddon != null) Destroy(oldAddon.gameObject);

        MeshRenderer mr = visual.GetComponent<MeshRenderer>();
        Material chassisMat = null;
        if (mr != null && mr.material != null)
        {
            chassisMat = mr.material;
        }

        GameObject addon = new GameObject("ChassisAddon");
        addon.transform.SetParent(visual, false);
        addon.transform.localPosition = Vector3.zero;
        addon.transform.localRotation = Quaternion.identity;

        switch (chassis)
        {
            case MetaProgression.ShipChassis.Interceptor:
                visual.localScale = new Vector3(0.8f, 0.8f, 0.85f);
                if (chassisMat != null)
                {
                    Color cyan = new Color(0f, 0.9f, 1f);
                    chassisMat.SetColor("_BaseColor", cyan);
                    chassisMat.EnableKeyword("_EMISSION");
                    chassisMat.SetColor("_EmissionColor", cyan * 2.5f);
                }
                break;

            case MetaProgression.ShipChassis.Titan:
                visual.localScale = new Vector3(1.15f, 0.85f, 0.85f);
                Color amber = new Color(1f, 0.55f, 0.05f);
                if (chassisMat != null)
                {
                    chassisMat.SetColor("_BaseColor", amber);
                    chassisMat.EnableKeyword("_EMISSION");
                    chassisMat.SetColor("_EmissionColor", amber * 2.5f);
                }

                // Dual heavy booster pods / armor plates on the flanks
                Material armorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                armorMat.SetColor("_BaseColor", new Color(0.2f, 0.2f, 0.25f));
                armorMat.EnableKeyword("_EMISSION");
                armorMat.SetColor("_EmissionColor", amber * 1.5f);

                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject booster = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    booster.name = "TitanArmorPlate";
                    booster.transform.SetParent(addon.transform, false);
                    booster.transform.localPosition = new Vector3(side * 0.65f, 0f, -0.2f);
                    booster.transform.localScale = new Vector3(0.35f, 0.25f, 0.7f);
                    Destroy(booster.GetComponent<Collider>());
                    booster.GetComponent<MeshRenderer>().material = armorMat;
                }
                break;

            case MetaProgression.ShipChassis.Spectre:
                visual.localScale = new Vector3(0.65f, 0.65f, 1.15f);
                Color violet = new Color(0.85f, 0.2f, 1f);
                if (chassisMat != null)
                {
                    chassisMat.SetColor("_BaseColor", violet);
                    chassisMat.EnableKeyword("_EMISSION");
                    chassisMat.SetColor("_EmissionColor", violet * 3.2f);
                }

                // Twin razor stealth winglets
                Material finMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                finMat.SetColor("_BaseColor", Color.black);
                finMat.EnableKeyword("_EMISSION");
                finMat.SetColor("_EmissionColor", violet * 2f);

                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    fin.name = "SpectreFin";
                    fin.transform.SetParent(addon.transform, false);
                    fin.transform.localPosition = new Vector3(side * 0.55f, 0.05f, 0.1f);
                    fin.transform.localRotation = Quaternion.Euler(0, side * 25f, side * 35f);
                    fin.transform.localScale = new Vector3(0.12f, 0.12f, 0.75f);
                    Destroy(fin.GetComponent<Collider>());
                    fin.GetComponent<MeshRenderer>().material = finMat;
                }
                break;

            case MetaProgression.ShipChassis.Architect:
                visual.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                Color emerald = new Color(0f, 0.95f, 0.45f);
                if (chassisMat != null)
                {
                    chassisMat.SetColor("_BaseColor", emerald);
                    chassisMat.EnableKeyword("_EMISSION");
                    chassisMat.SetColor("_EmissionColor", emerald * 2.5f);
                }

                // Rotating orbital energy halo ring
                GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                halo.name = "ArchitectHalo";
                halo.transform.SetParent(addon.transform, false);
                halo.transform.localPosition = new Vector3(0f, 0.15f, 0f);
                halo.transform.localScale = new Vector3(1.6f, 0.025f, 1.6f);
                Destroy(halo.GetComponent<Collider>());

                Material haloMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                haloMat.SetColor("_BaseColor", emerald);
                haloMat.EnableKeyword("_EMISSION");
                haloMat.SetColor("_EmissionColor", emerald * 4f);
                halo.GetComponent<MeshRenderer>().material = haloMat;

                halo.AddComponent<ArchitectHaloRotator>();
                break;
        }
    }
}

public class ArchitectHaloRotator : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(0, 120f * Time.deltaTime, 0);
    }
}
