using System;using UnityEngine;
[Serializable] public sealed class ForgeLeaderboardName {public string name;}
[Serializable] public sealed class ForgeLeaderboardEntry {public string playerId,playerName;public int rank;public double score;public ForgeLeaderboardName metadata;public string Name=>ForgeLeaderboardData.CleanName(metadata?.name??playerName);}
[Serializable] public sealed class ForgeLeaderboardScores {public ForgeLeaderboardEntry[] results=Array.Empty<ForgeLeaderboardEntry>();}
[Serializable] public sealed class ForgeLeaderboardData {
 public const string BestKey="MakeYourItem.Leaderboard.HighestSold.v1";
 public string name="",sessionToken="",playerId="",updatedAt="";public bool nameRegistered;public ForgeLeaderboardScores cached=new ForgeLeaderboardScores();
 public static int Best=>Math.Max(0,PlayerPrefs.GetInt(BestKey,0));
 public static bool Record(int price,bool sword,bool practice){if(practice||!sword||price<=Best)return false;PlayerPrefs.SetInt(BestKey,price);PlayerPrefs.Save();return true;}
 public static string CleanName(string value){var result=new System.Text.StringBuilder();foreach(char c in value??""){if(char.IsLetterOrDigit(c)||c==' '||c=='_'||c=='-')result.Append(c);if(result.Length>=16)break;}return result.ToString().Trim();}
 public static ForgeLeaderboardData Load(string key){try{return JsonUtility.FromJson<ForgeLeaderboardData>(PlayerPrefs.GetString("MakeYourItem.Leaderboard."+key,"{}"))??new ForgeLeaderboardData();}catch{return new ForgeLeaderboardData();}}
 public void Save(string key){PlayerPrefs.SetString("MakeYourItem.Leaderboard."+key,JsonUtility.ToJson(this));PlayerPrefs.Save();}
}
