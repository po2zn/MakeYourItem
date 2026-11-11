using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public sealed class ForgeCodeView:MonoBehaviour,IPointerDownHandler
{
 public InputField title;public ForgeCodeInput editor;public Button run,step,stop,collapse,close;public Text status;public RectTransform body;public RectTransform editorArea,statusArea;public RectTransform rect;ForgeMvp game;int id;bool syncing;Rect last;
 public void Bind(ForgeMvp owner,int key){game=owner;id=key;ForgeCodeColors.Apply(editor.textComponent);editor.GetComponent<ForgeCodeHelp>().Bind(owner.HierarchyGame);status.GetComponent<Button>().onClick.AddListener(()=>game.GameCodeAction(id,"reveal"));run.onClick.AddListener(()=>game.GameCodeAction(id,"run"));step.onClick.AddListener(()=>game.GameCodeAction(id,"step"));stop.onClick.AddListener(()=>game.GameCodeAction(id,"stop"));collapse.onClick.AddListener(()=>game.GameCodeAction(id,"collapse"));close.onClick.AddListener(()=>game.GameCodeAction(id,"close"));editor.onValueChanged.AddListener(value=>{if(!syncing)game.GameCodeEdit(id,value);});title.onEndEdit.AddListener(value=>game.GameCodeName(id,value));}
 public void OnPointerDown(PointerEventData e){game.GameCodeAction(id,"select");rect.SetAsLastSibling();}
 public void Sync(string name,string source,Rect bounds,bool collapsed,bool running,bool canRun,int line,string diagnostic){syncing=true;if(!title.isFocused)title.SetTextWithoutNotify(name);if(editor.text!=source)editor.SetTextWithoutNotify(source);if(bounds!=last){rect.anchoredPosition=new Vector2(bounds.x,-bounds.y);rect.sizeDelta=bounds.size;last=bounds;}body.gameObject.SetActive(!collapsed);if(statusArea){bool show=diagnostic!=null||line>0;statusArea.gameObject.SetActive(show);editorArea.offsetMin=new Vector2(editorArea.offsetMin.x,show?64:0);}run.interactable=step.interactable=canRun;stop.interactable=running;status.text=diagnostic!=null?diagnostic+" · 클릭하여 오류 줄 보기":(line>0?line.ToString()+""+"줄 실행 대기":"Tab 들여쓰기 · Ctrl+Z 실행 취소");syncing=false;}
 public void Reveal(int index){StartCoroutine(RevealCaret(index));}
 System.Collections.IEnumerator RevealCaret(int index){editor.ActivateInputField();yield return null;editor.caretPosition=Mathf.Clamp(index,0,editor.text.Length);editor.ForceLabelUpdate();}
 void LateUpdate(){if(game==null)return;var bounds=new Rect(rect.anchoredPosition.x,-rect.anchoredPosition.y,rect.sizeDelta.x,rect.sizeDelta.y);if(bounds!=last){last=bounds;game.GameCodeBounds(id,bounds);}}
}
