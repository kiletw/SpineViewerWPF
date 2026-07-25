# ADR-003: Establish CLI and Application Contracts Before MCP

## Status

Proposed

## Context

AI integration is desirable, but a full MCP server built before stable application boundaries would duplicate or expose unstable WPF and Runtime internals.

## Decision

Define Application use cases and machine-readable CLI contracts early. Implement a small CLI vertical slice before MCP. Later MCP tools call the same Application handlers directly.

## Consequences

- Core capabilities become testable without WPF.
- CI and AI can consume JSON output early.
- MCP is delayed but avoids a second integration rewrite.
