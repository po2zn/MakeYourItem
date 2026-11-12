using UnityEngine;
public sealed partial class ForgeGuideReader
{
 public bool VisibleEntry(int i)=>visible.Contains(i);public bool FreshEntry(int i)=>fresh.Contains(i);public bool VisibleMaterial(int i)=>visibleMaterials.Contains(i);public bool FreshMaterial(int i)=>freshMaterials.Contains(i);public bool VisibleSection(string value)=>SectionVisible(value);
 public void ReadEntry(int i){fresh.Remove(i);}public void ReadMaterial(int i){freshMaterials.Remove(i);}
 public string IntroductionPage=>IntroductionText;
 public string Prose(string text)=>VisibleProse(text);
}
public partial class ForgeMvp
{
 string gameGuideSection="게임 소개";int gameGuideEntry=-1,gameGuideMaterial=-1;bool gameGuideFresh;string gameGuideCopy="";string guideSignature;
 public void GameGuideAction(string key){if(key.StartsWith("guide-section:")){gameGuideSection=key.Substring(14);gameGuideEntry=gameGuideMaterial=-1;gameGuideFresh=false;}else if(key.StartsWith("guide-entry:")){gameGuideEntry=int.Parse(key.Substring(12));gameGuideMaterial=-1;guideBook.Reader.ReadEntry(gameGuideEntry);}else if(key.StartsWith("guide-material:")){gameGuideMaterial=int.Parse(key.Substring(15));gameGuideEntry=-1;guideBook.Reader.ReadMaterial(gameGuideMaterial);}else if(key=="guide-new"){gameGuideSection="";gameGuideFresh=true;gameGuideEntry=gameGuideMaterial=-1;hierarchyGame.Input("GuideSearch","");}else if(key=="guide-back"){gameGuideEntry=gameGuideMaterial=-1;}else if(key=="guide-copy")GUIUtility.systemCopyBuffer=gameGuideCopy;guideSignature=null;RefreshGameGuide();}
 public void GameGuideSearch(){gameGuideEntry=gameGuideMaterial=-1;gameGuideFresh=false;if(hierarchyGame.Input("GuideSearch").Length>0)gameGuideSection="";guideSignature=null;RefreshGameGuide();}
 public void RefreshGameGuide(){if(!hierarchyGame||!hierarchyGame.Bound)return;var ui=hierarchyGame;var reader=guideBook.Reader;reader.Sync(technology,blacksmiths);string query=ui.Input("GuideSearch");var book=ui.Node("Guide").GetComponent<ForgeLegacyBook>();if(book)book.Layout(gameGuideSection=="게임 소개"&&gameGuideEntry<0&&gameGuideMaterial<0&&query.Length==0,reader.HasNew);string signature=HierarchyTreeStamp+"|"+reader.NewCount+"|"+gameGuideSection+"|"+gameGuideEntry+"|"+gameGuideMaterial+"|"+query+"|"+englishGuideUnlocked+"|"+gameGuideFresh;if(signature==guideSignature)return;guideSignature=signature;
 ForgeGuideNewShimmer.Apply(ui.Node("GuideNew"));
 for(int i=0;i<ForgeGuideReader.Sections.Length;i++)ui.Active("GuideSection"+i,reader.VisibleSection(ForgeGuideReader.Sections[i]));
 ui.Active("GuideNew",reader.HasNew);ui.Label("GuideNew","새 내용 "+reader.NewCount+"개");bool detail=gameGuideEntry>=0||gameGuideMaterial>=0||query.Length==0&&(gameGuideSection=="게임 소개"||gameGuideSection=="테크 트리");ui.Active("GuideIndex",!detail);ui.Active("GuideDetail",detail);gameGuideCopy="";
 for(int i=0;i<BlueprintGuide.Entries.Count;i++){var entry=BlueprintGuide.Entries[i];bool show=reader.VisibleEntry(i)&&(!gameGuideFresh||reader.FreshEntry(i))&&(query.Length>0||gameGuideSection==""||gameGuideSection==entry.Section)&&entry.Matches(query,englishGuideUnlocked);ui.Active("GuideEntry"+i,show);if(show)ui.Label("GuideEntry"+i,(reader.FreshEntry(i)?"새 항목 · ":"")+entry.HeadingFor(englishGuideUnlocked));}
 for(int i=0;i<ForgeMaterialGuide.Entries.Count;i++)ui.Active("GuideMaterial"+i,reader.VisibleMaterial(i)&&(gameGuideSection=="재료"||gameGuideFresh&&reader.FreshMaterial(i)));
 string heading="",body="",after="";
 if(gameGuideEntry>=0&&reader.VisibleEntry(gameGuideEntry)){var e=BlueprintGuide.Entries[gameGuideEntry];heading=e.HeadingFor(englishGuideUnlocked);gameGuideCopy=e.ExampleFor(englishGuideUnlocked);if(e.Feature==BlueprintFeature.Speed&&technology.MaxSpeedLevel==1)gameGuideCopy=gameGuideCopy.Replace("222(2)","222(1)").Replace("speed(2)","speed(1)");body=reader.Prose(e.DescriptionFor(englishGuideUnlocked))+"\n\n연구 및 준비\n"+reader.Prose(e.ResearchFor(englishGuideUnlocked))+"\n\n예제";after=(e.IsFunctionReference?"":e.Expected)+"\n\n"+reader.Prose(e.NoteFor(englishGuideUnlocked));}
 else if(gameGuideMaterial>=0&&reader.VisibleMaterial(gameGuideMaterial)){var m=ForgeMaterialGuide.Entries[gameGuideMaterial];heading=m.Heading;body=BlueprintResearchGuide.MaterialInfo(m.Number,englishGuideUnlocked)+"\n\n"+reader.Prose(m.Description);}
 else if(gameGuideSection=="테크 트리"){heading="현재 테크 트리 · 성장 안내";body=BlueprintResearchGuide.UnlockedOverview(technology,englishGuideUnlocked);}else{heading=BlueprintGuide.IntroductionTitle;body=reader.IntroductionPage;}
 ui.Text("GuideHeading",heading);ui.Text("GuideBody",body);ui.GuideExample(gameGuideCopy,after.Trim());ui.Active("GuideCopy",gameGuideCopy.Length>0);
 }
}
