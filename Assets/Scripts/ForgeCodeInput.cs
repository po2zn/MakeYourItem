using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public sealed class ForgeCodeInput:InputField,IScrollHandler
{
 struct Snapshot{public string text;public int caret,anchor;}
 readonly List<Snapshot> undo=new List<Snapshot>();readonly Event keyEvent=new Event();public int DrawStart=>m_DrawStart;public ScrollRect outerScroll;int lastCaret=-1;string lastText,layoutText;float layoutWidth=-1,layoutViewport=-1,viewportWidth=-1;
 void Apply(BlueprintIndentation.Edit edit){text=edit.Text;selectionFocusPosition=edit.Cursor;selectionAnchorPosition=edit.Selection;ForceLabelUpdate();}
 bool composing;int compositionFrame=-1;
 string compositionText="";
 CanvasRenderer caretRenderer;
 readonly Vector3[] clipCorners=new Vector3[4];
 readonly TextGenerator caretLayout=new TextGenerator();
#if ENABLE_INPUT_SYSTEM
 UnityEngine.InputSystem.Keyboard keyboard;
 void Composition(UnityEngine.InputSystem.LowLevel.IMECompositionString value){compositionText=value.ToString();composing=value.Count>0;compositionFrame=Time.frameCount;}
 protected override void OnEnable(){base.OnEnable();keyboard=UnityEngine.InputSystem.Keyboard.current;if(keyboard!=null)keyboard.onIMECompositionChange+=Composition;}
 protected override void OnDisable(){if(keyboard!=null)keyboard.onIMECompositionChange-=Composition;keyboard=null;composing=false;compositionText="";base.OnDisable();}
#endif
 // uGUI Insert uses caretPositionInternal += 1. Its getter includes the
 // *next* IME preedit, so a commit while composing advances into following code.
 // Source edits must use the raw committed-text indices, never display indices.
 protected override void Append(char character){
 if(caretPositionInternal==m_CaretPosition&&!composing){base.Append(character);return;}
 if(readOnly||char.IsSurrogate(character)||character=='\0'||text.Length>=16382)return;
 int start=Mathf.Clamp(Mathf.Min(m_CaretPosition,m_CaretSelectPosition),0,text.Length);
 int end=Mathf.Clamp(Mathf.Max(m_CaretPosition,m_CaretSelectPosition),start,text.Length);
 string committed=text.Remove(start,end-start);
 if(onValidateInput!=null)character=onValidateInput(committed,start,character);
 else if(characterValidation!=CharacterValidation.None)character=Validate(committed,start,character);
 if(character=='\0'||(characterLimit>0&&committed.Length>=characterLimit))return;
 SetTextWithoutNotify(committed.Insert(start,character.ToString()));
 m_CaretPosition=m_CaretSelectPosition=start+1;
 UpdateLabel();onValueChanged.Invoke(text);
 }
 // Escape opens the game menu; it must not cancel the current code draft.
 bool FinishOnEscape(Event key){if(key.keyCode!=KeyCode.Escape)return false;DeactivateInputField();return true;}
 // Navigate logical source lines, independent of visual word wrapping.
 bool MoveLineBoundary(Event key){
 if(key.keyCode!=KeyCode.Home&&key.keyCode!=KeyCode.End)return false;
 int target=Mathf.Clamp(selectionFocusPosition,0,text.Length);
 if(key.control||key.command)target=key.keyCode==KeyCode.Home?0:text.Length;
 else if(key.keyCode==KeyCode.Home){while(target>0&&text[target-1]!='\n')target--;}
 else {while(target<text.Length&&text[target]!='\n')target++;if(target>0&&text[target-1]=='\r')target--;}
 if(!key.shift)selectionAnchorPosition=target;
 selectionFocusPosition=target;ForceLabelUpdate();return true;
 }
 public override void OnUpdateSelected(BaseEventData data){if(!isFocused)return;if(composing||compositionFrame==Time.frameCount){base.OnUpdateSelected(data);return;}while(Event.PopEvent(keyEvent)){
 if(keyEvent.rawType==EventType.KeyDown){if(FinishOnEscape(keyEvent))break;if(MoveLineBoundary(keyEvent))continue;var before=new Snapshot{text=text,caret=selectionFocusPosition,anchor=selectionAnchorPosition};bool undoKey=keyEvent.control&&keyEvent.keyCode==KeyCode.Z;
 if(undoKey){if(undo.Count>0){var s=undo[undo.Count-1];undo.RemoveAt(undo.Count-1);text=s.text;selectionFocusPosition=s.caret;selectionAnchorPosition=s.anchor;}}
 else if(!keyEvent.control&&!keyEvent.alt&&!keyEvent.command&&BlueprintIndentation.TryTabEvent(text,selectionFocusPosition,selectionAnchorPosition,keyEvent.keyCode==KeyCode.Tab,keyEvent.character,keyEvent.shift,out var tabEdit)){if(!readOnly)Apply(tabEdit);}
 else if(keyEvent.keyCode==KeyCode.Backspace&&!keyEvent.control&&BlueprintIndentation.Backspace(text,selectionFocusPosition,selectionAnchorPosition,out var edit))Apply(edit);
 else {if(KeyPressed(keyEvent)==EditState.Finish){DeactivateInputField();break;}if(BlueprintIndentation.IndentNativeNewline(before.text,text,before.caret,before.anchor,out var newline))Apply(newline);}
 if(text!=before.text&&!undoKey){undo.Add(before);if(undo.Count>256)undo.RemoveAt(0);}UpdateLabel();}
 else if((keyEvent.type==EventType.ValidateCommand||keyEvent.type==EventType.ExecuteCommand)&&keyEvent.commandName=="SelectAll")SelectAll();
 }data.Use();}
 protected override void LateUpdate(){
 if(!outerScroll){base.LateUpdate();return;}
 var r=(RectTransform)transform;var label=textComponent;var viewport=outerScroll.viewport.rect;
 string displayText=isFocused&&composing?text.Insert(Mathf.Clamp(m_CaretPosition,0,text.Length),compositionText):text;
 outerScroll.horizontal=true;outerScroll.inertia=false;label.horizontalOverflow=HorizontalWrapMode.Overflow;
 // The InputField must cover the entire logical line; the parent viewport clips it.
 // Merely disabling Text wrapping leaves InputField's own caret layout constrained.
 var settings=label.GetGenerationSettings(Vector2.zero);settings.horizontalOverflow=HorizontalWrapMode.Overflow;
 float width=Mathf.Max(viewport.width,label.cachedTextGeneratorForLayout.GetPreferredWidth(displayText,settings)/label.pixelsPerUnit+32);
 bool changed=layoutText!=displayText||layoutWidth!=width||layoutViewport!=viewport.height||viewportWidth!=viewport.width;
 if(changed){
 layoutText=displayText;layoutWidth=width;layoutViewport=viewport.height;viewportWidth=viewport.width;
 r.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);
 settings=label.GetGenerationSettings(new Vector2(width-20,0));settings.horizontalOverflow=HorizontalWrapMode.Overflow;
 float height=Mathf.Max(viewport.height,label.cachedTextGeneratorForLayout.GetPreferredHeight(displayText,settings)/label.pixelsPerUnit+24);
 r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);ForceLabelUpdate();
 }
 base.LateUpdate();
 float x=-r.anchoredPosition.x,y=r.anchoredPosition.y;
 if(isFocused&&(changed||lastCaret!=caretPosition||lastText!=text)){
 // ForceLabelUpdate changes Text.text but its render generator is only rebuilt
 // later by Canvas. Never use that previous-frame geometry for a new edit.
 ForceLabelUpdate();
 var caretSettings=label.GetGenerationSettings(label.rectTransform.rect.size);
 caretSettings.generateOutOfBounds=true;
 caretLayout.Populate(label.text,caretSettings);
 var chars=caretLayout.characters;int index=Mathf.Clamp(caretPosition-m_DrawStart,0,chars.Count-1);
 if(chars.Count>0){
 float cx=chars[index].cursorPos.x/label.pixelsPerUnit-label.rectTransform.rect.xMin+10,cy=label.rectTransform.rect.yMax-chars[index].cursorPos.y/label.pixelsPerUnit;
 if(cx<x+8)x=cx-8;else if(cx+16>x+viewport.width)x=cx+16-viewport.width;
 if(cy<y)y=cy;else if(cy+32>y+viewport.height)y=cy+32-viewport.height;
 }
 }
 r.anchoredPosition=new Vector2(-Mathf.Clamp(x,0,Mathf.Max(0,r.rect.width-viewport.width)),Mathf.Clamp(y,0,Mathf.Max(0,r.rect.height-viewport.height)));
 lastCaret=caretPosition;lastText=text;
 ClipCaret();
 }
 public override void Rebuild(CanvasUpdate update){base.Rebuild(update);if(update==CanvasUpdate.LatePreRender)ClipCaret();}
 void ClipCaret(){
 if(!outerScroll||!textComponent)return;
 if(!caretRenderer){var parent=textComponent.transform.parent;for(int i=0;i<parent.childCount;i++){var child=parent.GetChild(i);if(child.name==name+" Input Caret"){caretRenderer=child.GetComponent<CanvasRenderer>();break;}}}
 if(!caretRenderer)return;
 var canvas=textComponent.canvas;if(!canvas)return;var root=canvas.rootCanvas.transform;
 // InputField's raw CanvasRenderer is not a MaskableGraphic, so RectMask2D
 // never registers it. Clip both the caret and selection mesh explicitly.
 Rect clip=new Rect();bool first=true;
 foreach(var mask in outerScroll.viewport.GetComponentsInParent<RectMask2D>()){
 if(!mask.isActiveAndEnabled)continue;
 mask.rectTransform.GetWorldCorners(clipCorners);Vector2 min=root.InverseTransformPoint(clipCorners[0]),max=root.InverseTransformPoint(clipCorners[2]);
 var rect=Rect.MinMaxRect(min.x,min.y,max.x,max.y);
 if(first){clip=rect;first=false;}else clip=Rect.MinMaxRect(Mathf.Max(clip.xMin,rect.xMin),Mathf.Max(clip.yMin,rect.yMin),Mathf.Min(clip.xMax,rect.xMax),Mathf.Min(clip.yMax,rect.yMax));
 }
 if(!first)caretRenderer.EnableRectClipping(clip);
 }
 public void OnScroll(PointerEventData e){var scroll=outerScroll?outerScroll:GetComponentInParent<ScrollRect>();if(scroll)scroll.OnScroll(e);e.Use();}
}
