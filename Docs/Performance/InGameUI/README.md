# Hierarchy in-game UI

The SampleScene root **Forge In-game UI** references Assets/UI/ForgeGameUI.prefab. Edit its Canvas children directly. The prebuilt screens include the HUD, guide/bookmarks, sales/results, debug tools/output, workshop records, tutorial, ending, menu and delete confirmation. Code windows and sale rows instantiate Assets/UI/ForgeCodeWindow.prefab and Assets/UI/ForgeSaleItem.prefab only when data is added. There is no per-frame UI construction.

ForgeGameBridge connects retained controls to existing purchases, inventory, execution, saves and tutorial logic. The old in-game IMGUI path is bypassed when the hierarchy reference is assigned; title/settings retain their previous implementation. ForgeDayCycle forwards its day announcement to the retained HUD. The research UI remains separate.

Use Tools > MakeYourItem > Rebuild In-game UI only to regenerate the authored layout: it overwrites prefab layout edits. Normal play never invokes the builder.

Validation artifacts: functional-checks.txt (runtime execution and actions), layout-checks.txt (text-height metrics at four resolutions), and captured PNGs (visual inspection). Metrics are not a substitute for visual inspection or an FPS benchmark. Test runs disable progress saving.

Input verification uses Unity Event.QueueEvent plus the real InputField event handler (newline/Tab/Ctrl+Z), not physical keyboard injection. Windows computer-use capture timed out twice, so physical keyboard/IME confirmation is not claimed. Material icons are baked once into Assets/UI/MaterialIcons in the editor.
