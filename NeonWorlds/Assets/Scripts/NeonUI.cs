using UnityEngine;
using UnityEngine.UI;

public static class NeonUI
{
    public static readonly Color Surface = new Color(.045f,.062f,.095f,.98f);
    public static readonly Color White = new Color(.91f,.95f,1f);
    public static readonly Color Muted = new Color(.53f,.63f,.73f);
    public static readonly Color Cyan = new Color(.27f,.89f,.93f);
    public static readonly Color Violet = new Color(.66f,.53f,1f);
    public static readonly Color Danger = new Color(1f,.36f,.43f);

    public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false);
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
    public static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color, bool raycast=false)
    {
        Image image=Rect(name,parent,anchor,position,size).gameObject.AddComponent<Image>();
        image.color=color; image.raycastTarget=raycast;
        return image;
    }
    public static Text Label(string name, Transform parent, string value, int size, Color color,
        Vector2 anchor, Vector2 position, Vector2 bounds, TextAnchor alignment=TextAnchor.MiddleLeft)
    {
        Text text=Rect(name,parent,anchor,position,bounds).gameObject.AddComponent<Text>();
        text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize=size; text.color=color; text.text=value; text.alignment=alignment;
        text.raycastTarget=false; text.supportRichText=false;
        return text;
    }
    public static void StyleButton(Button button)
    {
        ColorBlock colors=button.colors;
        colors.normalColor=Color.white;
        colors.highlightedColor=new Color(1.3f,1.45f,1.6f);
        colors.selectedColor=colors.highlightedColor;
        colors.pressedColor=new Color(.7f,.9f,1f);
        colors.fadeDuration=.12f;
        button.colors=colors;
    }
}
