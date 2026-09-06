using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class MusicControls : MonoBehaviour
{
    GameMusic music;
    Text label;
    Image background;
    bool lastEnabled;

    public static void Create(GameMusic music)
    {
        if (music.GetComponentInChildren<MusicControls>() != null) return;
        GameObject root = new GameObject("MusicControls", typeof(RectTransform));
        root.transform.SetParent(music.transform, false);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        MusicControls controls = root.AddComponent<MusicControls>();
        controls.music = music;

        GameObject buttonObject = new GameObject("MusicToggle", typeof(RectTransform));
        buttonObject.transform.SetParent(root.transform, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-16f, -38f);
        rect.sizeDelta = new Vector2(250f, 38f);
        controls.background = buttonObject.AddComponent<Image>();
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = controls.background;
        // Keep the combat controller's selection free; mouse and F8 both work during pause.
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(music.ToggleMusic);
        Outline border = buttonObject.AddComponent<Outline>();
        border.effectColor = new Color(0f, 0.9f, 1f, 0.8f);
        border.effectDistance = new Vector2(1f, -1f);

        GameObject textObject = new GameObject("Label", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);
        controls.label = textObject.AddComponent<Text>();
        controls.label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        controls.label.fontSize = 16;
        controls.label.fontStyle = FontStyle.Bold;
        controls.label.alignment = TextAnchor.MiddleCenter;
        controls.label.raycastTarget = false;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        controls.Refresh();

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject events = new GameObject("MusicUIEventSystem");
            events.transform.SetParent(root.transform, false);
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>();
        }
    }

    void Update()
    {
        if (music != null && lastEnabled != music.MusicEnabled) Refresh();
    }

    void Refresh()
    {
        lastEnabled = music.MusicEnabled;
        label.text = lastEnabled ? "MÚSICA: LIGADA  [F8]" : "MÚSICA: DESLIGADA  [F8]";
        label.color = lastEnabled ? Color.cyan : new Color(0.7f, 0.75f, 0.8f);
        background.color = new Color(0.025f, 0.04f, 0.085f, 0.9f);
    }
}
