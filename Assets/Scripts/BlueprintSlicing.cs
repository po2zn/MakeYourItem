using System;
using System.Collections.Generic;
using System.Threading.Tasks;
public sealed partial class BlueprintRuntime {
 struct SliceRange {public int start,stop,step;}
 async Task<SliceRange> SliceBounds(BlueprintExpr e,Dictionary<string,object> env,int length){
 var a=await Eval(e.Args[1],env);var b=await Eval(e.Args[2],env);var c=await Eval(e.Args[3],env);currentLine=e.Token.Line;
 int step=c==null?1:Int(c);if(step==0)Fail("슬라이스 간격(step)은 0일 수 없습니다.");
 int Bound(object value,bool start){if(value==null)return step>0?(start?0:length):(start?length-1:-1);long n=Int(value);if(n<0)n+=length;return (int)Math.Max(step>0?0:-1,Math.Min(step>0?length:length-1,n));}
 return new SliceRange{start=Bound(a,true),stop=Bound(b,false),step=step};
 }
 List<int> SliceIndices(SliceRange range){var indices=new List<int>();for(long i=range.start;range.step>0?i<range.stop:i>range.stop;i+=(long)range.step){if(indices.Count>=CollectionLimit)Fail("슬라이스는 최대 1024개입니다.");indices.Add((int)i);}return indices;}
 async Task<object> Slice(BlueprintExpr e,Dictionary<string,object> env){
 var value=await Eval(e.Args[0],env);
 if(value is string text){var chars=new List<string>();for(int i=0;i<text.Length;i++){int size=char.IsHighSurrogate(text[i])&&i+1<text.Length&&char.IsLowSurrogate(text[i+1])?2:1;chars.Add(text.Substring(i,size));i+=size-1;}var indices=SliceIndices(await SliceBounds(e,env,chars.Count));Allocate(indices.Count);var output=new System.Text.StringBuilder();foreach(int i in indices)output.Append(chars[i]);return output.ToString();}
 var sequence=Sequence(value);var selected=SliceIndices(await SliceBounds(e,env,sequence.Count));Allocate(selected.Count);var result=new List<int>();foreach(int i in selected)result.Add(sequence[i]);return value is int[]?(object)result.ToArray():result;
 }
 async Task AssignSlice(BlueprintExpr target,object value,Dictionary<string,object> env){
 var container=await Eval(target.Args[0],env);if(!(container is List<int>))Fail("슬라이스 대입 대상은 리스트여야 합니다.");var list=(List<int>)container;var replacement=new List<int>(Sequence(value));var range=await SliceBounds(target,env,list.Count);var indices=SliceIndices(range);
 if(range.step!=1){if(indices.Count!=replacement.Count)Fail("간격이 있는 슬라이스 대입은 선택한 원소 수와 대입할 원소 수가 같아야 합니다.");for(int i=0;i<indices.Count;i++)list[indices[i]]=replacement[i];}
 else {int removed=Math.Max(0,range.stop-range.start);int size=list.Count-removed+replacement.Count;if(size>CollectionLimit)Fail("리스트는 최대 1024개입니다.");Allocate(replacement.Count);list.RemoveRange(range.start,removed);list.InsertRange(range.start,replacement);}
 }
}
