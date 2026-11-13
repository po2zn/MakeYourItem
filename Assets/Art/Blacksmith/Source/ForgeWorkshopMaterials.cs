using UnityEngine;

// Physical props only. Inventory, tech icons, prices and interpreter receipts stay authoritative.
public sealed partial class ForgeWorkshopVisuals
{
    Transform frostTarget,ingredient,frostRipple;
    GameObject frostStation;
    int ingredientId;
    bool frostObstacleAdded;Bounds frostHitBounds;
    MaterialPropertyBlock rawHeat;
    public bool HitFrostStation(Ray ray,out float distance){distance=0;if(!FrostStationVisible)return false;return new Bounds(frostHitBounds.center+transform.position,frostHitBounds.size).IntersectRay(ray,out distance);}
    public bool FrostStationVisible=>frostStation&&frostStation.activeSelf;
    public Vector3 FrostWorkPosition=>frostTarget?frostTarget.position:Vector3.zero;
    public void ResetFrostStation(){if(frostStation){frostStation.SetActive(false);Destroy(frostStation);}frostStation=null;frostTarget=null;if(frostObstacleAdded)obstacles.Remove(frostHitBounds);frostObstacleAdded=false;HideFrostRipple();}
    public void SetFrostUnlocked(bool unlocked){
        if(unlocked&&!frostStation&&assets.frostStation){
            frostStation=Spawn(assets.frostStation,"Frost water workstation",new Vector3(3.5f,0,0));
            frostStation.transform.localRotation*=Quaternion.Euler(0,180,0);
            frostTarget=frostStation.transform.Find("FrostTarget");
            if(!frostObstacleAdded){var rs=frostStation.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);bounds.center-=transform.position;frostHitBounds=bounds;obstacles.Add(bounds);frostObstacleAdded=true;}
            var owner=GetComponentInParent<ForgeMvp>(true);
            frostStation.AddComponent<ForgeFrostStationReveal>().Initialize(!owner||owner.TryBeginFrostReveal());
        }
        if(frostStation)frostStation.SetActive(unlocked);
        if(!unlocked)HideFrostRipple();
    }
    static bool UsesFrostStation(ForgeMvp.Step step)=>step.process==4&&step.consumed!=null&&step.consumed.TryGetValue(8,out var amount)&&amount>0;
    void EnsureRaw(PieceView piece,int material){
        if(piece.raw||!assets.MaterialModel(material))return;
        piece.raw=Instantiate(assets.MaterialModel(material),piece.root.transform,false);piece.raw.name="Raw material model";piece.raw.transform.localPosition=new Vector3(0,.22f,0);
    }
    void SetRawVisible(PieceView piece,bool raw){
        if(!piece.raw)return;
        piece.raw.SetActive(raw);piece.blade.enabled=!raw;
    }
    void AnimateRawHeat(PieceView piece,float heat){
        if(!piece.raw||!piece.raw.activeSelf)return;
        if(rawHeat==null)rawHeat=new MaterialPropertyBlock();
        foreach(var r in piece.raw.GetComponentsInChildren<Renderer>()){
            r.GetPropertyBlock(rawHeat);rawHeat.SetColor("_BaseColor",Color.Lerp(Color.white,new Color(1,.55f,.3f),Mathf.Clamp01(heat/1200f)*.4f));
            rawHeat.SetColor("_EmissionColor",new Color(1,.16f,.015f)*Mathf.Clamp01(heat/1200f));r.SetPropertyBlock(rawHeat);
        }
    }
    void PrepareIngredient(ForgeMvp.Step step){
        HideIngredient();int id=0;
        if(step.process!=1&&step.consumed!=null)foreach(int candidate in new[]{8,6,7,9})if(step.consumed.TryGetValue(candidate,out var count)&&count>0&&assets.MaterialModel(candidate)){id=candidate;break;}
        if(id==0){ingredientId=0;return;}
        if(ingredient&&ingredientId!=id){ingredient.gameObject.SetActive(false);Destroy(ingredient.gameObject);ingredient=null;}
        if(!ingredient){ingredient=Instantiate(assets.MaterialModel(id),transform,false).transform;ingredient.name="Held ingredient "+id;}
        ingredientId=id;ingredient.gameObject.SetActive(false);
    }
    void HideIngredient(){if(ingredient)ingredient.gameObject.SetActive(false);HideFrostRipple();if(frostMist)frostMist.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
    void AnimateIngredient(ForgeMvp.Step step,PieceView piece,float t,Vector3 station){
        if(!ingredient||ingredientId==0)return;
        bool frost=UsesFrostStation(step);float begin=frost?.28f:.42f,end=frost?.6f:.88f;
        bool visible=t>=begin&&t<end;ingredient.gameObject.SetActive(visible);if(!visible)return;
        float phase=Mathf.InverseLerp(begin,end,t);
        Vector3 ready=smith.TransformPoint(new Vector3(.27f,.96f,.22f));
        Vector3 contact=frost?station+Vector3.up*.24f:piece.root.transform.position+Vector3.up*.12f;
        Vector3 hand=Vector3.Lerp(ready,contact,Mathf.SmoothStep(0,1,phase));
        Reach(rightArm,rightFore,rightHand,rightSocket,hand,Quaternion.LookRotation(smith.forward,Vector3.up),false);
        ingredient.SetPositionAndRotation(rightSocket.position,Quaternion.Euler(0,phase*70,0));
        if(frost){float drop=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,phase));ingredient.position=Vector3.Lerp(rightSocket.position,station,drop);ingredient.localScale=Vector3.one*Mathf.Lerp(1,.25f,drop);}else ingredient.localScale=Vector3.one;
    }
    ParticleSystem frostMist;
    Texture2D mistTexture;
    Material FrostEffectMaterial(){
        var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;owned.Add(material);return material;
    }
    void EmitFrostMist(Vector3 position){
        if(!frostMist){
            var obj=new GameObject("Frost water mist");obj.transform.SetParent(transform,false);frostMist=obj.AddComponent<ParticleSystem>();frostMist.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=frostMist.main;main.playOnAwake=false;main.loop=false;main.maxParticles=32;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=frostMist.emission;emission.enabled=false;var shape=frostMist.shape;shape.enabled=false;
            var size=frostMist.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.3f),new Keyframe(.6f,1),new Keyframe(1,.2f)));
            var color=frostMist.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.4f,.8f,1),1)},new[]{new GradientAlphaKey(.55f,0),new GradientAlphaKey(0,1)});color.color=gradient;
            mistTexture=new Texture2D(32,32,TextureFormat.RGBA32,false);mistTexture.name="Frost mist softness";mistTexture.wrapMode=TextureWrapMode.Clamp;
            for(int y=0;y<32;y++)for(int x=0;x<32;x++){float radius=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;mistTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-radius),2)));}mistTexture.Apply();
            var material=FrostEffectMaterial();material.SetTexture("_BaseMap",mistTexture);material.SetColor("_BaseColor",new Color(.7f,.9f,1,.65f));
            var renderer=obj.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.sharedMaterial=material;
        }
        frostMist.Play();for(int i=0;i<24;i++){float angle=i*2.39996f;frostMist.Emit(new ParticleSystem.EmitParams{position=position+new Vector3(Mathf.Cos(angle)*.13f,0,Mathf.Sin(angle)*.09f),velocity=new Vector3(Mathf.Cos(angle)*.1f,.18f+(i%4)*.04f,Mathf.Sin(angle)*.1f),startLifetime=.75f,startSize=.18f+(i%3)*.04f,startColor=new Color(.7f,.9f,1,.6f)},1);}
    }
    void FrostRipple(Vector3 position,float t){
        if(!frostRipple){var g=new GameObject("Frost cooling glow");g.transform.SetParent(transform,false);frostRipple=g.transform;var line=g.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=32;line.widthMultiplier=.018f;
            for(int i=0;i<32;i++){float angle=i*Mathf.PI*2/32;line.SetPosition(i,new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)));}var mat=FrostEffectMaterial();mat.SetColor("_BaseColor",new Color(.15f,.65f,1,.55f));line.sharedMaterial=mat;}
        frostRipple.gameObject.SetActive(true);frostRipple.position=position+Vector3.up*.015f;frostRipple.localScale=new Vector3(.06f+.12f*t,1,.06f+.12f*t);
    }
    void HideFrostRipple(){if(frostRipple)frostRipple.gameObject.SetActive(false);}
}
