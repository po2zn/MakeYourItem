using UnityEngine;

// Explicit prefab references keep the asset set available in player builds.
public sealed class ForgeWorkshopAssets : ScriptableObject
{
    public GameObject blacksmith, forge, bucket, anvil, sword, hammer;
    public GameObject butterfly;
    public GameObject tongs;
    public GameObject floor;
    public GameObject crystalMaterial, starIronMaterial, frostWaterMaterial, dragonBoneMaterial;
    public GameObject frostStation, giantRunePillar, giantRuneCircle;
    public GameObject MaterialModel(int id)=>id==6?crystalMaterial:id==7?starIronMaterial:id==8?frostWaterMaterial:id==9?dragonBoneMaterial:null;
    public GameObject GiantRuneModel(int grade)=>grade>=3?giantRuneCircle:giantRunePillar;
    public bool Ready => blacksmith && forge && bucket && anvil && sword && hammer;
}
