using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
// A non-interactive background follows the generated visual rows of a source line.
public sealed class ForgeExecutionLine:MonoBehaviour {
 public int Line;public readonly List<int> WorkerLines=new List<int>();readonly List<Image> workerBands=new List<Image>();ForgeCodeInput input;Image band;
 public void Initialize(ForgeCodeInput value){input=value;if(band)return;var go=new GameObject("ExecutionLine",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.layer=5;go.transform.SetParent(input.textComponent.transform.parent,false);go.transform.SetSiblingIndex(input.textComponent.transform.GetSiblingIndex());band=go.GetComponent<Image>();band.color=new Color(1f,.58f,.22f,.22f);band.raycastTarget=false;band.gameObject.SetActive(false);}
 void LateUpdate(){
 if(!input||!band)return;
 UpdateBand(band,Line);
 while(workerBands.Count<WorkerLines.Count){var extra=Instantiate(band,band.transform.parent);extra.name="WorkerExecutionLine";extra.color=new Color(.25f,.8f,1f,.25f);extra.transform.SetSiblingIndex(band.transform.GetSiblingIndex());workerBands.Add(extra);}
 for(int i=0;i<workerBands.Count;i++)UpdateBand(workerBands[i],i<WorkerLines.Count?WorkerLines[i]:0);
 }
 void UpdateBand(Image band,int Line){
var label=input.textComponent;var source=input.text;int start=0;
 if(Line<=0){band.gameObject.SetActive(false);return;}
 for(int n=1;n<Line;n++){int next=source.IndexOf('\n',start);if(next<0){band.gameObject.SetActive(false);return;}start=next+1;}
 int end=source.IndexOf('\n',start);if(end<0)end=source.Length;
 int visibleStart=input.DrawStart,visibleEnd=visibleStart+label.text.Length;
 if(end<visibleStart||start>visibleEnd){band.gameObject.SetActive(false);return;}
 var lines=label.cachedTextGenerator.lines;if(lines.Count==0){band.gameObject.SetActive(false);return;}
 int first=0,last=lines.Count-1;int localStart=Mathf.Max(0,start-visibleStart),localEnd=Mathf.Min(label.text.Length,end-visibleStart);
 for(int i=0;i<lines.Count;i++){if(lines[i].startCharIdx<=localStart)first=i;if(lines[i].startCharIdx<=Mathf.Max(localStart,localEnd-1))last=i;}
 float scale=label.pixelsPerUnit;float top=label.rectTransform.rect.yMax+(lines[first].topY-lines[0].topY)/scale;float bottom=label.rectTransform.rect.yMax+(lines[last].topY-lines[0].topY-lines[last].height)/scale;
 var rect=band.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.position=label.rectTransform.TransformPoint(new Vector3(label.rectTransform.rect.xMin,top,0));rect.sizeDelta=new Vector2(label.rectTransform.rect.width,top-bottom);band.gameObject.SetActive(true);
 }
}
