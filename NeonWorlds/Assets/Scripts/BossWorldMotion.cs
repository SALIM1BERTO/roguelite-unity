using UnityEngine;

// Planet transforms may be scaled; distances and velocities here are world units.
public static class BossWorldMotion
{
    public static void SetWorldScale(Transform target,Vector3 scale)
    {
        Vector3 parent=target.parent!=null ? target.parent.lossyScale : Vector3.one;
        target.localScale=new Vector3(scale.x/Mathf.Max(.0001f,Mathf.Abs(parent.x)),scale.y/Mathf.Max(.0001f,Mathf.Abs(parent.y)),scale.z/Mathf.Max(.0001f,Mathf.Abs(parent.z)));
    }
    public static Vector3 SurfacePoint(Transform planet,Vector3 position,Vector3 direction,float distance,float height)
    {
        if(planet==null) return position+direction.normalized*distance;
        Vector3 normal=(position-planet.position).normalized;
        Vector3 tangent=Vector3.ProjectOnPlane(direction,normal).normalized;
        if(tangent.sqrMagnitude<.001f) tangent=Vector3.Cross(normal,Mathf.Abs(normal.y)<.9f ? Vector3.up : Vector3.right).normalized;
        float radius=Mathf.Abs(planet.lossyScale.x)*.5f+height;
        Vector3 next=Quaternion.AngleAxis(distance/Mathf.Max(.1f,radius)*Mathf.Rad2Deg,Vector3.Cross(normal,tangent))*normal;
        return planet.position+next*radius;
    }
    public static void Move(Transform target,Transform planet,ref Vector3 direction,float distance,float height)
    {
        Vector3 oldNormal=planet!=null ? (target.position-planet.position).normalized : target.up;
        Vector3 next=SurfacePoint(planet,target.position,direction,distance,height);
        Vector3 normal=planet!=null ? (next-planet.position).normalized : oldNormal;
        direction=Quaternion.FromToRotation(oldNormal,normal)*direction;
        direction=Vector3.ProjectOnPlane(direction,normal).normalized;
        target.position=next;
        if(direction.sqrMagnitude>.001f) target.rotation=Quaternion.LookRotation(direction,normal);
    }
}
