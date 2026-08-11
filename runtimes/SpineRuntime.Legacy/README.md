# Historical Runtime adapter source

`Adapter.cs` is linked by the isolated `SpineRuntime.V*` projects. Each
Runtime project owns its matching vendored read-only source under `src/**`
and supplies only its compile-time namespace and version metadata.
