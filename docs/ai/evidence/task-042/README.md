# TASK-042 diagnostic evidence

The external user asset remains outside tracked fixtures. A temporary copy was
used only under gitignored `artifacts/diagnostics-3551/116421`.

## Reproduction

- Source: external `116421.json`, exported by Spine 3.5.51
- Skeleton SHA-256:
  `34EF61634D8256348D0D047139152015510C54511937E8AF9A4279C2F9B445DC`
- Atlas SHA-256:
  `457B8BDBF457598E74CFF15610D0BDE25218E24C4FF75018351EFCC50B4F7F35`
- Texture SHA-256:
  `17255F4BA11FDE54114900E6E2CE8650882D9530407EF4134644A95C50A72DC4`
- Metadata: 120 bones, 114 slots, 17 animations, default skin, one 2048 by
  2048 texture page
- Blend metadata: 113 normal/default slots and one Screen slot named `screen`

The deterministic CPU render retained the expected colors. The original GPU
viewport emitted straight RGB for non-PMA input while Screen still used
`OneMinusSrcColor` as its destination factor. On translucent pixels this
subtracted the full, unscaled source color from the destination and produced
the observed dark brown/black band.

The corrected GPU path always emits premultiplied framebuffer RGB. Non-PMA
texture input is multiplied by texture and tint alpha in the fragment shader;
PMA input is not multiplied again. Screen then uses PMA color factors
`(One, OneMinusSrcColor)` and independent source-over alpha factors.

Competitor source commit `905d265b5c74fd5c2f52ba818d4d2e4028f91a51`
was inspected under the gitignored diagnostic cache. Its `SFMLShader` and
`SFMLBlendMode` use the same always-PMA output convention and separate Screen
color/alpha factors.

## Regression

Application smoke checks that straight-alpha shader input is premultiplied and
that Screen consumes premultiplied source color. The external asset is not
stored or modified by the test.

The Release WPF application was also launched with the external asset. Its
status reported Runtime 3.5.51 and GPU rendering; the captured frame no longer
contained the dark translucent band. The gitignored diagnostic crop SHA-256 is
`CD1302E7E27573C7E595A85A76F8561B99CB526BB70030BD0B7ED1B254A144AE`.

Validation on 2026-08-09:

- WPF Release build: passed, 0 warnings and 0 errors
- Application smoke: passed
- `scripts/test-v3.ps1`: passed, including 12 historical Runtime lines and 4.2
- deterministic 4.1 baseline remained
  `7178BBFA4315C36332AB5C4743A413FE6A7CD165D75C907BBC34D88DB846301E`
