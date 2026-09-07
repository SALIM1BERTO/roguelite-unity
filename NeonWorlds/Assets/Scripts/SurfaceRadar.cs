using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SurfaceRadar : MonoBehaviour
{
    public static SurfaceRadar Instance { get; private set; }

    [Header("World Pointers")]
    public float pointerRadius = 2.2f;
    public float fadeDistance = 6.0f;

    private LineRenderer tpChevron;
    private LineRenderer dropChevron;
    private LineRenderer bossChevron;

    private Material tpMat;
    private Material dropMat;
    private Material bossMat;

    private readonly Color colorTeleport = new Color(0f, 1f, 0.85f, 0.85f);  // Cyan / Mint
    private readonly Color colorDrop = new Color(1f, 0.88f, 0.25f, 0.85f);     // Gold
    private readonly Color colorBoss = new Color(1f, 0.25f, 0.35f, 0.95f);     // Crimson

    // HUD Navigation Elements
    private GameObject hudPanel;
    private RectTransform tpBadge;
    private Text tpText;
    private RectTransform tpArrow;

    private RectTransform dropBadge;
    private Text dropText;
    private RectTransform dropArrow;

    private RectTransform bossBadge;
    private Text bossText;
    private RectTransform bossArrow;

    private GravityBody gravityBody;

    void Awake()
    {
        Instance = this;
        gravityBody = GetComponent<GravityBody>();
        CreateWorldChevrons();
    }

    void Start()
    {
        BuildHUDWidgets();
    }

    void CreateWorldChevrons()
    {
        tpChevron = CreateChevronLine("Radar_TeleportChevron", colorTeleport, out tpMat);
        dropChevron = CreateChevronLine("Radar_DropChevron", colorDrop, out dropMat);
        bossChevron = CreateChevronLine("Radar_BossChevron", colorBoss, out bossMat);
    }

    LineRenderer CreateChevronLine(string objName, Color color, out Material mat)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(transform, false);

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = 5;
        lr.startWidth = 0.12f;
        lr.endWidth = 0.12f;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        mat = new Material(unlitShader != null ? unlitShader : Shader.Find("Sprites/Default"));
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive glow
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3100;
        mat.SetColor("_BaseColor", color);

        lr.sharedMaterial = mat;
        lr.enabled = false;
        return lr;
    }

    void BuildHUDWidgets()
    {
        GameObject canvas = GameObject.Find("CanvasHUD");
        if (canvas == null) return;

        // Container row centered at top below Sector label
        hudPanel = new GameObject("SurfaceRadarHUD", typeof(RectTransform));
        RectTransform rt = hudPanel.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -92);
        rt.sizeDelta = new Vector2(500, 26);

        // 1. Teleporter Badge
        tpBadge = CreateBadge(hudPanel.transform, "TeleportBadge", colorTeleport, out tpText, out tpArrow, "PORTAL", -140f);

        // 2. Drop Crate Badge
        dropBadge = CreateBadge(hudPanel.transform, "DropBadge", colorDrop, out dropText, out dropArrow, "SUPRIMENTO", 0f);

        // 3. Boss Badge
        bossBadge = CreateBadge(hudPanel.transform, "BossBadge", colorBoss, out bossText, out bossArrow, "CHEFE", 140f);
    }

    RectTransform CreateBadge(Transform parent, string name, Color color, out Text distText, out RectTransform arrowRt, string label, float xOffset)
    {
        var badgeImg = NeonUI.Panel(name, parent, new Vector2(0.5f, 0.5f), new Vector2(xOffset, 0), new Vector2(130, 24), new Color(0.02f, 0.04f, 0.07f, 0.85f), false);
        RectTransform badgeRt = badgeImg.rectTransform;

        // Accent strip on bottom
        NeonUI.Panel("Accent", badgeImg.transform, Vector2.zero, Vector2.zero, new Vector2(130, 2), color);

        // Arrow indicator
        var arrowImg = NeonUI.Panel("Arrow", badgeImg.transform, new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(12, 12), color);
        arrowRt = arrowImg.rectTransform;

        // Text display
        distText = NeonUI.Label("Distance", badgeImg.transform, label + " --", 10, color, new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(100, 20), TextAnchor.MiddleLeft);

        badgeRt.gameObject.SetActive(false);
        return badgeRt;
    }

    void LateUpdate()
    {
        if (gravityBody == null || gravityBody.planet == null)
        {
            DisableAllPointers();
            return;
        }

        Transform planet = gravityBody.planet.transform;
        Vector3 playerPos = transform.position;
        Vector3 surfaceNormal = (playerPos - planet.position).normalized;
        float planetRadius = Mathf.Abs(planet.lossyScale.x) * 0.5f;

        // --- 1. TELEPORTER TRACKING ---
        Transform targetTp = FindActiveTeleporter(planet);
        UpdateTracker(targetTp, planet, playerPos, surfaceNormal, planetRadius, tpChevron, tpMat, colorTeleport, tpBadge, tpText, tpArrow, "PORTAL");

        // --- 2. CARE PACKAGE DROP TRACKING ---
        Transform targetDrop = FindActiveDrop(planet);
        UpdateTracker(targetDrop, planet, playerPos, surfaceNormal, planetRadius, dropChevron, dropMat, colorDrop, dropBadge, dropText, dropArrow, "SUPRIM.");

        // --- 3. BOSS TRACKING ---
        Transform targetBoss = FindActiveBoss(planet);
        UpdateTracker(targetBoss, planet, playerPos, surfaceNormal, planetRadius, bossChevron, bossMat, colorBoss, bossBadge, bossText, bossArrow, "CHEFE");
    }

    Transform FindActiveTeleporter(Transform currentPlanet)
    {
        Teleporter[] tps = FindObjectsByType<Teleporter>(FindObjectsInactive.Exclude);
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var tp in tps)
        {
            if (tp == null || !tp.gameObject.activeInHierarchy) continue;
            // Validate planet assignment
            if (tp.transform.parent != null && tp.transform.parent != currentPlanet) continue;

            float d = Vector3.Distance(transform.position, tp.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = tp.transform;
            }
        }
        return closest;
    }

    Transform FindActiveDrop(Transform currentPlanet)
    {
        CarePackageDrop[] drops = FindObjectsByType<CarePackageDrop>(FindObjectsInactive.Exclude);
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var drop in drops)
        {
            if (drop == null || !drop.gameObject.activeInHierarchy) continue;
            if (drop.transform.parent != null && drop.transform.parent != currentPlanet) continue;

            float d = Vector3.Distance(transform.position, drop.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = drop.transform;
            }
        }
        return closest;
    }

    Transform FindActiveBoss(Transform currentPlanet)
    {
        if (BossLeviathan.Instance != null && BossLeviathan.Instance.gameObject.activeInHierarchy)
        {
            GravityBody bossGb = BossLeviathan.Instance.GetComponent<GravityBody>();
            if (bossGb != null && bossGb.planet != null && bossGb.planet.transform == currentPlanet)
            {
                return BossLeviathan.Instance.transform;
            }
        }
        return null;
    }

    void UpdateTracker(Transform target, Transform planet, Vector3 playerPos, Vector3 surfaceNormal, float planetRadius,
                       LineRenderer chevron, Material mat, Color baseColor,
                       RectTransform badge, Text labelText, RectTransform arrowRt, string labelName)
    {
        if (target == null)
        {
            if (chevron != null) chevron.enabled = false;
            if (badge != null && badge.gameObject.activeSelf) badge.gameObject.SetActive(false);
            return;
        }

        Vector3 toTarget = target.position - playerPos;
        Vector3 playerCenterDir = surfaceNormal;
        Vector3 targetCenterDir = (target.position - planet.position).normalized;

        float dot = Mathf.Clamp(Vector3.Dot(playerCenterDir, targetCenterDir), -1f, 1f);
        float arcAngle = Mathf.Acos(dot);
        float surfaceDist = arcAngle * planetRadius;

        Vector3 tangentDir = Vector3.ProjectOnPlane(toTarget, surfaceNormal).normalized;
        if (tangentDir.sqrMagnitude < 0.001f)
        {
            tangentDir = Vector3.ProjectOnPlane(transform.forward, surfaceNormal).normalized;
        }
        Vector3 sideDir = Vector3.Cross(surfaceNormal, tangentDir).normalized;

        // World Pointer (Chevron)
        float alpha = Mathf.Clamp01((surfaceDist - 2.5f) / (fadeDistance - 2.5f));
        if (chevron != null && alpha > 0.02f)
        {
            Vector3 center = playerPos + surfaceNormal * 0.2f;
            Vector3 tip = center + tangentDir * (pointerRadius + 0.45f);
            Vector3 left = center + tangentDir * pointerRadius - sideDir * 0.32f;
            Vector3 notch = center + tangentDir * (pointerRadius + 0.16f);
            Vector3 right = center + tangentDir * pointerRadius + sideDir * 0.32f;

            chevron.positionCount = 5;
            chevron.SetPosition(0, tip);
            chevron.SetPosition(1, left);
            chevron.SetPosition(2, notch);
            chevron.SetPosition(3, right);
            chevron.SetPosition(4, tip);

            mat.SetColor("_BaseColor", new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha));
            chevron.enabled = true;
        }
        else if (chevron != null)
        {
            chevron.enabled = false;
        }

        // HUD Badge
        if (badge != null)
        {
            if (!badge.gameObject.activeSelf) badge.gameObject.SetActive(true);

            if (labelText != null)
            {
                labelText.text = $"{labelName} {Mathf.RoundToInt(surfaceDist)}m";
            }

            if (arrowRt != null && Camera.main != null)
            {
                Vector3 screenP = Camera.main.WorldToScreenPoint(playerPos);
                Vector3 screenT = Camera.main.WorldToScreenPoint(playerPos + tangentDir * 10f);
                Vector2 sDir = (new Vector2(screenT.x, screenT.y) - new Vector2(screenP.x, screenP.y)).normalized;
                if (sDir.sqrMagnitude > 0.01f)
                {
                    float angleDeg = Mathf.Atan2(sDir.y, sDir.x) * Mathf.Rad2Deg - 90f;
                    arrowRt.localEulerAngles = new Vector3(0, 0, angleDeg);
                }
            }
        }
    }

    void DisableAllPointers()
    {
        if (tpChevron != null) tpChevron.enabled = false;
        if (dropChevron != null) dropChevron.enabled = false;
        if (bossChevron != null) bossChevron.enabled = false;

        if (tpBadge != null && tpBadge.gameObject.activeSelf) tpBadge.gameObject.SetActive(false);
        if (dropBadge != null && dropBadge.gameObject.activeSelf) dropBadge.gameObject.SetActive(false);
        if (bossBadge != null && bossBadge.gameObject.activeSelf) bossBadge.gameObject.SetActive(false);
    }
}
