using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
public partial class ForgeMvp
{
 [SerializeField] ForgeGameUI hierarchyGame;public ForgeGameUI HierarchyGame=>hierarchyGame;public void SetHierarchyGame(ForgeGameUI ui){hierarchyGame=ui;}
 static void ClearUIFocus(){if(Event.current!=null)GUI.FocusControl(null);else if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);}
 void SyncHierarchyGame(){if(!hierarchyGame)return;hierarchyGame.Bind(this);bool visible=!showTitle&&!showTechTree||!showTitle&&showGameMenu;if(hierarchyGame.gameObject.activeSelf!=visible){hierarchyGame.gameObject.SetActive(visible);if(visible)hierarchyGame.Refresh();}if(DayCycle)DayCycle.UseHierarchyUI=true;hierarchyGame.workspace.localScale=Vector3.one*workspaceZoom;hierarchyGame.workspace.anchoredPosition=new Vector2(workspaceOffset.x,-workspaceOffset.y)*workspaceZoom/Mathf.Max(.01f,hierarchyGame.canvas.scaleFactor);if(returnToTitleRequested&&!busy&&!tutorialAnimating)FinishTitleReturn();}
 public void ClampGameCodeWindows(){if(!hierarchyGame)return;var bounds=((RectTransform)hierarchyGame.transform).rect;foreach(var w in codeWindows){w.rect.x=Mathf.Clamp(w.rect.x,8,Mathf.Max(8,bounds.width-w.rect.width-8));w.rect.y=Mathf.Clamp(w.rect.y,Mathf.Min(ToolbarHeight+15,bounds.height-200),Mathf.Max(Mathf.Min(ToolbarHeight+15,bounds.height-200),bounds.height-w.rect.height-52));}}
 public void GameCodeEdit(int id,string value){var w=codeWindows.Find(v=>v.id==id);if(w==null)return;w.text=value;if(codeWindows[activeWindow]==w)source=value;if(w.diagnostic!=null&&w.diagnostic.Source!=value){w.diagnostic=null;w.showDiagnostic=false;}}
 public void GameCodeName(int id,string value){var w=codeWindows.Find(v=>v.id==id);if(w!=null&&!string.IsNullOrWhiteSpace(value))w.name=value.Trim();}
 public void GameCodeBounds(int id,Rect rect){var w=codeWindows.Find(v=>v.id==id);if(w!=null)w.rect=rect;}
 public void GameCodeAction(int id,string action){int index=codeWindows.FindIndex(w=>w.id==id);if(index<0)return;activeWindow=index;source=codeWindows[index].text;workspaceWindow=codeWindows[index].rect;var w=codeWindows[index];switch(action){case "run":Execute(false);break;case "step":Execute(true);break;case "stop":StopExecution(true);break;case "close":deleteWindowId=id;break;case "reveal":if(w.diagnostic!=null){if(w.collapsed){w.collapsed=false;w.rect.height=w.expandedHeight;}w.diagnostic.GetLineSpan(out int start,out int length);hierarchyGame.Refresh();hierarchyGame.codes[id].Reveal(start);}break;case "collapse":if(!w.collapsed){w.expandedHeight=w.rect.height;w.rect.height=36;}else w.rect.height=w.expandedHeight;w.collapsed=!w.collapsed;break;}hierarchyGame.Refresh();}
 public void GameWorkspaceMove(Vector2 delta){workspaceOffset+=new Vector2(delta.x,-delta.y)/workspaceZoom;}
 public void GameWorkspaceZoom(float delta,Vector2 position){if(EventSystem.current?.currentInputModule is UnityEngine.InputSystem.UI.InputSystemUIInputModule module)delta/=Mathf.Max(.001f,module.scrollDeltaPerTick);float next=Mathf.Clamp(workspaceZoom*Mathf.Pow(1.08f,Mathf.Clamp(delta,-4,4)),.25f,2);var p=new Vector2(position.x,Screen.height-position.y);workspaceOffset+=p/next-p/workspaceZoom;workspaceZoom=next;}
 public void GameTutorialInput(string value){tutorialInput=value;}
 public void GameUIAction(string key){
 if(key.StartsWith("guide-")||key=="guide-copy"){GameGuideAction(key);return;}
 if(key.StartsWith("product:")){int.TryParse(key.Substring(8),out selectedProductId);return;}
 if(key.StartsWith("material:")){if(!tutorialAnimating&&lesson.Stage==0&&lesson.SelectMaterial(int.Parse(key.Substring(9)),out tutorialFeedback))tutorialInput=lesson.Code;return;}
 if(key.StartsWith("resource:")){if(!HierarchyBusy){int id=int.Parse(key.Substring(9));if(id==0)gold=(int)Math.Min(int.MaxValue,(long)gold+1000);else if(id==1)iron=(int)Math.Min(int.MaxValue,(long)iron+1000);else{var s=Array.Find(materialStocks,x=>x.material==id);if(s!=null){s.unlocked=true;s.count=Math.Min(long.MaxValue-1000,s.count)+1000;}}SaveProgress();}return;}
 switch(key){
 case "tree":showProgress=false;showTechTree=true;break;case "guide":showGuideBook=!showGuideBook;RefreshGameGuide();break;
 case "add":int id=nextWindowId++;codeWindows.Add(new CodeWindow{id=id,name="f"+(id+1),rect=new Rect(new Vector2(70+id%6*20,ToolbarHeight+15+id%6*20)/workspaceZoom-workspaceOffset,new Vector2(360,240))});activeWindow=codeWindows.Count-1;source="";break;
 case "sales":showSalesPanel=!showSalesPanel;break;case "debug":showDebugWindow=!showDebugWindow;break;case "debug-clear":debugOutput.Clear();break;
 case "resources":hierarchyGame.Active("Developer",!hierarchyGame.Node("Developer").gameObject.activeSelf);break;case "developer-close":hierarchyGame.Active("Developer",false);break;
 case "progress":showProgress=true;break;case "back":showProgress=false;break;case "menu":ToggleGameMenu();break;case "title":RequestTitleReturn();break;
 case "save":SaveProgress();break;case "numeric":TranslateCurrent(false);break;case "english":TranslateCurrent(true);break;
 case "sell":SellProduct(selectedProductId);break;case "delete-no":deleteWindowId=-1;break;case "delete-yes":int index=codeWindows.FindIndex(w=>w.id==deleteWindowId);if(index>=0)codeWindows.RemoveAt(index);activeWindow=Mathf.Clamp(activeWindow,0,Mathf.Max(0,codeWindows.Count-1));source=codeWindows.Count>0?codeWindows[activeWindow].text:"";deleteWindowId=-1;break;
 case "tutorial":OpenTutorial();break;case "tutorial-close":CloseTutorial();break;case "tutorial-start":if(!tutorialAnimating)StartCoroutine(RevealTutorialLesson());break;
 case "tutorial-submit":if(!tutorialAnimating)SubmitTutorial();break;case "tutorial-prev":PreviousTutorial();break;case "tutorial-hint":tutorialHint=!tutorialHint;break;case "tutorial-copy":GUIUtility.systemCopyBuffer=lesson.Code;break;case "tutorial-restart":RestartTutorialFromDebug();break;
 case "day":if(!HierarchyBusy){DayCycle.Clock.Restore(DayCycle.Day+1,DayCycle.CurrentHour);SaveProgress();}break;case "reset":ResetProgressForDebug();break;
 case "ending":endingSeen=false;showProgress=false;break;case "ending-continue":endingSeen=true;SaveProgress();break;
 }
 }
 public void RefreshGameUI(){if(!hierarchyGame||showTitle)return;var ui=hierarchyGame;guideBook.SyncResearch(technology,blacksmiths);
 bool ending=campaignComplete&&!endingSeen&&!busy&&executionSession==null&&!showTutorial;
 bool world=!showGameMenu&&!showProgress&&!showTutorial&&!ending;
 ui.Active("Play",world);ui.Active("Menu",showGameMenu);ui.Active("Progress",showProgress&&!showGameMenu);ui.Active("Tutorial",showTutorial&&!showGameMenu);ui.Active("Ending",ending&&!showGameMenu);ui.Active("Delete",deleteWindowId>=0&&world);
 if(world){if(!windowsInitialized){windowsInitialized=true;nextWindowId=1;codeWindows.Add(new CodeWindow{id=0,name="f1",rect=InitialCodeWindow});}
 ui.Text("Gold","자금\n"+ForgeMaterialHud.FormatCount(gold)+"G");ui.Text("Products","완성품\n"+products);ui.Text("Zoom","확대\n"+Mathf.RoundToInt(workspaceZoom*100)+"%");
 ui.Text("DayNotice",DayCycle.AnnouncementAlpha>0?"Day "+DayCycle.Day:"");ui.Tint("DayNotice",new Color(1,1,1,DayCycle.AnnouncementAlpha));ui.Text("Clock","Day "+DayCycle.Day+" · "+((int)DayCycle.CurrentHour).ToString("00")+":"+((int)(DayCycle.CurrentHour*60)%60).ToString("00"));
 for(int material=1;material<=9;material++){var stock=Array.Find(materialStocks,m=>m.material==material);bool visible=material==1||stock!=null&&stock.unlocked;ui.Active("MaterialTile"+material,visible);if(visible){long count=material==1?iron:stock.count;var info=ForgeMaterialGuide.Find(material);ui.Text("MaterialCount"+material,ForgeMaterialHud.FormatCount(count));ui.Tooltip("MaterialTile"+material,info.Name+" · "+count.ToString("N0")+"개\n"+guideBook.Reader.Prose(info.Description));}}

 var ids=new HashSet<int>();foreach(var w in codeWindows){ids.Add(w.id);ui.Code(w.id,w.name,w.text,w.rect,w.collapsed,executionSession!=null,!busy,w.executionLine,w.showDiagnostic&&w.diagnostic!=null?w.diagnosticCaption+" · "+w.diagnostic.Line+"줄: "+w.diagnostic.Message:null);}ui.PruneCodes(ids);
 ui.Active("Sales",showSalesPanel);ui.Active("Debug",showDebugWindow);ui.Active("Guide",showGuideBook);
 if(showDebugWindow)ui.Text("DebugText",debugOutput.Text);
 if(showSalesPanel){ui.Text("Report","실행 도면: "+resultOwner+(busy?" · 제작 중":"")+"\n"+report);ids.Clear();foreach(var item in shop.Items){ids.Add(item.Id);ui.Sale(item.Id,"#"+item.Id+" "+item.Product.DisplayName+" · 제작비 "+item.Product.CashCost+"G",selectedProductId==item.Id);}ui.PruneSales(ids);var selected=shop.Find(selectedProductId);ui.Text("SaleDetails",selected==null?"완성품을 선택하면 판매 견적이 표시됩니다.":ForgePricing.Describe(selected.Product,false));ui.Label("Sell",selected==null?"판매할 완성품을 선택하세요":"선택 완성품 판매 · +"+ForgePricing.Quote(selected.Product).SalePrice+"G");ui.Enabled("Sell",selected!=null&&!HierarchyBusy);}
 ui.Text("GuideNotice",guideBook.HasNew?"도면 안내서에 새 내용이 추가되었습니다":"");ui.Tint("GuideNotice",Color.Lerp(Color.white,new Color(1,.75f,.25f),.5f+.5f*Mathf.Sin(Time.unscaledTime*6)));if(showGuideBook)RefreshGameGuide();
 }
 if(showProgress){var text=new StringBuilder("공방 기록 · Day "+DayCycle.Day+"\n골드 "+gold+"G · 다이아몬드 "+diamonds+" · 판매 "+totalSales+"개 · 누적 판매액 "+earnedGold+"G\n가속 "+craftSpeed+"배 · 자동판매 "+(autoSell?"켜짐":"꺼짐")+"\n\n성장 목표 · "+CampaignObjective+"\n\n최종 의뢰: "+ForgeCampaign.FinalGoal+"\n\n발견 도감\n");foreach(var d in discoveries)text.AppendLine(d+" · "+DiscoveryRule(d));text.Append("\n"+saveMessage+"\n제작·판매·구매 후 자동 저장합니다.");ui.Text("ProgressText",text.ToString());ui.Active("EndingReplay",campaignComplete);}
 ui.Text("MenuText",returnToTitleRequested?"진행 중인 작업을 마무리하고 저장하는 중입니다…":"진행 상황을 저장하고 타이틀로 이동할 수 있습니다.\n"+saveMessage);
 if(showTutorial){ui.Active("TutorialWelcome",showTutorialWelcome);ui.Active("TutorialPrevious",!showTutorialWelcome);ui.Active("TutorialCopy",lesson.Complete);ui.Active("TutorialLesson",!showTutorialWelcome);ui.Active("TutorialEditor",!showTutorialWelcome);ui.Input("TutorialInput",tutorialInput);ui.Text("TutorialHeading","첫 검 만들기 · "+Mathf.Min(lesson.Stage+1,8)+" / 8");ui.Text("TutorialInstructions",lesson.Complete?"첫 번째 검을 완성했습니다! 도면을 복사하거나 다시 연습할 수 있습니다.":ForgeTutorialLesson.Titles[lesson.Stage]+"\n\n"+ForgeTutorialLesson.Instructions[lesson.Stage]+(tutorialHint?"\n\n힌트: "+ForgeTutorialLesson.Hints[lesson.Stage]:""));ui.Text("TutorialFeedback",tutorialAnimating?"작업물의 변화를 확인하세요…":tutorialFeedback);ui.Active("TutorialMaterials",lesson.Stage==0);ui.Enabled("TutorialSubmit",!tutorialAnimating&&lesson.Stage>0&&!lesson.Complete);ui.Enabled("TutorialPrevious",!tutorialAnimating&&lesson.Stage>0);ui.Enabled("TutorialClose",!tutorialAnimating);ui.Enabled("TutorialStart",!tutorialAnimating);}
 ui.Text("EndingText","당신의 도면이 전설이 되었습니다\n\n작은 철 조각으로 시작한 공방에서 별철과 용뼈, 마력과 룬을 하나의 검으로 완성했습니다.\n\n명장의 마지막 의뢰 완료\n"+ForgeCampaign.FinalGoal+"\n\nDay "+DayCycle.Day+" · 판매 "+totalSales+"개 · 누적 판매액 "+earnedGold+"G");
 }
}
