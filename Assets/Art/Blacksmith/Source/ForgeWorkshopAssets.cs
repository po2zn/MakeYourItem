using UnityEngine;

// Explicit prefab references keep the asset set available in player builds.
public sealed class ForgeWorkshopAssets : ScriptableObject
{
    public GameObject blacksmith, forge, bucket, anvil, sword, hammer;
    public GameObject butterfly;
    public GameObject tongs;
    public GameObject floor;
    public bool Ready => blacksmith && forge && bucket && anvil && sword && hammer;
}
