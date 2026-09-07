using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UpgradeGlyph : MaskableGraphic
{
    public GameManager.UpgradeType kind;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if(kind==GameManager.UpgradeType.Heal) { Line(mesh,new Vector2(-.5f,0),new Vector2(.5f,0)); Line(mesh,new Vector2(0,-.5f),new Vector2(0,.5f)); }
        else if(kind==GameManager.UpgradeType.Spread)
        {
            for(int i=-1;i<=1;i++) { Vector2 tip=new Vector2(i*.5f,.55f); Line(mesh,new Vector2(i*.2f,-.4f),tip); Line(mesh,tip,tip+new Vector2(-.12f,-.18f)); Line(mesh,tip,tip+new Vector2(.12f,-.18f)); }
        }
        else if(kind==GameManager.UpgradeType.Magnet)
        {
            for(int i=0;i<20;i++) { float a=Mathf.PI*i/20f,b=Mathf.PI*(i+1)/20f; Line(mesh,new Vector2(Mathf.Cos(a)*.45f,-Mathf.Sin(a)*.45f),new Vector2(Mathf.Cos(b)*.45f,-Mathf.Sin(b)*.45f)); }
            Line(mesh,new Vector2(-.45f,0),new Vector2(-.45f,.5f)); Line(mesh,new Vector2(.45f,0),new Vector2(.45f,.5f));
        }
        else if(kind==GameManager.UpgradeType.Explosive)
        {
            for(int i=0;i<8;i++) { float angle=i*Mathf.PI/4; Vector2 dir=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)); Line(mesh,dir*.22f,dir*.58f); }
        }
        else if(kind==GameManager.UpgradeType.Pierce)
        {
            Line(mesh,new Vector2(0,-.6f),new Vector2(0,.6f));
            Line(mesh,new Vector2(-.2f,.35f),new Vector2(0,.6f)); Line(mesh,new Vector2(.2f,.35f),new Vector2(0,.6f));
            Line(mesh,new Vector2(-.45f,-.12f),new Vector2(-.12f,-.12f)); Line(mesh,new Vector2(.12f,-.12f),new Vector2(.45f,-.12f));
        }
        else if(kind==GameManager.UpgradeType.Bounce)
        {
            Line(mesh,new Vector2(-.5f,-.5f),new Vector2(.4f,0)); Line(mesh,new Vector2(.4f,0),new Vector2(-.3f,.5f));
            Line(mesh,new Vector2(.52f,-.3f),new Vector2(.52f,.3f));
        }
        else if(kind==GameManager.UpgradeType.AegisShield || kind==GameManager.UpgradeType.HyperionBarrier)
        {
            Vector2[] shield={new Vector2(-.45f,.45f),new Vector2(.45f,.45f),new Vector2(.36f,-.2f),new Vector2(0,-.6f),new Vector2(-.36f,-.2f),new Vector2(-.45f,.45f)};
            for(int i=1;i<shield.Length;i++) Line(mesh,shield[i-1],shield[i]);
        }
        else if(kind==GameManager.UpgradeType.OrbitalMines || kind==GameManager.UpgradeType.VoidVortex || kind==GameManager.UpgradeType.LifeSteal)
        {
            for(int i=0;i<24;i++) { float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12; Line(mesh,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.5f,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*.5f); }
            Line(mesh,new Vector2(-.2f,0),new Vector2(.2f,0)); Line(mesh,new Vector2(0,-.2f),new Vector2(0,.2f));
        }
        else if(kind==GameManager.UpgradeType.SentinelDrone || kind==GameManager.UpgradeType.TeslaChain || kind==GameManager.UpgradeType.Critical || kind==GameManager.UpgradeType.Damage)
        {
            Vector2[] diamond={new Vector2(0,.6f),new Vector2(.45f,0),new Vector2(0,-.6f),new Vector2(-.45f,0),new Vector2(0,.6f)};
            for(int i=1;i<diamond.Length;i++) Line(mesh,diamond[i-1],diamond[i]);
            Line(mesh,new Vector2(-.15f,0),new Vector2(.15f,0));
        }
        else
        {
            Vector2[] p=kind==GameManager.UpgradeType.FireRate ? new[]{new Vector2(.2f,.6f),new Vector2(-.3f,-.05f),new Vector2(.2f,-.05f),new Vector2(-.2f,-.6f)} : new[]{new Vector2(-.4f,-.4f),new Vector2(0,.2f),new Vector2(.4f,-.4f)};
            for(int i=1;i<p.Length;i++) Line(mesh,p[i-1],p[i]);
            if(kind==GameManager.UpgradeType.Speed) { Line(mesh,new Vector2(-.4f,0),new Vector2(0,.6f)); Line(mesh,new Vector2(0,.6f),new Vector2(.4f,0)); }
        }
    }
    void Line(VertexHelper mesh,Vector2 a,Vector2 b)
    {
        Rect rect=rectTransform.rect; float scale=Mathf.Min(rect.width,rect.height)*.7f;
        a=rect.center+a*scale; b=rect.center+b*scale;
        Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*1.4f;
        int start=mesh.currentVertCount;
        mesh.AddVert(a-n,color,Vector2.zero); mesh.AddVert(a+n,color,Vector2.zero);
        mesh.AddVert(b+n,color,Vector2.zero); mesh.AddVert(b-n,color,Vector2.zero);
        mesh.AddTriangle(start,start+1,start+2); mesh.AddTriangle(start,start+2,start+3);
    }
}
