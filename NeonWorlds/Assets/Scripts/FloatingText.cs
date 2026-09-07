using UnityEngine;
using System.Collections.Generic;
public enum DamageTextStyle { Normal, Critical, Area, Electric }
public class FloatingText : MonoBehaviour
{
    const int MaximumVisible=32;
    static readonly List<FloatingText> visible=new List<FloatingText>();
    static int sequence;
    TextMesh textMesh;
    MeshRenderer meshRenderer;
    float timer,lifetime=.7f,pixelHeight=14f;
    Vector3 drift;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { visible.Clear(); sequence=0; }
    void OnEnable()
    {
        visible.RemoveAll(item=>item==null);
        while(visible.Count>=MaximumVisible)
        {
            FloatingText oldest=visible[0]; visible.RemoveAt(0);
            if(oldest!=null) { oldest.gameObject.SetActive(false); Destroy(oldest.gameObject); }
        }
        visible.Add(this);
    }
    void OnDisable() { visible.Remove(this); }
    public void Setup(string text) { Setup(text,new Color(.92f,.98f,1f)); }
    public void SetupDamage(int damage,bool isCrit,DamageTextStyle style=DamageTextStyle.Normal)
    {
        Color color=isCrit ? new Color(1f,.84f,.32f) : style==DamageTextStyle.Area ? new Color(.88f,.5f,1f) : style==DamageTextStyle.Electric ? new Color(.2f,.95f,1f) : new Color(.92f,.98f,1f);
        Setup(damage.ToString()+(isCrit ? "!" : ""),color,isCrit ? 1.2f : 1f);
    }
    public void Setup(string text,Color color,float sizeMultiplier=1f)
    {
        timer=0; pixelHeight=14f*Mathf.Clamp(sizeMultiplier,.8f,1.25f);
        lifetime=sizeMultiplier>1 ? .8f : .7f;
        textMesh=GetComponent<TextMesh>(); if(textMesh==null) textMesh=gameObject.AddComponent<TextMesh>();
        textMesh.text=text; textMesh.anchor=TextAnchor.MiddleCenter; textMesh.alignment=TextAlignment.Center;
        textMesh.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textMesh.fontSize=48; textMesh.characterSize=.1f; textMesh.color=color;
        meshRenderer=GetComponent<MeshRenderer>(); meshRenderer.sharedMaterial=textMesh.font.material;
        Camera camera=Camera.main;
        float side=((sequence++%7)-3)*.18f;
        Vector3 normal=transform.parent!=null ? (transform.position-transform.parent.position).normalized : Vector3.up;
        drift=normal*.85f+(camera!=null ? Vector3.ProjectOnPlane(camera.transform.right,normal)*side : Vector3.zero);
        UpdatePresentation();
    }
    void Update()
    {
        timer+=Time.deltaTime;
        if(timer>=lifetime) { gameObject.SetActive(false); Destroy(gameObject); return; }
        transform.position+=drift*Time.deltaTime;
        if(textMesh!=null)
        {
            Color color=textMesh.color; color.a=1f-Mathf.InverseLerp(lifetime*.55f,lifetime,timer); textMesh.color=color;
        }
    }
    void LateUpdate() { UpdatePresentation(); }
    void UpdatePresentation()
    {
        if(textMesh==null || meshRenderer==null) return;
        Camera camera=Camera.main;
        float worldHeight=.35f;
        if(camera!=null)
        {
            transform.rotation=camera.transform.rotation;
            float depth=Vector3.Dot(transform.position-camera.transform.position,camera.transform.forward);
            float viewHeight=camera.orthographic ? camera.orthographicSize*2f : 2f*Mathf.Max(camera.nearClipPlane,depth)*Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad);
            worldHeight=viewHeight*pixelHeight/Mathf.Max(1,camera.pixelHeight);
        }
        float pop=Mathf.Lerp(1.12f,1f,Mathf.Clamp01(timer/.12f));
        float glyphHeight=Mathf.Max(.01f,meshRenderer.localBounds.size.y);
        float scale=Mathf.Clamp(worldHeight,.12f,1.1f)*pop/glyphHeight;
        BossWorldMotion.SetWorldScale(transform,Vector3.one*scale);
    }
}
