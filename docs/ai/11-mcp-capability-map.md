# MCP Capability Map

## Status

Planning only. Do not implement until Application use cases and CLI schemas are stable.

## Architecture Rule

MCP, CLI, and WPF call the same Application handlers. MCP must not construct shell command strings as its primary integration.

## Planned Resources

- `spineviewerwpf://project/context`
- `spineviewerwpf://project/runtime-matrix`
- `spineviewerwpf://project/architecture`
- `spineviewerwpf://asset/{id}/metadata`
- `spineviewerwpf://asset/{id}/diagnostics`

## Planned Tools

| Tool | Use case | Effect | Confirmation |
|---|---|---|---|
| `list_supported_runtimes` | ListSupportedRuntimes | read | no |
| `detect_spine_version` | DetectSpineVersion | read | no |
| `inspect_spine_asset` | InspectSpineAsset | read | no |
| `validate_spine_asset` | ValidateSpineAsset | read | no |
| `list_spine_animations` | ListAnimations | read | no |
| `list_spine_skins` | ListSkins | read | no |
| `render_spine_frame` | RenderFrame | compute/write | path policy |
| `export_spine_animation` | ExportAnimation | write/high cost | yes |

## Security Boundaries

- local `stdio` transport first
- explicit allowed roots for read/write
- no arbitrary command execution
- output overwrite disabled by default
- maximum dimensions, frames, duration, and concurrency
- cancellation and progress required for high-cost tools
