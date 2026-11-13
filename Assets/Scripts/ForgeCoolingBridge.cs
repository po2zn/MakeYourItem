using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public partial class ForgeMvp {
    public ForgeCraftMotion MainCoolingMotion=>craftMotion;
    public bool CoolingUnlocked=>technology.MaterialAvailable(8);
    public bool DragonUnlocked=>technology.MaterialAvailable(9);
    public bool ProcessingUnlocked=>CoolingUnlocked||DragonUnlocked;
    public Dictionary<int,long> ProcessingStocks(){var result=new Dictionary<int,long>();foreach(var stock in materialStocks)result[stock.material]=stock.count;return result;}
    public bool CoolingVisible=>ProcessingUnlocked&&!showTitle&&!showTutorial&&!showGameMenu&&!showTechTree&&!showProgress;
    public long CoolingWater=>Array.Find(materialStocks,s=>s.material==8)?.count??0;
    public readonly ForgeCoolingViewState CoolingView=new ForgeCoolingViewState();
    readonly BlueprintObservation coolingObservation=new BlueprintObservation();
    readonly BlueprintBreakpoints coolingBreakpoints=new BlueprintBreakpoints();
    ForgeCoolingPanel coolingPanel;bool executionPauseRequested;int coolingProductId=-1;
    public BlueprintObservation CoolingObservation=>coolingObservation;
    public bool CoolingRunning=>busy;
    public int CoolingNextLine=>executionSession?.NextLine??0;
    public string CoolingActiveCode=>codeWindows.Count==0?"":codeWindows[activeWindow].text;
    public void CoolingPause(){executionPauseRequested=true;}
    public void CoolingRun(bool step){if(!ProcessingUnlocked)return;executionPauseRequested=false;Execute(step);}
    public void CoolingStop(){if(executionOwner!=null)StopCodeExecution(executionOwner.id);else StopExecution(true);CoolingView.Clear();coolingObservation.Clear();}
    public string CoolingBreakpoint(int line){if(codeWindows.Count==0||line<1)return "코드창과 양의 줄 번호를 지정하세요.";var w=codeWindows[activeWindow];string module=w.id.ToString();bool added=coolingBreakpoints.Toggle(module,line);return w.name+" · "+line+"줄 중단점 "+(added?"설정":"해제");}
    bool CoolingBreakpointStop(BlueprintRuntime.Session session,bool step){var window=FindExecutionWindow(session.NextModule);return coolingBreakpoints.StopBefore((window??executionOwner)?.id.ToString(),session.NextLine,step);}
    public void CoolingBreakpointLines(int id,List<int> lines){lines.Clear();lines.AddRange(coolingBreakpoints.Lines(id.ToString()));}
    public string CoolingSelectVariable(string name){if(!ProcessingUnlocked)return "서리수 공급을 먼저 해금하세요.";if(busy)return "일시 정지 후 칼날 변수를 선택하세요.";if(!coolingObservation.TryValue(name,out var value)||!(value is Piece p))return "현재 범위의 칼날 변수를 선택하세요.";if(p.material==9&&!DragonUnlocked||p.material!=9&&!CoolingUnlocked)return "해당 재료를 먼저 해금하세요.";CoolingView.Select(p);coolingPanel?.TargetChanged();return CoolingView.Latest;}
    void SyncCoolingUI(){if(!hierarchyGame||!hierarchyGame.Bound)return;if(!coolingPanel)coolingPanel=ForgeCoolingPanel.Create(this,hierarchyGame);coolingPanel.SyncVisibility(CoolingVisible);}
    public void OpenCoolingPanel(){if(!ProcessingUnlocked)return;SyncCoolingUI();coolingPanel?.Open();}
    public void CoolingPanelClosed(){coolingObservation.Enabled=false;CoolingView.A=CoolingView.B=-1;CoolingView.Path=null;CoolingView.Progress=1;}
    void SelectCoolingProduct(int id){coolingProductId=id;var item=shop.Find(id);CoolingView.Clear();if(item!=null){var p=item.Product;CoolingView.Select(new Piece{id=id,material=p.Material,heat=p.FinalHeat,shaped=p.Shaped,traits=p.Traits,data=p.Data});}coolingPanel?.TargetChanged();}
    public uint BeginCoolingReceipt(Step step){coolingProductId=-1;if(!ProcessingUnlocked)return CoolingView.Generation;if(step.material==9&&!DragonUnlocked||step.material!=9&&!CoolingUnlocked)return CoolingView.Generation;uint previous=CoolingView.Generation;var generation=CoolingView.Receipt(step);if(generation!=previous)coolingPanel?.TargetChanged();return generation;}
    public void EndCoolingReceipt(uint generation){CoolingView.Animate(generation,1);}
    public void CoolingProgress(uint generation,float progress){CoolingView.Animate(generation,progress);}
}
