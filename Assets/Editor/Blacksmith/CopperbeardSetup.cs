using UnityEngine;
using UnityEditor;
using System.Linq;
public static class CopperbeardSetup
{
 const string Model="Assets/Art/Blacksmith/Characters/MeshCharacter/Meshy_AI_Copperbeard_the_Craftsman_20261111_texture.fbx";
 public static string Build(){
 if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
 const string folder="Assets/Art/Blacksmith/Characters/CopperbeardRigged";System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();
 var catalog=Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets");
 var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Blacksmith/Prefabs/Set_20261111_171955/Blacksmith.prefab");
 var root=Object.Instantiate(original);root.name="Blacksmith_Copperbeard";
 try{
 foreach(var r in root.GetComponentsInChildren<Renderer>(true))Object.DestroyImmediate(r);
 foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))Object.DestroyImmediate(f);
 Transform Bone(string n)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
 void Position(string n,Vector3 p){Bone(n).position=p;Bone(n).rotation=Quaternion.identity;}
 Position("Hips",new Vector3(0,.67f,0));Position("Spine",new Vector3(0,.78f,0));Position("Spine1",new Vector3(0,.89f,0));Position("Spine2",new Vector3(0,1.02f,0));Position("Neck",new Vector3(0,1.25f,0));Position("Head",new Vector3(0,1.4f,0));
 foreach(var side in new[]{"Left","Right"}){float sign=side=="Left"?-1:1;
 Position(side+"Shoulder",new Vector3(sign*.19f,1.13f,0));Position(side+"Arm",new Vector3(sign*.32f,1.12f,0));Position(side+"ForeArm",new Vector3(sign*.56f,1.08f,0));Position(side+"Hand",new Vector3(sign*.77f,1.04f,0));
 Position(side+"UpLeg",new Vector3(sign*.16f,.65f,0));Position(side+"Leg",new Vector3(sign*.16f,.34f,0));Position(side+"Foot",new Vector3(sign*.16f,.11f,0));Position(side+"ToeBase",new Vector3(sign*.16f,.06f,.15f));
 var socket=Bone(side+"HandSocket");socket.position=new Vector3(sign*.82f,1.025f,.025f);socket.rotation=Quaternion.identity;
 }
 var src=AssetDatabase.LoadAssetAtPath<GameObject>(Model);var mf=src.GetComponent<MeshFilter>();var mesh=Object.Instantiate(mf.sharedMesh);mesh.name="Copperbeard_Skinned";
 var matrix=mf.transform.localToWorldMatrix;var vertices=mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();var bounds=new Bounds(vertices[0],Vector3.zero);foreach(var v in vertices)bounds.Encapsulate(v);
 float scale=1.75f/bounds.size.y;var offset=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-offset)*scale;
 mesh.vertices=vertices;mesh.normals=mesh.normals.Select(n=>matrix.MultiplyVector(n).normalized).ToArray();mesh.RecalculateTangents();mesh.RecalculateBounds();
 string[] names={"Hips","Spine","Spine1","Spine2","Neck","Head","LeftArm","LeftForeArm","LeftHand","RightArm","RightForeArm","RightHand","LeftUpLeg","LeftLeg","LeftFoot","RightUpLeg","RightLeg","RightFoot"};
 var bones=names.Select(Bone).ToArray();var weights=new BoneWeight[vertices.Length];
 BoneWeight Blend(string a,string b,float t){return new BoneWeight{boneIndex0=System.Array.IndexOf(names,a),boneIndex1=System.Array.IndexOf(names,b),weight0=1-t,weight1=t};}
 for(int i=0;i<vertices.Length;i++){
 var v=vertices[i];float x=Mathf.Abs(v.x);string side=v.x<0?"Left":"Right";
 if(v.y>1.25f||(v.y>1.08f&&v.z>.23f&&x<.32f))weights[i]=Blend("Head","Head",0);
 else if(v.y>.9f&&x>.32f){if(x>.70f)weights[i]=Blend(side+"ForeArm",side+"Hand",Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,.8f,x)));else weights[i]=Blend(side+"Arm",side+"ForeArm",Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.62f,x)));}
 else if(v.y<.59f){if(v.y<.21f)weights[i]=Blend(side+"Foot",side+"Leg",Mathf.SmoothStep(0,1,Mathf.InverseLerp(.13f,.23f,v.y)));else weights[i]=Blend(side+"Leg",side+"UpLeg",Mathf.SmoothStep(0,1,Mathf.InverseLerp(.29f,.41f,v.y)));}
 else if(v.y<.79f)weights[i]=Blend("Hips","Spine",Mathf.InverseLerp(.66f,.79f,v.y));
 else weights[i]=Blend("Spine1","Spine2",Mathf.InverseLerp(.82f,1.08f,v.y));
 }
 mesh.boneWeights=weights;mesh.bindposes=bones.Select(b=>b.worldToLocalMatrix*root.transform.localToWorldMatrix).ToArray();AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(folder+"/CopperbeardMesh.asset"));
 var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name="Copperbeard_Texture";mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Model.Replace(".fbx",".png")));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.22f);AssetDatabase.CreateAsset(mat,AssetDatabase.GenerateUniqueAssetPath(folder+"/Copperbeard.mat"));
 var go=new GameObject("CopperbeardMesh");go.transform.SetParent(root.transform,false);var skin=go.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.sharedMaterial=mat;skin.bones=bones;skin.rootBone=Bone("Hips");skin.localBounds=new Bounds(new Vector3(0,.9f,0),new Vector3(2.5f,2.4f,2));
 string path=AssetDatabase.GenerateUniqueAssetPath("Assets/Art/Blacksmith/Prefabs/Blacksmith_Copperbeard.prefab");catalog.blacksmith=PrefabUtility.SaveAsPrefabAsset(root,path);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();return path+"; vertices="+mesh.vertexCount+" triangles="+mesh.triangles.Length/3+"; bones="+bones.Length;
 }finally{Object.DestroyImmediate(root);}
 }
 public static string Inspect(){AssetDatabase.Refresh();var g=AssetDatabase.LoadAssetAtPath<GameObject>(Model);string s="playing="+EditorApplication.isPlaying+"\n";
 foreach(var t in g.GetComponentsInChildren<Transform>(true))s+=t.name+" pos="+t.position+" rot="+t.localEulerAngles+" scale="+t.localScale+"\n";
 foreach(var r in g.GetComponentsInChildren<Renderer>(true))s+=r.GetType().Name+" "+r.name+" "+r.bounds+" mats="+string.Join(",",r.sharedMaterials.Select(m=>m.name))+"\n";
 foreach(var m in AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Mesh>())s+="mesh "+m.name+" vertices="+m.vertexCount+" bones="+m.bindposes.Length+" bounds="+m.bounds+"\n";
 return s;}
 public static string Preview(){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
 var g=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Model));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g,scene);
 var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Model.Replace(".fbx",".png")));g.GetComponent<Renderer>().sharedMaterial=mat;
 var cam=new GameObject("Camera").AddComponent<Camera>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cam.gameObject,scene);cam.scene=scene;cam.transform.position=new Vector3(1,.4f,-2);cam.transform.LookAt(Vector3.zero);cam.orthographic=true;cam.orthographicSize=.7f;
 var light=new GameObject("Light").AddComponent<Light>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(light.gameObject,scene);light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(40,-30,0);light.intensity=1.7f;
 var rt=new RenderTexture(900,900,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(900,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,900,900),0,0);tex.Apply();System.IO.File.WriteAllBytes("Assets/Art/Blacksmith/Documentation/CopperbeardOriginal.png",tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(mat);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);return "captured";
 }
}
