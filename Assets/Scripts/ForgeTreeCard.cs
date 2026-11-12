using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// All graphics are serialized children. This component only forwards pointer events.
public sealed class ForgeTreeCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string offerId;
    public int smithId=-1;
    public Button button;
    public Image surface;
    public Text title,state,price;
    public InputField quantity;
    [System.NonSerialized] public ForgeTechnology.Offer offer;
    ForgeTreePanel panel;
    public void Bind(ForgeTreePanel owner)
    {
        panel=owner;offer=string.IsNullOrEmpty(offerId)?null:ForgeTechnology.Find(offerId);
        if(offer!=null)title.text=offer.Title;
        button.onClick.AddListener(()=>panel.Purchase(this));
        if(quantity!=null)quantity.onValueChanged.AddListener(_=>panel.QuantityChanged(this));
    }
    public int Quantity=>quantity==null?1:int.TryParse(quantity.text,out int count)&&count>0?count:0;
    public void OnPointerEnter(PointerEventData e){panel.Hover(this,e.position);}
    public void OnPointerExit(PointerEventData e){panel.Leave(this);}
}
