using UnityEngine;
[CreateAssetMenu(menuName="Forge/Leaderboard Settings")]
public sealed class ForgeLeaderboardSettings : ScriptableObject {
 public bool enabledForPlayers;
 public string projectId="cb1fbf8a-2e62-412a-9f01-fb1b4426efb7";
 public string environment="production";
 public string leaderboardId="highest-sword-sale";
 public string Key=>projectId+"."+environment+"."+leaderboardId;
 public bool Ready=>enabledForPlayers&&System.Guid.TryParse(projectId,out _)&&!string.IsNullOrWhiteSpace(leaderboardId);
}
