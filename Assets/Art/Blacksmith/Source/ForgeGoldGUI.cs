using UnityEngine;
using System.Text;
public static class ForgeGoldGUI
{
 static ForgeGoldArtwork artwork,diamondArtwork;
 static GUIContent Prefix(GUIContent content){
 var value=content.text;var result=new StringBuilder();int copied=0;
 for(int i=1;i<value.Length;i++)if(ForgeGoldGlyph.CurrencyAt(value,i)){int start=ForgeGoldGlyph.AmountStart(value,i);result.Append(value,copied,start-copied);result.Append(value[i]);result.Append("\u2002 ");result.Append(value,start,i-start);copied=i+1;}
 result.Append(value,copied,value.Length-copied);return new GUIContent(result.ToString(),content.image,content.tooltip);
 }
 static bool PrefixAt(string value,int i){return (value[i]=='G'||value[i]=='D')&&i+1<value.Length&&(char.IsDigit(value[i+1])||value[i+1]=='+'||value[i+1]=='-'||value[i+1]=='\u2002');}
 static bool HasGold(string value){if(string.IsNullOrEmpty(value))return false;for(int i=1;i<value.Length;i++)if(ForgeGoldGlyph.CurrencyAt(value,i))return true;return false;}
 static GUIContent Hidden(GUIContent content){var s=new StringBuilder();for(int i=0;i<content.text.Length;i++)s.Append(PrefixAt(content.text,i)?"<color=#FFFFFF00>"+content.text[i]+"</color>":content.text[i].ToString());return new GUIContent(s.ToString(),content.image,content.tooltip);}
 static void Coins(Rect rect,GUIContent original,GUIStyle style){
 if(Event.current.type!=EventType.Repaint)return;if(!artwork)artwork=Resources.Load<ForgeGoldArtwork>("BlacksmithGoldArtwork");if(!artwork)return;
 float width=style.CalcSize(new GUIContent("G\u2002")).x-style.padding.horizontal;float height=ForgeTextLayout.FontSize(style)*.9f;
 for(int i=0;i<original.text.Length;i++)if(PrefixAt(original.text,i)){
 if(!diamondArtwork)diamondArtwork=Resources.Load<ForgeGoldArtwork>("BlacksmithDiamondArtwork");var currency=original.text[i]=='D'?diamondArtwork:artwork;if(!currency||!currency.texture)continue;
 var cursor=style.GetCursorPixelPosition(rect,original,i);var uv=currency.region;float aspect=currency.texture.width*uv.width/(currency.texture.height*uv.height);float h=Mathf.Min(height,width/aspect),w=h*aspect;
 var box=new Rect(cursor.x+(width-w)*.5f,cursor.y+(style.lineHeight-h)*.5f,w,h);GUI.DrawTextureWithTexCoords(box,currency.texture,uv,true);
 }
 }
 public static void Label(Rect rect,GUIContent content,GUIStyle style){if(!HasGold(content.text)){GUI.Label(rect,content,style);return;}content=Prefix(content);GUI.Label(rect,Hidden(content),new GUIStyle(style){richText=true});Coins(rect,content,style);}
 public static bool Button(Rect rect,GUIContent content,GUIStyle style){if(!HasGold(content.text))return GUI.Button(rect,content,style);content=Prefix(content);bool clicked=GUI.Button(rect,Hidden(content),new GUIStyle(style){richText=true});Coins(rect,content,style);return clicked;}
 public static bool LayoutButton(string text,GUIStyle style,params GUILayoutOption[] options){var content=new GUIContent(text);var rect=GUILayoutUtility.GetRect(content,style,options);return Button(rect,content,style);}
 public static void LayoutLabel(string text,GUIStyle style,params GUILayoutOption[] options){var content=new GUIContent(text);var rect=GUILayoutUtility.GetRect(content,style,options);Label(rect,content,style);}
}
