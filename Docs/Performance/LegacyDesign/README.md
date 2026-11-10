# Original in-game design restoration

Restores the original IMGUI appearance using authored Canvas/prefab objects. Research UI is unchanged. Reference: reference-1280.png, old ForgeMvp/ForgeGuideUI rendering code.

Restored status tiles, four outline toolbar icons, stock icons/counts, thin code headers, original OS font selection, skin borders, paper guide with external ribbon bookmarks, compact floating panels and tutorial controls. The editor-only ForgeLegacyUIStyle applies the style to prefabs; runtime layout only positions existing objects.

Validation: Unity compilation passed; 22 functional assertions passed; 154 numeric/English guide pages and minimum guide/code sizes passed. Event-queue tests passed newline indentation, Tab, undo and long-code scrolling without caret movement (not physical keyboard/IME testing). Layout measurements passed 7 pages at 800x600, 1280x720, 1920x1080 and 3840x2160. Captures at all four resolutions were visually reviewed. Long content remains accessible through scroll views.
