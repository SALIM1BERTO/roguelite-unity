using UnityEngine;
using UnityEngine.Rendering;

/// <summary>A single static star mesh. No particles, colliders or gameplay random state.</summary>
public class OrbitalStars : MonoBehaviour
{
    Mesh mesh;
    Camera view;
    void Start()
    {
        Material material=Resources.Load<Material>("NeonStars");
        if(material==null) return;
        const int count=280;
        var vertices=new Vector3[count*4]; var triangles=new int[count*6];
        var rng=new System.Random(1709);
        for(int i=0;i<count;i++)
        {
            float y=(float)rng.NextDouble()*2-1;
            float angle=(float)rng.NextDouble()*Mathf.PI*2;
            float radius=Mathf.Sqrt(1-y*y);
            Vector3 direction=new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius);
            Vector3 right=Vector3.Cross(direction,Vector3.up).normalized;
            Vector3 up=Vector3.Cross(right,direction).normalized;
            float size=.45f+(float)rng.NextDouble()*.9f;
            Vector3 center=direction*1200;
            vertices[i*4]=center-right*size-up*size;
            vertices[i*4+1]=center-right*size+up*size;
            vertices[i*4+2]=center+right*size+up*size;
            vertices[i*4+3]=center+right*size-up*size;
            int t=i*6,v=i*4;
            triangles[t]=v; triangles[t+1]=v+1; triangles[t+2]=v+2;
            triangles[t+3]=v; triangles[t+4]=v+2; triangles[t+5]=v+3;
        }
        mesh=new Mesh { name="Orbital starfield" };
        mesh.vertices=vertices; mesh.triangles=triangles; mesh.RecalculateBounds();
        gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
    }
    void LateUpdate()
    {
        if(view==null) view=Camera.main;
        if(view!=null) transform.position=view.transform.position;
    }
    void OnDestroy() { if(mesh!=null) Destroy(mesh); }
}
