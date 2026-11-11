using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public sealed class ForgeCodeInput:InputField,IScrollHandler
{
 struct Snapshot{public string text;public int caret,anchor;}
 readonly List<Snapshot> undo=new List<Snapshot>();readonly Event keyEvent=new Event();public int DrawStart=>m_DrawStart;public ScrollRect outerScroll;int lastCaret=-1;string lastText,layoutText;float layoutWidth=-1,layoutViewport=-1;
 void Apply(BlueprintIndentation.Edit edit){text=edit.Text;selectionFocusPosition=edit.Cursor;selectionAnchorPosition=edit.Selection;ForceLabelUpdate();}
 bool composing;int compositionFrame=-1;
#if ENABLE_INPUT_SYSTEM
 UnityEngine.InputSystem.Keyboard keyboard;
 void Composition(UnityEngine.InputSystem.LowLevel.IMECompositionString value){composing=value.Count>0;compositionFrame=Time.frameCount;}
 protected override void OnEnable(){base.OnEnable();keyboard=UnityEngine.InputSystem.Keyboard.current;if(keyboard!=null)keyboard.onIMECompositionChange+=Composition;}
 protected override void OnDisable(){if(keyboard!=null)keyboard.onIMECompositionChange-=Composition;keyboard=null;composing=false;base.OnDisable();}
#endif
 // Escape opens the game menu; it must not cancel the current code draft.
 bool FinishOnEscape(Event key){if(key.keyCode!=KeyCode.Escape)return false;DeactivateInputField();return true;}
 public override void OnUpdateSelected(BaseEventData data){if(!isFocused)return;if(composing||compositionFrame==Time.frameCount){base.OnUpdateSelected(data);return;}while(Event.PopEvent(keyEvent)){
 if(keyEvent.rawType==EventType.KeyDown){if(FinishOnEscape(keyEvent))break;var before=new Snapshot{text=text,caret=selectionFocusPosition,anchor=selectionAnchorPosition};bool undoKey=keyEvent.control&&keyEvent.keyCode==KeyCode.Z;
 if(undoKey){if(undo.Count>0){var s=undo[undo.Count-1];undo.RemoveAt(undo.Count-1);text=s.text;selectionFocusPosition=s.caret;selectionAnchorPosition=s.anchor;}}
 else if(keyEvent.keyCode==KeyCode.Tab&&!keyEvent.control)Apply(BlueprintIndentation.Tab(text,selectionFocusPosition,selectionAnchorPosition,keyEvent.shift));
 else if(keyEvent.keyCode==KeyCode.Backspace&&!keyEvent.control&&BlueprintIndentation.Backspace(text,selectionFocusPosition,selectionAnchorPosition,out var edit))Apply(edit);
 else {if(KeyPressed(keyEvent)==EditState.Finish){DeactivateInputField();break;}if(BlueprintIndentation.IndentNativeNewline(before.text,text,before.caret,before.anchor,out var newline))Apply(newline);}
 if(text!=before.text&&!undoKey){undo.Add(before);if(undo.Count>256)undo.RemoveAt(0);}UpdateLabel();}
 else if((keyEvent.type==EventType.ValidateCommand||keyEvent.type==EventType.ExecuteCommand)&&keyEvent.commandName=="SelectAll")SelectAll();
 }data.Use();}
 protected override void LateUpdate(){base.LateUpdate();if(!outerScroll)return;var r=(RectTransform)transform;float width=textComponent.rectTransform.rect.width,viewportHeight=outerScroll.viewport.rect.height;float height=r.rect.height;if(layoutText!=text||layoutWidth!=width||layoutViewport!=viewportHeight){layoutText=text;layoutWidth=width;layoutViewport=viewportHeight;height=Mathf.Max(outerScroll.viewport.rect.height,textComponent.cachedTextGeneratorForLayout.GetPreferredHeight(text,textComponent.GetGenerationSettings(new Vector2(textComponent.rectTransform.rect.width,0)))/textComponent.pixelsPerUnit+24);r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);}if(isFocused&&(lastCaret!=caretPosition||lastText!=text)){ForceLabelUpdate();var chars=textComponent.cachedTextGenerator.characters;int index=Mathf.Clamp(caretPosition-m_DrawStart,0,chars.Count-1);if(chars.Count>0){float y=-chars[index].cursorPos.y/textComponent.pixelsPerUnit;float scroll=r.anchoredPosition.y;if(y<scroll)scroll=y;else if(y+32>scroll+outerScroll.viewport.rect.height)scroll=y+32-outerScroll.viewport.rect.height;r.anchoredPosition=new Vector2(0,Mathf.Clamp(scroll,0,Mathf.Max(0,height-outerScroll.viewport.rect.height)));}}lastCaret=caretPosition;lastText=text;}
 public void OnScroll(PointerEventData e){var scroll=outerScroll?outerScroll:GetComponentInParent<ScrollRect>();if(scroll)scroll.OnScroll(e);e.Use();}
}
