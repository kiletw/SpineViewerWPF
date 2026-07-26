# CLI Contract

## Purpose

The CLI validates that core capabilities are independent of WPF and provides a stable automation surface for CI and AI.

## Executable

`spineviewerwpf`

TASK-001 implements `inspect` and `render` for Runtime 4.1; TASK-023 extends both commands to verified 4.0.64 project-authored fixtures with explicit or automatic Runtime selection. The other MVP commands remain planned.

## MVP Commands

```text
spineviewerwpf runtime list
spineviewerwpf inspect <skeleton> [--atlas <path>] [--runtime <line>] [--format json]
spineviewerwpf validate <skeleton> [--atlas <path>] [--format json]
spineviewerwpf animation list <skeleton> [--atlas <path>] [--format json]
spineviewerwpf skin list <skeleton> [--atlas <path>] [--format json]
spineviewerwpf render <skeleton> --animation <name> --time <seconds> --output <png>
```

## Output Rules

- `stdout`: result data only
- `stderr`: diagnostics, progress, and logs
- `--format json`: stable machine-readable result
- no WPF dialog, MessageBox, or implicit prompt
- destructive overwrite requires `--overwrite`

## Exit Codes

| Code | Meaning |
|---:|---|
| 0 | success |
| 1 | general execution failure |
| 2 | invalid arguments |
| 3 | unsupported Runtime |
| 4 | missing asset dependency |
| 5 | Runtime load failure |
| 6 | render failure |
| 7 | export failure |
| 130 | canceled |

## Versioning

Schema changes follow product versioning. Additive fields are preferred; renamed or retyped fields require migration notes.
