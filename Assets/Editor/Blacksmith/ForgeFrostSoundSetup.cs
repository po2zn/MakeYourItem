using System.IO;
using UnityEditor;
using UnityEngine;
public static class ForgeFrostSoundSetup
{
 public static void Build(){var lib=ForgeSfxLibrary.Instance;lib.frostDrop=Save("FrostWater_Drop",true);lib.frostCooling=Save("FrostWater_Cooling",false);EditorUtility.SetDirty(lib);AssetDatabase.SaveAssets();}
 static AudioClip Save(string name,bool drop){const int rate=44100;var samples=ForgeSfxLibrary.FrostSamples(rate,drop);string path="Assets/Sound/SFX/"+name+".wav";
  using(var writer=new BinaryWriter(File.Create(path))){writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples.Length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples.Length*2);foreach(float value in samples)writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(value,-1,1)*32767));}
  AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
 }
}
