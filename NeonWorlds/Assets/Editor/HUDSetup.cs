using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if NEONWORLDS_LEGACY_AUTO_SETUP
[InitializeOnLoad]
#endif
public class HUDSetup
{
    static HUDSetup()
    {
        EditorApplication.delayCall += ApplyHUD;
    }

    static void ApplyHUD()
    {
        if (EditorPrefs.GetBool("NeonWorlds_HUD", false)) return;

        // 1. Apagar Canvas antigo se existir
        GameObject oldCanvas = GameObject.Find("Canvas");
        if (oldCanvas) Object.DestroyImmediate(oldCanvas);
        GameObject oldES = GameObject.Find("EventSystem");
        if (oldES) Object.DestroyImmediate(oldES);

        // 2. EventSystem (Removido por enquanto, já que não temos botões clicáveis,
        // para evitar conflitos com o Novo Input System).

        // 3. Criar Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // 4. Barra de XP (Topo da Tela)
        GameObject xpBgObj = new GameObject("XP_Background");
        xpBgObj.transform.SetParent(canvasObj.transform, false);
        Image xpBg = xpBgObj.AddComponent<Image>();
        xpBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        RectTransform xpBgRect = xpBgObj.GetComponent<RectTransform>();
        xpBgRect.anchorMin = new Vector2(0, 1);
        xpBgRect.anchorMax = new Vector2(1, 1);
        xpBgRect.pivot = new Vector2(0.5f, 1);
        xpBgRect.anchoredPosition = new Vector2(0, 0);
        xpBgRect.sizeDelta = new Vector2(0, 30); // Estica horizontal, 30px altura

        GameObject xpFillObj = new GameObject("XP_Fill");
        xpFillObj.transform.SetParent(xpBgObj.transform, false);
        Image xpFill = xpFillObj.AddComponent<Image>();
        xpFill.color = new Color(0f, 1f, 0.2f, 1f); // Verde Neon
        RectTransform xpFillRect = xpFillObj.GetComponent<RectTransform>();
        xpFillRect.anchorMin = new Vector2(0, 0);
        xpFillRect.anchorMax = new Vector2(1, 1);
        xpFillRect.pivot = new Vector2(0, 0.5f);
        xpFillRect.anchoredPosition = new Vector2(0, 0);
        xpFillRect.sizeDelta = new Vector2(0, 0);
        xpFill.type = Image.Type.Filled;
        xpFill.fillMethod = Image.FillMethod.Horizontal;
        xpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        xpFill.fillAmount = 0f;

        // 5. Barra de Vida (Centro Inferior)
        GameObject hpBgObj = new GameObject("HP_Background");
        hpBgObj.transform.SetParent(canvasObj.transform, false);
        Image hpBg = hpBgObj.AddComponent<Image>();
        hpBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        RectTransform hpBgRect = hpBgObj.GetComponent<RectTransform>();
        hpBgRect.anchorMin = new Vector2(0.5f, 0);
        hpBgRect.anchorMax = new Vector2(0.5f, 0);
        hpBgRect.pivot = new Vector2(0.5f, 0);
        hpBgRect.anchoredPosition = new Vector2(0, 50);
        hpBgRect.sizeDelta = new Vector2(400, 25);

        GameObject hpFillObj = new GameObject("HP_Fill");
        hpFillObj.transform.SetParent(hpBgObj.transform, false);
        Image hpFill = hpFillObj.AddComponent<Image>();
        hpFill.color = new Color(1f, 0f, 0.2f, 1f); // Vermelho Neon
        RectTransform hpFillRect = hpFillObj.GetComponent<RectTransform>();
        hpFillRect.anchorMin = new Vector2(0, 0);
        hpFillRect.anchorMax = new Vector2(1, 1);
        hpFillRect.pivot = new Vector2(0, 0.5f);
        hpFillRect.anchoredPosition = new Vector2(0, 0);
        hpFillRect.sizeDelta = new Vector2(0, 0);
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillAmount = 1f;

        // 6. Texto do Level (Centro Superior)
        GameObject levelTextObj = new GameObject("Level_Text");
        levelTextObj.transform.SetParent(canvasObj.transform, false);
        Text levelText = levelTextObj.AddComponent<Text>();
        levelText.text = "LEVEL 1";
        levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        levelText.fontSize = 40;
        levelText.color = Color.white;
        levelText.alignment = TextAnchor.MiddleCenter;
        levelText.fontStyle = FontStyle.Bold;
        RectTransform levelRect = levelTextObj.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0.5f, 1);
        levelRect.anchorMax = new Vector2(0.5f, 1);
        levelRect.pivot = new Vector2(0.5f, 1);
        levelRect.anchoredPosition = new Vector2(0, -40);
        levelRect.sizeDelta = new Vector2(300, 60);

        // 7. Ligar tudo no UIManager
        UIManager uiManager = canvasObj.AddComponent<UIManager>();
        uiManager.xpFill = xpFill;
        uiManager.hpFill = hpFill;
        uiManager.levelText = levelText;

        EditorPrefs.SetBool("NeonWorlds_HUD", true);
        Debug.Log("[NeonWorlds] HUD criado com sucesso!");
    }
}
