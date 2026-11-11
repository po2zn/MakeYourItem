using UnityEngine;
using System.Text;
public static class ForgeGoldGUI
{
 static ForgeGoldArtwork artwork;
 static bool HasGold(string value){if(string.IsNullOrEmpty(value))return false;for(int i=1;i<value.Length;i++)if(ForgeGoldGlyph.CurrencyAt(value,i))return true;return false;}
 static GUIContent Hidden(GUIContent content){var s=new StringBuilder();for(int i=0;i<content.text.Length;i++)s.Append(ForgeGoldGlyph.CurrencyAt(content.text,i)?"<color=#FFFFFF00>G</color>":content.text[i].ToString());return new GUIContent(s.ToString(),content.image,content.tooltip);}
 static void Coins(Rect rect,GUIContent original,GUIStyle style){
 if(Event.current.type!=EventType.Repaint)return;if(!artwork)artwork=Resources.Load<ForgeGoldArtwork>("BlacksmithGoldArtwork");if(!artwork)return;
 float width=style.CalcSize(new GUIContent("G")).x-style.padding.horizontal;float height=ForgeTextLayout.FontSize(style)*.85f;
 for(int i=1;i<original.text.Length;i++)if(ForgeGoldGlyph.CurrencyAt(original.text,i)){
 var cursor=style.GetCursorPixelPosition(rect,original,i);var uv=artwork.region;float aspect=artwork.texture.width*uv.width/(artwork.texture.height*uv.height);float h=Mathf.Min(height,width/aspect),w=h*aspect;
 var box=new Rect(cursor.x+(width-w)*.5f,cursor.y+(style.lineHeight-h)*.5f,w,h);GUI.DrawTextureWithTexCoords(box,artwork.texture,uv,true);
 }
 }
 public static void Label(Rect rect,GUIContent content,GUIStyle style){if(!HasGold(content.text)){GUI.Label(rect,content,style);return;}GUI.Label(rect,Hidden(content),new GUIStyle(style){richText=true});Coins(rect,content,style);}
 public static bool Button(Rect rect,GUIContent content,GUIStyle style){if(!HasGold(content.text))return GUI.Button(rect,content,style);bool clicked=GUI.Button(rect,Hidden(content),new GUIStyle(style){richText=true});Coins(rect,content,style);return clicked;}
 public static bool LayoutButton(string text,GUIStyle style,params GUILayoutOption[] options){var content=new GUIContent(text);var rect=GUILayoutUtility.GetRect(content,style,options);return Button(rect,content,style);}
 public static void LayoutLabel(string text,GUIStyle style,params GUILayoutOption[] options){var content=new GUIContent(text);var rect=GUILayoutUtility.GetRect(content,style,options);Label(rect,content,style);}
}
