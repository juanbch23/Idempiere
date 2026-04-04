---
globs: scripts/*.ps1
---

# PowerShell Script Rules

- Read ONLY from Windows system env vars: `[Environment]::GetEnvironmentVariable($v, 'Machine')`. NEVER read from `.env` files
- Scripts requiring admin: include `#requires -RunAsAdministrator`
- MUST be idempotent (safe to run multiple times)
- Naming: `do{Action}.ps1` for actions, `check{What}.ps1` for validations
- Logging with prefixes and colors:
  - `[*]` Cyan = in progress
  - `[+]` Green = success
  - `[-]` Red = error
  - `[!]` Yellow = warning
  - `[=]` White = no change
  - `[~]` Yellow = updated
- New env vars MUST also be added to `checkVars.ps1`
- No absolute paths — relative to project root
- DNS scripts use project-scoped markers in hosts file
- Cert/DNS scripts auto-detect hostnames from `*_HOST` system vars
