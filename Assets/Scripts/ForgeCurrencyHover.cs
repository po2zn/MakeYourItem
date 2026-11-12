using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;

// Refresh while stationary too, so purchases and rewards never leave a stale balance.
public sealed class ForgeCurrencyHover : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    public Action<Vector2> show;
    public Action hide;
    bool inside;
    Vector2 pointer;
    float nextRefresh;
    public static string Details(ForgeMvp game) => "골드: " + game.HierarchyGold.ToString("N0", CultureInfo.InvariantCulture)
        + "개\n다이아몬드: " + game.HierarchyDiamonds.ToString("N0", CultureInfo.InvariantCulture) + "개";
    public void OnPointerEnter(PointerEventData e) { inside=true;pointer=e.position;Show(); }
    public void OnPointerMove(PointerEventData e) { pointer=e.position;if(inside)Show(); }
    public void OnPointerExit(PointerEventData e) { Close(); }
    void Update() { if(inside && Time.unscaledTime>=nextRefresh)Show(); }
    void Show() { show?.Invoke(pointer);nextRefresh=Time.unscaledTime+.15f; }
    void Close() { if(inside)hide?.Invoke();inside=false; }
    void OnDisable() { Close(); }
}
