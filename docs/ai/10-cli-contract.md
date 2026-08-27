# CLI Contract

## Purpose

The CLI validates that core capabilities are independent of WPF and provides a stable automation surface for CI and AI.

## Executable

`spineviewerwpf`

TASK-001 implements `inspect` and `render` for Runtime 4.1; TASK-023 and TASK-024 extend both commands to all historical fixture-backed lines from 2.1.08 through 4.1.00, TASK-026 verifies the official 3.8.55 cache, TASK-041 adds official 4.2 JSON/binary support, and TASK-051 adds official 4.3 JSON/binary support. The other MVP commands remain planned.

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

The current adapter catalog accepts `2.1.08`, `2.1.25`, `3.1.07`, `3.2.xx`,
`3.4.02`, `3.5.51`, `3.6.32`, `3.6.39`, `3.6.53`, `3.7.94`, `3.8.95`,
`4.0.31`, `4.0.64`, `4.1`, `4.2`, and `4.3`.

Schema changes follow product versioning. Additive fields are preferred; renamed or retyped fields require migration notes.
