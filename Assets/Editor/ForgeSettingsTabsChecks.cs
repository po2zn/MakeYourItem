using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class ForgeSettingsTabsChecks {
 public static void Check(){
 var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/NotoSansKR.ttf");var body=new GUIStyle{font=font,fontSize=18,wordWrap=true};var button=new GUIStyle(body){fontSize=22,padding=new RectOffset(12,12,4,4)};
 foreach(var size in new[]{new Vector2Int(640,480),new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160)}){
 float width=Mathf.Min(900,size.x-64),height=Mathf.Min(560,size.y-24),control=width*.71f;
 foreach(string tab in new[]{"● 화면","● 소리","● 코드 색상","● 코드 효과"}){var rect=new Rect(0,0,(width-24)/4,42);var text=new GUIContent(tab);var fitted=ForgeTextLayout.Fit(rect,text,button);if(fitted.fontSize<14||!ForgeTextLayout.Fits(rect,text,fitted))throw new Exception("Tab clipped "+size+" "+tab);}
 float typing=ForgeTextLayout.Height("키 입력 소리 · 효과음(SFX) 음량 적용",body,control),effect=ForgeTextLayout.Height("코드가 붉게 달아올랐다가 금속빛으로 식으며, 화면이 미세하게 흔들립니다.",body,control);
 if(116+60+typing+48+effect>height-108)throw new Exception("Code effects overlap footer "+size);
 if(116+116+52+44>height-108)throw new Exception("Display help overlap footer "+size);
 if(116+162+44>height-108)throw new Exception("Audio help overlap footer "+size);
 }
 Directory.CreateDirectory("Artifacts/SettingsTabs");File.WriteAllText("Artifacts/SettingsTabs/results.txt","PASS 640x480,800x600,1280x720,1920x1080,3840x2160 tabs fit at readable size; display/audio/code help stays above footer. Metrics only; not screenshots.");Debug.Log("SETTINGS_TABS_PASS");
 }
}
