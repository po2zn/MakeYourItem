using System;
using UnityEngine;

// One off-screen stage per inventory, rendered only when a pooled cell changes item.
public sealed class ForgeProductPreview : IDisposable
{
    readonly GameObject stage;
    readonly Camera camera;
    readonly GameObject sword;
    public ForgeProductPreview(){
        sword=Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets").sword;
        stage=new GameObject("Product preview stage");stage.hideFlags=HideFlags.DontSave;
        stage.transform.position=new Vector3(10000,10000,10000);
        camera=new GameObject("Product preview camera").AddComponent<Camera>();camera.transform.SetParent(stage.transform,false);
        camera.enabled=false;camera.orthographic=true;camera.orthographicSize=.65f;camera.nearClipPlane=.1f;camera.farClipPlane=6;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.17f,.23f);camera.cullingMask=1<<31;
        camera.transform.localPosition=new Vector3(.65f,.6f,-2);camera.transform.LookAt(stage.transform.position+Vector3.up*.4f);
        var light=new GameObject("Product preview light").AddComponent<Light>();light.transform.SetParent(stage.transform,false);
        light.type=LightType.Point;light.transform.localPosition=new Vector3(-.5f,1,-1);light.range=5;light.intensity=5;light.cullingMask=1<<31;light.shadows=LightShadows.None;
    }
    public RenderTexture Render(ForgeProduct product,RenderTexture texture,int resolution=128){
        if(!texture){texture=new RenderTexture(resolution,resolution,16){name="Visible product thumbnail",hideFlags=HideFlags.DontSave};texture.Create();}
        var model=UnityEngine.Object.Instantiate(sword,stage.transform,false);
        try{
            model.AddComponent<ForgeSwordAppearance>().Apply(product.Material,product.Shaped,product.Data,product.FinalHeat);
            foreach(var child in model.GetComponentsInChildren<Transform>(true))child.gameObject.layer=31;
            foreach(var collider in model.GetComponentsInChildren<Collider>())collider.enabled=false;
            camera.targetTexture=texture;camera.Render();
        }finally{camera.targetTexture=null;model.SetActive(false);UnityEngine.Object.Destroy(model);}
        return texture;
    }
    public void Dispose(){if(stage)UnityEngine.Object.Destroy(stage);}
}
