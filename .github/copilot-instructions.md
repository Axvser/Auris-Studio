# Copilot Instructions

## 项目指南
- User prefers the editor settings window to be much more compact vertically, especially reducing the vertical size and chip/button size in the alignment settings area. The alignment section should also be scrollable.
- User prefers the settings dialog to use explicit fixed pixel sizes for its layout and controls rather than adaptive sizing.
- User prefers ASCII-only, simplified button text in the settings dialog to avoid encoding/garbling issues.

## Control-Curve Editor
- Reserve a left spacer matching the piano-note area alignment.
- Render controller curves as horizontal step lines.
- Keep one extra virtualized event before and after the viewport to avoid visual truncation.