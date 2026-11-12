using UnityEngine;

// Reusable flat-shaded golden diamonds, no textures or per-frame allocations.
public sealed class ForgeLuckySparkles : MonoBehaviour
{
    Mesh mesh;
    Material material;
    readonly Transform[] flakes=new Transform[14];
    public void Initialize(Material template){
        material=new Material(template);material.SetColor("_BaseColor",new Color(1,.65f,.08f));
        material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(1,.55f,.04f)*1.8f);
        material.SetFloat("_Metallic",.35f);material.SetFloat("_Smoothness",.3f);
        var points=new[]{Vector3.up,Vector3.down,Vector3.right*.45f,Vector3.forward*.45f,Vector3.left*.45f,Vector3.back*.45f};
        var vertices=new Vector3[24];var triangles=new int[24];int n=0;
        for(int side=0;side<2;side++)for(int i=0;i<4;i++){
            vertices[n]=points[side];vertices[n+1]=points[2+(side==0?(i+1)%4:i)];vertices[n+2]=points[2+(side==0?i:(i+1)%4)];
            triangles[n]=n;triangles[n+1]=n+1;triangles[n+2]=n+2;n+=3;
        }
        mesh=new Mesh{name="Lucky low-poly diamonds"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
        for(int i=0;i<flakes.Length;i++){
            var g=new GameObject("Golden sparkle "+i,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(transform,false);
            g.GetComponent<MeshFilter>().sharedMesh=mesh;var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;flakes[i]=g.transform;
        }
        Hide();
    }
    public void Show(Transform sword,float phase){
        gameObject.SetActive(true);transform.SetPositionAndRotation(sword.position,sword.rotation);
        float envelope=Mathf.Sin(Mathf.Clamp01(phase)*Mathf.PI);
        for(int i=0;i<flakes.Length;i++){
            float angle=i*2.39996f+phase*2;
            flakes[i].localPosition=new Vector3(Mathf.Cos(angle)*(.13f+i%3*.035f),.17f+(i/2)*.11f+phase*.06f,Mathf.Sin(angle)*.17f);
            flakes[i].localRotation=Quaternion.Euler(15+phase*90,i*31+phase*180,i*27);
            float pulse=.25f+.75f*Mathf.Pow(.5f+.5f*Mathf.Sin(phase*28+i*2.1f),2);
            flakes[i].localScale=Vector3.one*(.045f+.02f*(i%3))*pulse*envelope;
        }
    }
    public void Hide(){gameObject.SetActive(false);}
    void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
}
