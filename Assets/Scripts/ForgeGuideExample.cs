using UnityEngine;
using UnityEngine.UI;

// A selectable, read-only example inside the guide's existing scrolling page.
public sealed class ForgeGuideExample : MonoBehaviour
{
    InputField field;
    Text code,notes;
    LayoutElement height;
    float measuredWidth=-1;
    string measuredSource;

    public static ForgeGuideExample Create(Text body)
    {
        var host=new GameObject("GuideExample",typeof(RectTransform),typeof(Image),typeof(LayoutElement));
        host.layer=body.gameObject.layer;host.transform.SetParent(body.transform.parent,false);
        host.transform.SetSiblingIndex(body.transform.GetSiblingIndex()+1);
        host.GetComponent<Image>().color=Color.black;
        var label=new GameObject("GuideExampleText",typeof(RectTransform),typeof(Text));
        label.layer=host.layer;label.transform.SetParent(host.transform,false);
        var text=label.GetComponent<Text>();text.font=body.font;text.fontSize=18;text.color=Color.white;
        text.supportRichText=false;text.raycastTarget=false;text.alignment=TextAnchor.UpperLeft;
        text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
        var rect=text.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
        rect.offsetMin=new Vector2(12,12);rect.offsetMax=new Vector2(-12,-12);
        var input=host.AddComponent<InputField>();input.textComponent=text;input.targetGraphic=host.GetComponent<Image>();
        input.lineType=InputField.LineType.MultiLineNewline;input.readOnly=true;
        input.transition=Selectable.Transition.None;input.navigation=new Navigation{mode=Navigation.Mode.None};
        input.customCaretColor=true;input.caretColor=Color.white;
        input.selectionColor=new Color(.25f,.48f,.8f,.55f);
        ForgeCodeColors.Apply(text);
        var tail=Instantiate(body,body.transform.parent);tail.name="GuideExampleNotes";
        tail.transform.SetSiblingIndex(host.transform.GetSiblingIndex()+1);tail.text="";
        var view=host.AddComponent<ForgeGuideExample>();view.field=input;view.code=text;view.notes=tail;
        view.height=host.GetComponent<LayoutElement>();view.height.minHeight=60;
        view.height.flexibleHeight=0;
        return view;
    }

    public void Show(string source,string after)
    {
        bool visible=!string.IsNullOrEmpty(source);gameObject.SetActive(visible);
        notes.text=after??"";notes.gameObject.SetActive(!string.IsNullOrEmpty(after));
        if(!visible)return;
        if(field.text!=source){field.DeactivateInputField();field.SetTextWithoutNotify(source);measuredSource=null;}
        Resize();
    }

    void LateUpdate(){Resize();}
    void Resize()
    {
        if(!code||!field)return;
        float width=Mathf.Max(1,((RectTransform)transform).rect.width-24);
        if(measuredWidth==width&&measuredSource==field.text)return;
        measuredWidth=width;measuredSource=field.text;
        var settings=code.GetGenerationSettings(new Vector2(width,0));settings.verticalOverflow=VerticalWrapMode.Overflow;
        // Include a spare line so native selection never hides the final line.
        float content=code.cachedTextGeneratorForLayout.GetPreferredHeight(field.text+"\n ",settings)/code.pixelsPerUnit;
        height.preferredHeight=Mathf.Max(60,Mathf.Ceil(content)+24);
        LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform.parent);
        field.ForceLabelUpdate();
    }
}
