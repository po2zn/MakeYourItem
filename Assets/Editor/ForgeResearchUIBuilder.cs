using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ForgeResearchUIBuilder
{
    static Font font;static Sprite sprite;
    static readonly Color Background=new Color(.065f,.09f,.13f),Surface=new Color(.16f,.23f,.32f);
    static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);return r;
    }
    static void Stretch(RectTransform r,float left=0,float bottom=0,float right=0,float top=0){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,-top);}
    static Image Image(RectTransform r,Color color,bool raycast=false){var i=r.gameObject.AddComponent<Image>();i.color=color;i.raycastTarget=raycast;return i;}
    static Text Text(string name,Transform parent,float x,float y,float width,float height,string value,int size=24,bool bold=false)
    {
        var r=Rect(name,parent,x,y,width,height);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;t.color=new Color(.93f,.96f,1);t.alignment=TextAnchor.MiddleLeft;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;t.supportRichText=false;return t;
    }
    static Button Button(string name,Transform parent,float x,float y,float w,float h,string title)
    {
        var r=Rect(name,parent,x,y,w,h);var image=Image(r,Surface,true);image.sprite=sprite;image.type=UnityEngine.UI.Image.Type.Sliced;
        var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;var colors=b.colors;colors.highlightedColor=new Color(1.25f,1.25f,1.25f);colors.pressedColor=new Color(.8f,.8f,.8f);b.colors=colors;b.navigation=new Navigation{mode=Navigation.Mode.None};
        var label=Text("Label",r,8,4,w-16,h-8,title,22,true);label.alignment=TextAnchor.MiddleCenter;label.resizeTextForBestFit=true;label.resizeTextMinSize=16;label.resizeTextMaxSize=22;return b;
    }
    static ForgeTreeCard Card(Transform parent,string name,string title,float x,float y,int smith=-1,string offer=null,bool material=false)
    {
        var r=Rect(name,parent,x,y,300,176);var image=Image(r,Surface,true);image.sprite=sprite;image.type=UnityEngine.UI.Image.Type.Sliced;
        var card=r.gameObject.AddComponent<ForgeTreeCard>();card.offerId=offer;card.smithId=smith;card.surface=image;
        var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.navigation=new Navigation{mode=Navigation.Mode.None};card.button=b;
        card.title=Text("Title",r,18,12,264,92,title,24,true);card.title.resizeTextForBestFit=true;card.title.resizeTextMinSize=20;card.title.resizeTextMaxSize=24;
        Image(Rect("Divider",r,18,114,264,1),new Color(.4f,.5f,.6f,.35f));
        card.state=Text("State",r,18,120,170,42,"구매 가능",20);card.state.resizeTextForBestFit=true;card.state.resizeTextMinSize=16;card.state.resizeTextMaxSize=20;
        card.price=Text("Price",r,190,120,94,42,"",18);card.price.alignment=TextAnchor.MiddleRight;
        if(material){
            card.state.rectTransform.sizeDelta=new Vector2(150,42);card.price.rectTransform.anchoredPosition=new Vector2(18,-84);card.price.rectTransform.sizeDelta=new Vector2(264,28);
            var input=Rect("Quantity",r,186,126,96,36);Image(input,new Color(.08f,.12f,.18f),true);var field=input.gameObject.AddComponent<InputField>();
            field.textComponent=Text("Value",input,8,2,80,32,"1",22);field.textComponent.supportRichText=false;field.text="1";field.contentType=InputField.ContentType.IntegerNumber;field.characterLimit=9;field.navigation=new Navigation{mode=Navigation.Mode.None};card.quantity=field;
            card.title.rectTransform.sizeDelta=new Vector2(264,68);
        }
        return card;
    }
    static ForgeTreePanel.Edge Edge(Transform parent,Rect from,Rect to,string offer=null,int smith=-1)
    {
        float mid=(from.xMax+to.x)/2;var lines=new List<Image>();
        lines.Add(Image(Rect("HorizontalFrom",parent,from.xMax,from.center.y,Mathf.Max(2,mid-from.xMax),3),Color.gray));
        lines.Add(Image(Rect("Vertical",parent,mid,Mathf.Min(from.center.y,to.center.y),3,Mathf.Abs(from.center.y-to.center.y)+3),Color.gray));
        lines.Add(Image(Rect("HorizontalTo",parent,mid,to.center.y,Mathf.Max(2,to.x-mid),3),Color.gray));
        return new ForgeTreePanel.Edge{lines=lines.ToArray(),parentOffer=offer,parentSmith=smith};
    }
    static Rect SmithRect(int i)=>i==0?new Rect(24,236,300,176):i<=5?new Rect(364+(i-1)*340,24,300,176):new Rect(364,236+(i-6)*212,300,176);
    [MenuItem("Tools/MakeYourItem/Rebuild Research UI")]
    public static void Rebuild(){Debug.Log(Build());}
    public static string Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Build in Edit mode only.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/SampleScene.unity")throw new Exception("Expected SampleScene.");
        var game=UnityEngine.Object.FindAnyObjectByType<ForgeMvp>();if(game==null)throw new Exception("Forge MVP not found");
        System.IO.Directory.CreateDirectory("Assets/UI");AssetDatabase.Refresh();
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/NotoSansKR.ttf");if(font==null)throw new Exception("Import NotoSansKR.ttf before building.");
        sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var existing=scene.GetRootGameObjects().FirstOrDefault(o=>o.name=="Forge Research UI");if(existing!=null)UnityEngine.Object.DestroyImmediate(existing);
        var root=Rect("Forge Research UI",null,0,0,1280,720);var panel=root.gameObject.AddComponent<ForgeTreePanel>();
        panel.canvas=root.gameObject.AddComponent<Canvas>();panel.canvas.renderMode=RenderMode.ScreenSpaceOverlay;panel.canvas.sortingOrder=200;
        var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        root.gameObject.AddComponent<GraphicRaycaster>();Image(root,Background,true);
        var header=Rect("Header",root,0,0,1280,150);header.anchorMax=new Vector2(1,1);header.sizeDelta=new Vector2(0,150);Image(header,new Color(.11f,.15f,.21f),true);header.gameObject.AddComponent<Canvas>();header.gameObject.AddComponent<GraphicRaycaster>();
        panel.heading=Text("Heading",header,16,8,1032,40,"테크 트리",28,true);panel.heading.rectTransform.anchorMax=new Vector2(1,1);panel.heading.rectTransform.sizeDelta=new Vector2(-232,40);
        panel.backButton=Button("BackToWorkshop",header,0,8,180,38,"대장간으로");var back=(RectTransform)panel.backButton.transform;back.anchorMin=back.anchorMax=new Vector2(1,1);back.anchoredPosition=new Vector2(-196,-8);
        panel.growthButton=Button("GrowthTab",header,16,54,176,40,"성장 트리");panel.materialsButton=Button("MaterialsTab",header,200,54,176,40,"재료 상점");panel.smithButton=Button("SmithTab",header,384,54,176,40,"대장장이");
        panel.fitButton=Button("Fit",header,568,54,148,40,"전체 보기");panel.resetButton=Button("ResetZoom",header,724,54,124,40,"100%");
        panel.message=Text("PurchaseMessage",header,16,100,1248,42,"항목에 마우스: 설명 · 클릭: 구매 · 빈 곳/우클릭 드래그: 이동 · 휠: 확대/축소",18);panel.message.rectTransform.anchorMax=new Vector2(1,1);panel.message.rectTransform.sizeDelta=new Vector2(-32,42);
        var pages=new List<GameObject>();var maps=new List<ForgeTreePan>();var cards=new List<ForgeTreeCard>();var edges=new List<ForgeTreePanel.Edge>();
        for(int tab=0;tab<3;tab++){
            var view=Rect(tab==0?"GrowthTree":tab==1?"MaterialShop":"BlacksmithTree",root,0,150,1280,570);Stretch(view,0,0,0,150);Image(view,Background,true);view.gameObject.AddComponent<RectMask2D>();
            var map=view.gameObject.AddComponent<ForgeTreePan>();var content=Rect("Content",view,0,0,4000,2500);map.content=content;maps.Add(map);pages.Add(view.gameObject);
            var edgeRoot=Rect("Connections",content,0,0,4000,2500);var cardRoot=Rect("Cards",content,0,0,4000,2500);
            if(tab<2){
                var positions=new Dictionary<string,Rect>();int material=0;
                foreach(var offer in ForgeTechnology.Offers){if((offer.Kind==ForgePurchaseKind.Material)!=(tab==1))continue;var r=tab==1?new Rect(24+(material%3)*340,24+(material++/3)*200,300,176):new Rect(24+offer.Tier*340,24+offer.Lane*200,300,176);positions.Add(offer.Id,r);cards.Add(Card(cardRoot,offer.Id,offer.Title,r.x,r.y,-1,offer.Id,tab==1));}
                if(tab==0){var baseRect=new Rect(24,824,300,176);var basic=Rect("BaseWorkshop",cardRoot,baseRect.x,baseRect.y,300,176);Image(basic,new Color(.12f,.3f,.23f));Text("Title",basic,18,12,264,152,"기본 공방 · 제공\n철 / 나무 / 물\n1~5 공정",24,true);
                    foreach(var offer in ForgeTechnology.Offers.Where(o=>o.Kind!=ForgePurchaseKind.Material)){if(offer.Parents.Count==0)edges.Add(Edge(edgeRoot,baseRect,positions[offer.Id]));foreach(var parent in offer.Parents)if(positions.ContainsKey(parent))edges.Add(Edge(edgeRoot,positions[parent],positions[offer.Id],parent));}
                    foreach(var pair in new[]{("금속 가공",0),("행운",2),("마력 가공",3),("공방 운영",6),("시간",8),("기술 연구",10)})Text(pair.Item1,content,24,24+pair.Item2*200,280,80,pair.Item1,28,true);
                    map.mapSize=new Vector2(positions.Values.Max(r=>r.xMax)+24,positions.Values.Max(r=>r.yMax)+24);map.focusPosition=new Vector2(16,baseRect.y-160);
                }else{map.mapSize=new Vector2(1024,624);map.focusPosition=new Vector2(16,-16);}
            }else{
                for(int i=0;i<9;i++){var r=SmithRect(i);cards.Add(Card(cardRoot,"Smith-"+i,ForgeMvp.HierarchySmithTitle(i),r.x,r.y,i));if(i>0)edges.Add(Edge(edgeRoot,SmithRect(i<=5?i-1:0),r,null,i<=5?i-1:0));}
                map.mapSize=new Vector2(2048,860);map.focusPosition=new Vector2(16,-16);
            }
            map.ResetView();
        }
        var baseSmith=cards.First(c=>c.smithId==0);baseSmith.state.gameObject.SetActive(false);baseSmith.price.gameObject.SetActive(false);
        panel.detailsButton=Button("SmithDetails",baseSmith.transform,18,122,264,40,"대장장이 현황");
        var details=Rect("SmithStatus",root,0,150,1280,570);Stretch(details,16,16,16,150);Image(details,Background,true);pages.Add(details.gameObject);
        var detailsView=Rect("Viewport",details,0,0,1248,460);Stretch(detailsView,0,64,0,0);Image(detailsView,Background,true);detailsView.gameObject.AddComponent<RectMask2D>();
        panel.detailsText=Text("StatusText",detailsView,0,0,1200,1600,"대장장이 현황",24);panel.detailsText.alignment=TextAnchor.UpperLeft;panel.detailsText.rectTransform.anchorMax=new Vector2(1,1);panel.detailsText.rectTransform.sizeDelta=new Vector2(-32,1600);
        var detailScroll=detailsView.gameObject.AddComponent<ScrollRect>();detailScroll.viewport=detailsView;detailScroll.content=panel.detailsText.rectTransform;detailScroll.horizontal=false;detailScroll.movementType=ScrollRect.MovementType.Clamped;detailScroll.scrollSensitivity=40;
        panel.stopJobsButton=Button("StopSmithJobs",details,0,0,260,48,"병렬 작업 모두 중단");var stop=(RectTransform)panel.stopJobsButton.transform;stop.anchorMin=stop.anchorMax=Vector2.zero;stop.pivot=Vector2.zero;stop.anchoredPosition=Vector2.zero;
        var tip=Rect("SharedTooltip",root,0,0,520,400);tip.anchorMin=tip.anchorMax=new Vector2(.5f,.5f);tip.pivot=new Vector2(0,1);Image(tip,new Color(.10f,.14f,.20f),true);var tipCanvas=tip.gameObject.AddComponent<Canvas>();tipCanvas.overrideSorting=true;tipCanvas.sortingOrder=210;var group=tip.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=true;tip.gameObject.AddComponent<GraphicRaycaster>();
        panel.tooltip=tip;panel.tooltipTitle=Text("Title",tip,16,12,488,60,"",28,true);panel.tooltipTitle.alignment=TextAnchor.UpperLeft;
        var tipView=Rect("BodyViewport",tip,12,84,496,304);Stretch(tipView,12,12,12,84);Image(tipView,new Color(0,0,0,0),true);tipView.gameObject.AddComponent<RectMask2D>();
        var tipContent=Rect("BodyContent",tipView,0,0,496,500);tipContent.anchorMax=new Vector2(1,1);tipContent.sizeDelta=new Vector2(0,500);panel.tooltipContent=tipContent;
        panel.tooltipBody=Text("Body",tipContent,0,0,476,500,"",22);panel.tooltipBody.alignment=TextAnchor.UpperLeft;panel.tooltipBody.rectTransform.anchorMax=new Vector2(1,1);panel.tooltipBody.rectTransform.sizeDelta=new Vector2(-20,500);
        panel.tooltipScroll=tipView.gameObject.AddComponent<ScrollRect>();panel.tooltipScroll.viewport=tipView;panel.tooltipScroll.content=tipContent;panel.tooltipScroll.horizontal=false;panel.tooltipScroll.movementType=ScrollRect.MovementType.Clamped;panel.tooltipScroll.scrollSensitivity=30;
        tip.gameObject.SetActive(false);panel.pages=pages.ToArray();panel.maps=maps.ToArray();panel.cards=cards.ToArray();panel.edges=edges.ToArray();
        for(int i=0;i<pages.Count;i++)pages[i].SetActive(i==0);
        root.gameObject.SetActive(false);
        PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,"Assets/UI/ForgeResearchUI.prefab",InteractionMode.AutomatedAction);
        game.SetHierarchyTree(panel);EditorUtility.SetDirty(game);
        if(UnityEngine.Object.FindAnyObjectByType<EventSystem>()==null){var system=new GameObject("Forge UI EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));system.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Saved scene and prefab: "+cards.Count+" cards, "+edges.Count+" edges, 4 pages. No runtime creation.";
    }
}
