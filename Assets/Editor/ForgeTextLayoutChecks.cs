using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ForgeTextLayoutChecks
{
    static string Root=>Path.GetDirectoryName(Application.dataPath);
    [MenuItem("Tools/Validation/Audit UI Text Layout")]
    public static void Run()
    {
        var lines=new List<string>();int checks=0;var failures=new List<string>();
        Font font=Font.CreateDynamicFontFromOSFont(new[]{"Consolas","Malgun Gothic"},18);
        var title=new GUIStyle{font=font,fontSize=24,fontStyle=FontStyle.Bold,wordWrap=true,padding=new RectOffset(4,4,4,4)};
        var body=new GUIStyle(title){fontSize=18,fontStyle=FontStyle.Normal};
        Action<string,string,Rect,GUIStyle> check=(id,text,rect,style)=>{
            var content=new GUIContent(text);var fitted=ForgeTextLayout.Fit(rect,content,style);checks++;
            if(!ForgeTextLayout.Fits(rect,content,fitted)||fitted.fontSize<12)failures.Add(id+": "+fitted.fontSize+"px in "+rect.size+" / "+text);
        };
        try{
            foreach(var offer in ForgeTechnology.Offers){
                check("tech title "+offer.Id,offer.Title,new Rect(0,0,260,94),title);
                foreach(var state in new[]{"구매 완료","선행 구매 필요","다이아 부족","골드 부족","구매 가능"})check("state",state,new Rect(0,0,172,38),body);
            }
            foreach(var name in new[]{"기본 대장장이 · 1명","대장장이 16명 고용","호감도 조회 · 90902()","위치 조회 · 90903()","요구 수량 조회 · 90909()"})check("smith title",name,new Rect(0,0,258,88),title);
            var guideBody=new GUIStyle(body){padding=new RectOffset(0,0,0,0)};
            var guideTitle=new GUIStyle(guideBody){fontSize=22,fontStyle=FontStyle.Bold};
            foreach(float width in new[]{190f,330f,550f,910f})foreach(bool english in new[]{false,true})foreach(var entry in BlueprintGuide.Entries){
                foreach(var pair in new[]{new KeyValuePair<string,GUIStyle>(entry.HeadingFor(english),guideTitle),new KeyValuePair<string,GUIStyle>(entry.DescriptionFor(english),guideBody),new KeyValuePair<string,GUIStyle>(entry.NoteFor(english),guideBody)}){
                    float h=ForgeTextLayout.Height(pair.Key,pair.Value,width);
                    checks++;if(float.IsNaN(h)||h<pair.Value.CalcHeight(new GUIContent(pair.Key),width))failures.Add("guide height "+entry.Id);
                }
            }
            foreach(int width in new[]{640,800,1280,1920,2560,3840}){
                float unit=Mathf.Min(160,Mathf.Max(1,(width-64)/5));
                foreach(string text in new[]{"대장장이 현황","전체 보기","성장 트리","재료 상점","대장간"})check("header "+width,text,new Rect(0,0,unit,48),title);
            }
            lines.Add("UI text metrics: "+checks+" checks; "+failures.Count+" failures");
            lines.Add("Guide widths: 190, 330, 550, 910; viewport widths: 640, 800, 1280, 1920, 2560, 3840");
            lines.Add("Font: "+font.name+"; includes all technology titles and numeric/English guide entries.");
            lines.Add("This checks layout metrics, not visual screenshots or arbitrary user-authored text.");
            lines.AddRange(failures);
        }catch(Exception error){lines.Add(error.ToString());}
        finally{
            Directory.CreateDirectory(Path.Combine(Root,"Logs"));File.WriteAllLines(Path.Combine(Root,"Logs/UiTextAudit.txt"),lines);
            UnityEngine.Object.DestroyImmediate(font);
        }
    }
}
