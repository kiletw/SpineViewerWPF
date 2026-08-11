# TASK-047 historical Runtime relocation evidence

The historical Runtime projects previously compiled source from
`SpineViewerWPF/SpineLibrary`. TASK-047 relocates only the active compile inputs
into the matching `runtimes/SpineRuntime.V*/src` directory.

For each Runtime line, the pre-move and post-move manifests were built from the
sorted Runtime-relative path and SHA-256 of every active `.cs` file. The
manifest values below are SHA-256 digests encoded as Base64. Every pre/post
pair matched.

| Runtime | Active `.cs` | Manifest SHA-256 (Base64) |
|---|---:|---|
| 2.1.08 | 26 | `OTUrQXBzb/jOJ6XucIDl4rMgmy3s0lyW5OWSop0sOA4=` |
| 2.1.25 | 27 | `dmmmXLlsX283bSh6RXgN2EAeVkzjx5ScPb270mXGv/Q=` |
| 3.1.07 | 34 | `LEkp4cKntBr4FUpoP4njvBVc+CnhTDC4VpH65hOzyYU=` |
| 3.2.xx | 34 | `Tp9R3gxuQBm+PlRFdyeEL+kXSOa6DQoT6UvErj/yoDI=` |
| 3.4.02 | 36 | `xR9oaih+YdnLi6FYFNqa2/+uMlv5cwJ2nKNlqV/ScrY=` |
| 3.5.51 | 37 | `3x5TQe+BCnAKOhYfkdLY6tCsW4mjqCP0vstBAH74w+U=` |
| 3.6.32 | 41 | `wmLb0ZrxD6BwUmJpjwXhe5ETE7vex8s/olQvyGS215M=` |
| 3.6.39 | 41 | `j+dvuKwbzds39u9Ljs2q0ZcJQDD1dYLxGFwpcxcgDR4=` |
| 3.6.53 | 41 | `3ejwjOfUVXoZt17jVEshfSRanCwU+lAexz9pXexPnlA=` |
| 3.7.94 | 41 | `r1geiiQhRU5+VkmZLAxG1SLLMIvNtpY213fEqVMsDnk=` |
| 3.8.95 | 42 | `ZrRyXqJm+f6/K93HUhvNcnXUSiJ3ktmolWzTREuTAfU=` |
| 4.0.31 | 42 | `R6eJTgHlWVL3oDz2bKx15fxYkFmcVVrX+JuBOmQhUjc=` |
| 4.0.64 | 42 | `vI0GUPgT1QE6kILRwU1PoBtsVbFlTzwTYVnSIoO/P9k=` |
| **Total** | **484** | **All 13 pre/post manifests matched** |

Validation after relocation:

- all 13 historical Runtime projects built in Release with zero warnings and
  zero errors;
- the v3 solution built all 20 projects in Release with zero warnings and zero
  errors;
- active projects, scripts, solution, and configuration files contain no
  reference to `SpineViewerWPF/SpineLibrary`.
