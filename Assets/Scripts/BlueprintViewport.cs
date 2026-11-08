using System;
public static class BlueprintViewport
{
    public static int Row(string text,int index)
    {int row=0;for(int i=0;i<Math.Min(index,text.Length);i++)if(text[i]=='\n')row++;return row;}
    public static float ContentHeight(string text,float lineHeight,float padding)
    {return padding+(Row(text,text.Length)+2)*Math.Max(1f,lineHeight);}
    public static float FollowY(string text,int index,float lineHeight,float paddingTop,float viewportHeight,float contentHeight,float scroll)
    {
        const float margin=4f;lineHeight=Math.Max(1f,lineHeight);
        float y=paddingTop+Row(text,index)*lineHeight;
        if(y<scroll+margin)scroll=y-margin;
        else if(y+lineHeight>scroll+viewportHeight-margin)scroll=y+lineHeight-viewportHeight+margin;
        return Math.Max(0,Math.Min(scroll,Math.Max(0,contentHeight-viewportHeight)));
    }
}
