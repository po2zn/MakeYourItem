# UI text visibility audit — 2026-11-10

## Scope and changes
- Reviewed all direct GUI.Label / GUI.Button call sites and GUILayout text in Assets/Scripts. Syntax-colored code tokens retain their separate measured editor renderer.
- Technology and blacksmith cards: larger cards and separate title/status regions; shared zoom transform preserved; fixed controls fit their complete text.
- Guide: measured prose height includes glyph descenders; reserved scrollbar gutters; visible vertical and horizontal code scrollbars; larger code-scroll content budget.
- Menus, tutorial, shop, campaign and progress: wrapped automatic-height labels with glyph padding; minimum rather than fixed sale/ending button heights.
- Settings and pause menu: scrollable content for shorter displays. Blacksmith heading and message heights adapt to content.
- HUD: resource grid uses available screen width; resource counts use fitted text. Code hover and resource tooltips use the common measured tooltip renderer.
- AGENTS.md records the user's requirement to check resolution and overlapping text for future UI edits.

## Validation
- Full runtime C# compilation: passed.
- New Editor text-audit code compilation: passed.
- Git diff whitespace check: passed.
- Actual visual inspection: NOT completed. Windows window capture timed out twice.
- Unity native font-metric execution: NOT completed. Isolated batch editor exited 127: unable to open log file; no successful measurement report was produced.
- Tools > Validation > Audit UI Text Layout runs the reusable font-metric check inside Unity. It covers all tech titles, guide numeric/English variants at 190/330/550/910 content widths, and header widths 640/800/1280/1920/2560/3840. It writes Logs/UiTextAudit.txt.

## Remaining verification
Check actual gameplay at 800x600, 1280x720, 1920x1080 and 3840x2160, the minimum guide window size, long tooltips, and tree zoom extremes. Code and metric checks do not establish pixel-level visual correctness, especially for platform font fallback and arbitrary user-authored text.
