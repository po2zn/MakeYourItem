using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Authored hierarchy + a viewport-sized pool. No GameObject is retained per product.
public sealed class ForgeProductInventory : MonoBehaviour
{
    public const int Columns=6;
    public const float CellHeight=244,Gap=8,RowHeight=CellHeight+Gap;
    public RectTransform gridArea,detailsArea;
    public ScrollRect gridScroll,detailScroll;
    public Button cellTemplate,backButton,sellButton;
    public Text heading,emptyMessage,detailText;
    ForgeMvp game;ForgeShop shop;Font font;
    int revision=-1,selected=-1,detailSelection=int.MinValue,detailRevision=-1;
    ForgeShop.Entry selectedEntry,lastClickedEntry;
    float lastClickedAt=-1;
    const float DoubleClickSeconds=.3f;
    Vector2 size,viewportSize;
    readonly List<Cell> cells=new List<Cell>();
    ForgeProductPreview preview;
    RawImage detailPreview;
    RenderTexture detailTexture;
    sealed class Cell {public Button button,sell;public RectTransform rect;public Text order,name,info;public Image blade;public RawImage preview;public RenderTexture texture;public int id=-1;public bool selected;}
    void OnDestroy(){if(preview!=null)preview.Dispose();foreach(var cell in cells)if(cell.texture){cell.texture.Release();Destroy(cell.texture);}if(detailTexture){detailTexture.Release();Destroy(detailTexture);}}
    public int PoolCount=>cells.Count;
    public static int Rows(int count)=>count<=0?0:(count-1)/Columns+1;
    public static int PoolLimit(float height)=>Columns*(Mathf.CeilToInt(Mathf.Max(0,height)/RowHeight)+2);
    public static int FirstRow(float offset,int rows)=>Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0,offset)/RowHeight)-1,0,Mathf.Max(0,rows-1));
    public static float ContentHeight(int count)=>Mathf.Max(0,Rows(count)*RowHeight-Gap);
    public void Bind(ForgeMvp owner,ForgeShop products,Font textFont)
    {
        if(game)return;game=owner;shop=products;font=textFont;ForgeSfxButton.Bind(this);
        foreach(var text in GetComponentsInChildren<Text>(true))text.font=font;
        backButton.onClick.AddListener(()=>game.GameUIAction("products-close"));
        // Retain the authored button only as the visual template for slot actions.
        sellButton.gameObject.SetActive(false);
        var detailRect=(RectTransform)detailScroll.transform;
        detailRect.offsetMin=new Vector2(detailRect.offsetMin.x,12);
        gridScroll.onValueChanged.AddListener(_=>Populate());
        cellTemplate.gameObject.SetActive(false);
        var picture=new GameObject("SelectedProduct3D",typeof(RectTransform),typeof(RawImage));picture.layer=gameObject.layer;picture.transform.SetParent(detailScroll.content,false);
        detailPreview=picture.GetComponent<RawImage>();detailPreview.raycastTarget=false;
        var rect=detailPreview.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-8);rect.sizeDelta=new Vector2(200,200);picture.SetActive(false);
    }
    void OnEnable(){lastClickedEntry=null;lastClickedAt=-1;revision=-1;size=Vector2.zero;viewportSize=Vector2.zero;}
    void LateUpdate(){if(!game)return;Layout();Refresh();}
    public void Refresh()
    {
        if(shop==null||!gameObject.activeInHierarchy)return;
        Layout();
        if(revision!=shop.Revision){revision=shop.Revision;heading.text="완성품 · "+shop.Count.ToString("N0")+"개";emptyMessage.gameObject.SetActive(shop.Count==0);
            gridScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,ContentHeight(shop.Count));
            var p=gridScroll.content.anchoredPosition;p.y=Mathf.Clamp(p.y,0,Mathf.Max(0,ContentHeight(shop.Count)-gridScroll.viewport.rect.height));gridScroll.content.anchoredPosition=p;
            if(shop.Find(selected)!=selectedEntry){selected=-1;selectedEntry=null;}foreach(var cell in cells)cell.id=-1;Populate();}
        var item=shop.Find(selected);sellButton.interactable=item!=null;
        if(detailSelection!=selected||detailRevision!=revision){detailSelection=selected;detailRevision=revision;
            detailPreview.gameObject.SetActive(item!=null);
            if(item!=null){if(preview==null)preview=new ForgeProductPreview();detailTexture=preview.Render(item.Product,detailTexture,256);detailPreview.texture=detailTexture;}
            var offset=detailText.rectTransform.offsetMax;offset.y=item!=null?-216:0;detailText.rectTransform.offsetMax=offset;
            detailText.text=item==null?"완성품을 선택하면 검의 상태와 판매 정보를 확인할 수 있습니다.":"제작 순서 #"+item.Order+"\n도면: "+item.Product.BlueprintName+"\n\n"+ForgePricing.Describe(item.Product,false);
            detailScroll.verticalNormalizedPosition=1;MeasureDetails();}
    }
    void Layout()
    {
        var root=(RectTransform)transform;var current=root.rect.size;
        if(size!=current){size=current;bool wide=size.x>=1100;
            gridArea.anchorMin=gridArea.anchorMax=gridArea.pivot=new Vector2(0,1);
            detailsArea.anchorMin=detailsArea.anchorMax=detailsArea.pivot=new Vector2(0,1);
            float detailWidth=wide?320:size.x-24,detailHeight=wide?size.y-84:186;
            gridArea.anchoredPosition=new Vector2(12,-72);gridArea.sizeDelta=new Vector2(wide?size.x-356:size.x-24,wide?size.y-84:size.y-282);
            detailsArea.anchoredPosition=new Vector2(wide?size.x-332:12,wide?-72:-(size.y-198));detailsArea.sizeDelta=new Vector2(detailWidth,detailHeight);
            Canvas.ForceUpdateCanvases();MeasureDetails();}
        var viewport=gridScroll.viewport.rect.size;
        if(viewportSize!=viewport){viewportSize=viewport;Populate();}
    }
    void MeasureDetails(){if(!detailText)return;Canvas.ForceUpdateCanvases();float h=detailText.preferredHeight+16+(detailPreview&&detailPreview.gameObject.activeSelf?216:0);detailScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,h);}
    Cell CreateCell()
    {
        var button=Instantiate(cellTemplate,gridScroll.content);button.name="VisibleProductCell_"+cells.Count;
        var cell=new Cell{button=button,rect=(RectTransform)button.transform,order=button.transform.Find("Order").GetComponent<Text>(),name=button.transform.Find("Name").GetComponent<Text>(),info=button.transform.Find("Info").GetComponent<Text>(),blade=button.transform.Find("Blade").GetComponent<Image>()};
        foreach(string part in new[]{"Blade","Guard","Grip"}){var old=button.transform.Find(part);if(old)old.gameObject.SetActive(false);}
        var picture=new GameObject("Product3DPreview",typeof(RectTransform),typeof(RawImage));picture.layer=button.gameObject.layer;picture.transform.SetParent(button.transform,false);
        cell.preview=picture.GetComponent<RawImage>();cell.preview.raycastTarget=false;
        var pictureRect=cell.preview.rectTransform;pictureRect.anchorMin=pictureRect.anchorMax=new Vector2(.5f,1);pictureRect.pivot=new Vector2(.5f,1);pictureRect.anchoredPosition=new Vector2(0,-38);pictureRect.sizeDelta=new Vector2(54,54);
        cell.sell=Instantiate(sellButton,cell.rect);cell.sell.name="SellThisProduct";
        cell.sell.onClick=new Button.ButtonClickedEvent();
        cell.sell.onClick.AddListener(()=>{if(cell.id==selected)SellEntry(selectedEntry);});
        var sellRect=(RectTransform)cell.sell.transform;sellRect.anchorMin=new Vector2(0,0);sellRect.anchorMax=new Vector2(1,0);sellRect.pivot=new Vector2(.5f,0);
        sellRect.anchoredPosition=new Vector2(0,6);sellRect.sizeDelta=new Vector2(-12,38);
        var sellLabel=cell.sell.GetComponentInChildren<Text>(true);sellLabel.text="판매";sellLabel.font=font;sellLabel.fontSize=16;
        sellLabel.resizeTextForBestFit=true;sellLabel.resizeTextMinSize=12;sellLabel.resizeTextMaxSize=16;
        cell.sell.gameObject.SetActive(false);
        button.onClick.AddListener(()=>SelectCell(cell));ForgeSfxButton.Bind(button);cells.Add(cell);return cell;
    }
    void SelectCell(Cell cell)
    {
        var entry=shop.Find(cell.id);if(entry==null)return;
        float now=Time.unscaledTime;
        bool doubleClick=ReferenceEquals(lastClickedEntry,entry)&&now-lastClickedAt<=DoubleClickSeconds;
        lastClickedEntry=entry;lastClickedAt=now;
        selected=entry.Id;selectedEntry=entry;
        if(doubleClick){SellEntry(entry);return;}
        Populate();Refresh();
    }
    void SellEntry(ForgeShop.Entry entry)
    {
        // Cells are recycled after a sale/scroll; never sell a replacement item.
        lastClickedEntry=null;lastClickedAt=-1;
        if(entry==null||!ReferenceEquals(shop.Find(entry.Id),entry))return;
        game.SellInventoryProduct(entry.Id);Refresh();
    }
    void Populate()
    {
        if(shop==null||!gameObject.activeInHierarchy)return;
        float width=gridScroll.viewport.rect.width;if(width<=0)return;
        int count=shop.Count,needed=Math.Min(count,PoolLimit(gridScroll.viewport.rect.height));
        while(cells.Count<needed)CreateCell();
        int start=FirstRow(gridScroll.content.anchoredPosition.y,Rows(count))*Columns;
        float cellWidth=(width-Gap*(Columns-1))/Columns;
        for(int i=0;i<cells.Count;i++){
            var cell=cells[i];int index=start+i;bool active=i<needed&&index<count;
            if(cell.button.gameObject.activeSelf!=active)cell.button.gameObject.SetActive(active);if(!active){cell.id=-1;continue;}
            var entry=shop.Items[index];cell.rect.anchoredPosition=new Vector2((index%Columns)*(cellWidth+Gap),-(index/Columns)*RowHeight);cell.rect.sizeDelta=new Vector2(cellWidth,CellHeight);
            if(cell.id!=entry.Id){cell.id=entry.Id;cell.order.text="#"+entry.Order;cell.name.text=entry.Product.DisplayName;
                cell.info.text=ForgeMaterialGuide.Find(entry.Product.Material).Name+" · 열 "+entry.Product.FinalHeat+"\n"+ForgeMaterialHud.FormatCount(ForgePricing.Quote(entry.Product).SalePrice)+"G";
                if(preview==null)preview=new ForgeProductPreview();
                cell.texture=preview.Render(entry.Product,cell.texture);cell.preview.texture=cell.texture;}
            cell.selected=cell.id==selected;cell.sell.gameObject.SetActive(cell.selected);cell.sell.interactable=cell.selected;cell.button.GetComponent<Image>().color=cell.selected?new Color(.22f,.4f,.55f):new Color(.12f,.17f,.23f);
        }
    }
}
