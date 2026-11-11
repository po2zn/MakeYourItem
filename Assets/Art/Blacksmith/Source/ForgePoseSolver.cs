using System.Collections.Generic;
using UnityEngine;

// Analytic two-bone chains: a stable bend plane, fixed bone lengths, no wrist CCD.
public static class ForgePoseSolver
{
    public static void Limb(Transform upper,Transform lower,Transform end,Vector3 target,Vector3 pole)
    {
        Vector3 origin=upper.position;
        float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,end.position);
        Vector3 delta=target-origin;float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.005f,a+b-.008f);
        Vector3 direction=delta.sqrMagnitude>.000001f?delta.normalized:Vector3.down;
        Vector3 bend=Vector3.ProjectOnPlane(pole-origin,direction).normalized;
        if(bend.sqrMagnitude<.01f)bend=Vector3.ProjectOnPlane(Vector3.forward,direction).normalized;
        float along=(a*a+distance*distance-b*b)/(2*distance);
        Vector3 elbow=origin+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(end.position-lower.position,origin+direction*distance-lower.position)*lower.rotation;
    }
    public static void Hand(Transform upper,Transform lower,Transform hand,Transform socket,Vector3 target,Quaternion gripRotation,Vector3 pole)
    {
        Vector3 offset=hand.InverseTransformPoint(socket.position);
        Quaternion localGrip=Quaternion.Inverse(hand.rotation)*socket.rotation;
        Quaternion rotation=gripRotation*Quaternion.Inverse(localGrip);
        Vector3 wrist=target-rotation*Vector3.Scale(offset,hand.lossyScale);
        Limb(upper,lower,hand,wrist,pole);hand.rotation=rotation;
    }
    // Small workshop grid. Renderer footprints include the actual replacement meshes;
    // all paths keep a body radius outside props, including between consecutive operations.
    public static List<Vector3> Route(Vector3 start,Vector3 goal,IReadOnlyList<Bounds> obstacles,float radius)
    {
        const float cell=.16f;const int width=51,height=51;
        Vector3 origin=new Vector3(Mathf.Min(start.x,goal.x)-4,start.y,Mathf.Min(start.z,goal.z)-4);
        bool Clear(Vector3 p){foreach(var b in obstacles)if(p.x>b.min.x-radius&&p.x<b.max.x+radius&&p.z>b.min.z-radius&&p.z<b.max.z+radius)return false;return true;}
        Vector3 Point(int id)=>origin+new Vector3(id%width*cell,0,id/width*cell);
        int Nearest(Vector3 p){int best=-1;float d=float.MaxValue;for(int i=0;i<width*height;i++){float n=(Point(i)-p).sqrMagnitude;if(n<d&&Clear(Point(i))){d=n;best=i;}}return best;}
        int first=Nearest(start),last=Nearest(goal);var result=new List<Vector3>{start};if(first<0||last<0)return result;
        var open=new List<int>{first};var cost=new Dictionary<int,float>{{first,0}};var previous=new Dictionary<int,int>();var closed=new HashSet<int>();
        while(open.Count>0){int current=open[0];float best=float.MaxValue;foreach(int id in open){float score=cost[id]+Vector3.Distance(Point(id),Point(last));if(score<best){best=score;current=id;}}
            open.Remove(current);if(current==last)break;closed.Add(current);
            for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++){
                if(dx==0&&dz==0)continue;int x=current%width+dx,z=current/width+dz;if(x<0||x>=width||z<0||z>=height)continue;
                int next=z*width+x;if(closed.Contains(next)||!Clear(Point(next)))continue;
                if(!Clear(Point(current)+new Vector3(dx*cell,0,0))||!Clear(Point(current)+new Vector3(0,0,dz*cell)))continue;
                float value=cost[current]+cell*Mathf.Sqrt(dx*dx+dz*dz);if(cost.TryGetValue(next,out float old)&&value>=old)continue;
                cost[next]=value;previous[next]=current;if(!open.Contains(next))open.Add(next);
            }
        }
        if(!cost.ContainsKey(last))return result;
        var reversed=new List<Vector3>();for(int id=last;id!=first;id=previous[id])reversed.Add(Point(id));reversed.Add(Point(first));reversed.Reverse();result.AddRange(reversed);
        if(Clear(goal))result.Add(new Vector3(goal.x,start.y,goal.z));return result;
    }
    public static Vector3 Along(List<Vector3> route,float t)
    {
        float length=0;for(int i=1;i<route.Count;i++)length+=Vector3.Distance(route[i-1],route[i]);float remaining=length*Mathf.Clamp01(t);
        for(int i=1;i<route.Count;i++){float d=Vector3.Distance(route[i-1],route[i]);if(remaining<=d)return Vector3.Lerp(route[i-1],route[i],d>.00001f?remaining/d:0);remaining-=d;}return route[route.Count-1];
    }
}
