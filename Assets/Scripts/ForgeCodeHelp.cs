using UnityEngine;using UnityEngine.EventSystems;
public sealed class ForgeCodeHelp:MonoBehaviour,IPointerMoveHandler,IPointerExitHandler {
 public ForgeCodeInput editor;ForgeGameUI ui;Vector2 position;float since;bool inside,shown,owns;
 public void Bind(ForgeGameUI value){ui=value;}
 public void OnPointerMove(PointerEventData e){if(position!=e.position){position=e.position;since=Time.unscaledTime;shown=false;ui?.HideCodeHelp();}inside=true;}
 public void OnPointerExit(PointerEventData e){inside=false;since=Time.unscaledTime;}
 void OnDisable(){inside=false;if(owns)ui?.HideCodeHelp();owns=false;}
 void Update(){if(owns&&!inside&&ui!=null&&Time.unscaledTime-since>.25f){
#if ENABLE_INPUT_SYSTEM
var mouse=UnityEngine.InputSystem.Mouse.current;if(mouse==null||!RectTransformUtility.RectangleContainsScreenPoint(ui.codeHelp,mouse.position.ReadValue())){ui.HideCodeHelp();owns=false;}
#endif
}if(!inside||shown||ui==null||Time.unscaledTime-since<.5f)return;shown=true;var t=editor.textComponent;RectTransformUtility.ScreenPointToLocalPointInRectangle(t.rectTransform,position,null,out var local);var chars=t.cachedTextGenerator.characters;float best=float.MaxValue;int index=-1;for(int i=0;i<chars.Count;i++){var p=chars[i].cursorPos/t.pixelsPerUnit;float distance=Mathf.Abs(local.y-p.y)*8+Mathf.Abs(local.x-p.x);if(distance<best){best=distance;index=i;}}if(index<0)return;index+=editor.DrawStart;foreach(var token in BlueprintLexer.Scan(editor.text,true))if(token.Length>0&&index>=token.Start&&index<token.Start+token.Length){string help=BlueprintHelp.Describe(editor.text,token);if(!string.IsNullOrEmpty(help)){ui.ShowCodeHelp(help,position);owns=true;}break;}}
}
