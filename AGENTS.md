# AGENTS.md
<!-- Funplay Unity MCP managed project skills -->

# Funplay Unity MCP Project Guidance

This file is managed by Funplay MCP for Unity.

## Installed project skills

- `funplay-using-funplay-skills` - Learn how to discover, choose, and apply FunPlay workflows in an agent session.
- `funplay-unity-prefab-workflow` - Safely plan and review Unity prefab, scene, and serialized asset edits.
- `funplay-gameplay-prototyping` - Turn a rough game concept into a small, buildable prototype spec.
- `funplay-level-design-review` - Review flow, readability, guidance, and pacing in a level or encounter layout.
- `funplay-sprite-sheet` - Split one sprite sheet into frame images and plan clean export workflows.
- `funplay-normal-map` - Generate or review normal-map workflows for 2D and 3D game textures.
- `funplay-audio-format-convert` - Convert game audio between wav, ogg, and mp3 with pipeline awareness.
- `funplay-game-audio-polish` - Review game audio assets for loudness, looping, and implementation readiness.
- `funplay-texture-atlas` - Plan atlas grouping, naming, padding, and packing strategy for 2D/UI assets.
- `funplay-ui-slicing-checklist` - Review UI sprites for slicing, nine-patch, and export readiness.

## Codex workflow rules

- Prefer project-local Funplay skills under `.agents/skills/`.
- Use `execute_code` as the primary Unity automation tool.
- Call `request_recompile` immediately after editing scripts or `Assets/` files outside Unity.
- If recompilation triggers a domain reload, call `get_reload_recovery_status`.
- Avoid changing `Library/`, `Temp/`, `Logs/`, or `obj/`.

## Project

- Project root: `D:\Multiplayer-Gun-Battle`
- Product name: `球球战争`

## Notes

- Re-run `Funplay > MCP Server > Configure Skills` after changing selected skills or platforms.
