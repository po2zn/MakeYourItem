using System;
using UnityEngine;

[Flags]
public enum ForgeRenderLayer { None=0, World=1, Syntax=2, Guide=4, Developer=8, TreeEdges=16, TreeCards=32, AllUI=64 }

// Diagnostic overrides are only writable in the Editor/development builds.
public static class ForgeRenderDiagnostics
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static bool Optimized=true;
    public static ForgeRenderLayer Hidden;
#else
    public const bool Optimized=true;
    public const ForgeRenderLayer Hidden=ForgeRenderLayer.None;
#endif
    public static bool Hide(ForgeRenderLayer layer)=> (Hidden&layer)!=0;
    public static bool SkipPaint(ForgeRenderLayer layer)=>Event.current.type==EventType.Repaint&&Hide(layer);
}

public partial class ForgeMvp
{
    bool cameraSuspended,cameraWasEnabled;
    void UpdateRenderVisibility()
    {
        bool covered=(ForgeRenderDiagnostics.Optimized&&showTechTree&&!showTitle)||ForgeRenderDiagnostics.Hide(ForgeRenderLayer.World);
        if(covered&&!cameraSuspended&&workspaceCamera){cameraWasEnabled=workspaceCamera.enabled;workspaceCamera.enabled=false;cameraSuspended=true;}
        else if(!covered)RestoreRenderCamera();
    }
    void RestoreRenderCamera()
    {
        if(cameraSuspended&&workspaceCamera)workspaceCamera.enabled=cameraWasEnabled;
        cameraSuspended=false;
    }
}
