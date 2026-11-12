using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;using UnityEditor;using System;using System.Linq;using System.Text;using System.Threading.Tasks;
public static class ForgeMultilingualCommentChecks{
 public static readonly string[] Lines={
 "# English: This comment stays on this line.",
 "# Русский: Этот комментарий остаётся на этой строке.",
 "# Italiano: Questo commento resta su questa riga.",
 "# العربية: هذا التعليق يبقى في هذا السطر.",
 "# Slovenčina: Tento komentár zostáva na tomto riadku.",
 "# Čeština: Tento komentář zůstává na tomto řádku.",
 "# Español: Este comentario permanece en esta línea.",
 "# Türkçe: Bu yorum bu satırda kalır.",
 "# Slovenščina: Ta komentar ostane v tej vrstici.",
 "# Português: Este comentário fica nesta linha.",
 "# עברית: ההערה הזאת נשארת בשורה הזאת.",
 "# हिन्दी: यह टिप्पणी इसी पंक्ति में रहती है।",
 "# Română: Acest comentariu rămâne pe acest rând.",
 "# Polski: Ten komentarz pozostaje w tym wierszu.",
 "# 한국어: 이 주석은 작성한 줄에만 있어야 합니다.",
 "# 简体中文：这条注释只能留在当前行。",
 "# 日本語：このコメントは同じ行に表示されます。"};
 public static async Task<string> Run(){
 var root=new GameObject("Multilingual typing regression",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=900;
 bool bg=Application.runInBackground;Application.runInBackground=true;var report=new StringBuilder();int failures=0;
 try{
 var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeCodeWindow.prefab"),root.transform);go.SetActive(true);ForgeLegacyUIFont.Apply(go.transform);var view=go.GetComponent<ForgeCodeView>();var input=view.editor;var label=input.textComponent;view.rect.anchorMin=view.rect.anchorMax=view.rect.pivot=new Vector2(0,1);view.rect.anchoredPosition=new Vector2(30,-40);view.rect.sizeDelta=new Vector2(340,220);view.body.gameObject.SetActive(true);input.onValueChanged=new InputField.OnChangeEvent();input.caretBlinkRate=0;ForgeCodeColors.Apply(label);input.text="";input.ActivateInputField();await Task.Delay(200);input.caretPosition=0;
 string expected=string.Join("\n",Enumerable.Repeat("m = 1(1)\n2(m)\n3(m)\n4(m, 0)\n5(m, 1)\n!m",3));input.text=expected;await Task.Delay(150);
 for(int row=0;row<Lines.Length;row++){
  int position=0;for(int n=0;n<=row;n++){int end=expected.IndexOf('\n',position);if(n==row){position=end<0?expected.Length:end;break;}position=end+1;}
 input.caretPosition=position;input.ForceLabelUpdate();await Task.Delay(100);Canvas.ForceUpdateCanvases();
 var gen=label.cachedTextGenerator;var cp=gen.characters[position-input.DrawStart].cursorPos;var gl=gen.lines[row];var local=new Vector3(cp.x/label.pixelsPerUnit,(gl.topY-gl.height*.5f)/label.pixelsPerUnit,0);var screen=RectTransformUtility.WorldToScreenPoint(null,label.rectTransform.TransformPoint(local));
 input.OnPointerDown(new PointerEventData(EventSystem.current){position=screen,pointerPressRaycast=new RaycastResult{screenPosition=screen},button=PointerEventData.InputButton.Left});input.OnPointerUp(new PointerEventData(EventSystem.current){position=screen,pointerPressRaycast=new RaycastResult{screenPosition=screen},button=PointerEventData.InputButton.Left});
 if(input.caretPosition!=position){report.AppendLine("FAIL CLICK row="+row+" expected="+position+" actual="+input.caretPosition);failures++;}
 foreach(char c in " "+Lines[row]){
 input.ProcessEvent(new Event{type=EventType.KeyDown,character=c,keyCode=c=='\n'?KeyCode.Return:KeyCode.None});input.ForceLabelUpdate();expected=expected.Insert(position,c.ToString());position++;Canvas.ForceUpdateCanvases();
 if(input.text!=expected){report.AppendLine("FAIL text at row="+row+" char="+(int)c);failures++;}
 if(input.caretPosition!=position){report.AppendLine("FAIL caret index row="+row+" expected="+position+" actual="+input.caretPosition);failures++;}
 int currentRow=row;var lines=label.cachedTextGenerator.lines;int caretRow=0;for(int j=0;j<lines.Count;j++)if(lines[j].startCharIdx<=input.caretPosition-input.DrawStart)caretRow=j;
 if(input.DrawStart!=0||caretRow!=currentRow||lines.Count!=18){report.AppendLine("FAIL render row="+row+" char="+(int)c+" drawStart="+input.DrawStart+" caretRow="+caretRow+" rows="+lines.Count+" expectedRow="+currentRow);failures++;}
 }
  await Task.Delay(150);Canvas.ForceUpdateCanvases();
 var caret=input.GetComponentsInChildren<CanvasRenderer>().First(c=>c.name.EndsWith("Input Caret"));var vp=input.outerScroll.viewport;
 foreach(var point in caret.GetMesh().vertices){var pointInView=vp.InverseTransformPoint(caret.transform.TransformPoint(point));if(pointInView.x<vp.rect.xMin-2||pointInView.x>vp.rect.xMax+2||pointInView.y<vp.rect.yMin-2||pointInView.y>vp.rect.yMax+2){report.AppendLine("FAIL visible caret row="+row+" point="+pointInView+" viewport="+vp.rect);failures++;}}
 report.AppendLine("ROW "+row+" "+ForgeMvp.LanguageCodes[row]+" caret="+input.caretPosition+" rows="+label.cachedTextGenerator.lineCount+" scroll="+((RectTransform)input.transform).anchoredPosition);
 }
 input.DeactivateInputField();view.rect.sizeDelta=new Vector2(900,900);await Task.Delay(200);input.outerScroll.horizontalNormalizedPosition=0;input.outerScroll.verticalNormalizedPosition=1;await Task.Delay(100);ScreenCapture.CaptureScreenshot("Docs/CodeWindowFeedback/sword-burst-typed.png");await Task.Delay(150);
 report.AppendLine("FAILURES="+failures);System.IO.File.WriteAllText("Docs/CodeWindowFeedback/sword-burst-checks.txt",report.ToString());System.IO.File.WriteAllText("Docs/CodeWindowFeedback/sword-burst-comments.txt",expected);if(failures>0)throw new Exception("Multilingual comment regression failed: "+failures+"; see Docs/CodeWindowFeedback/sword-burst-checks.txt");return "PASS: 17 languages, inline sword comments, caret visibility and logical line preservation";
 }finally{UnityEngine.Object.DestroyImmediate(root);Application.runInBackground=bg;}
 }
}
