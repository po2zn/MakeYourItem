# Hierarchy-based research UI

The scene contains an inactive **Forge Research UI** prefab instance. All 53 cards, 47 connections, headers, material quantity inputs, smith status view and the shared tooltip are serialized in Assets/UI/ForgeResearchUI.prefab. Expand it in the Hierarchy to edit individual objects.

- Runtime creates no tree GameObjects and has no tree OnGUI renderer. ForgeTreeBridge toggles the canvas and calls existing purchase rules.
- ForgeTreePanel updates labels/colors on opening, purchases and changed gameplay state; its 5 Hz state poll compares scalar values. Idle frames do not re-layout cards.
- ForgeTreePan changes only the content RectTransform. Titles remain card children.
- The tooltip and header have separate canvases. Tooltip content is measured on hover/state changes, not every mouse event; long content can be scrolled.
- Guide unlock notifications and ownership colors are retained.
- Noto Sans KR is included with its SIL Open Font License.

To deliberately regenerate the authored layout from the registry: **Tools > MakeYourItem > Rebuild Research UI** (Edit mode, SampleScene). This replaces the generated tree prefab/instance; keep manual layout edits by editing the prefab directly instead of rebuilding.

## Verification

Actual Unity Play Mode: purchases for automation, heat, material quantity and smith research/hiring; invalid quantity rejected; purchased cards green; wheel zoom; drag/release; fixed child text; shared tooltip; 20 page switches without additional objects. Test purchases ran with saving disabled and were discarded on exiting Play Mode.

Actual Game View captures: 800x600, 1280x720, 1920x1080, 3840x2160. Four pages were measured at each resolution using Unity UI Text.preferredHeight. Final results are in verification.txt; screenshots are adjacent.

Idle sample: 417 frames / 3 seconds, 0 card refreshes and 0 created UI objects. An explicit forced full refresh averaged 7.634 ms in this editor run. This is not a before/after FPS benchmark or a guarantee about world rendering performance.

Test helpers: AgentScripts/TestResearchUI.cs and AgentScripts/ResearchUIResolution.cs. Tests run through the local Unity Pipeline API.
