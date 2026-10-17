# HPoint IK FixPack v1.0.0 — public preservation

- Management script: Hotfix v1.0.1; the two successful DLL binaries are unchanged from the real-game validation on 2026-10-17.
- Full public package: [`release/windows/HPointIKFixPack_v1.0.0_ManagementHotfix_v1.0.1_Public.zip`](../../release/windows/HPointIKFixPack_v1.0.0_ManagementHotfix_v1.0.1_Public.zip).
- Public archive SHA-256: `6019244EFE9C078FF0FECA6BD2A5A252CFBB5CEB664BE1CC1492C5110BB98C0C`.
- RC1 DLL SHA-256: `F167EF2459C695B4A7B1782141E9640E1C013D00B833D1E48D5B0C54FD4ECC54`.
- Problem B DLL SHA-256: `AC4DCD9D7A809C5A88EE2E9CC3DE60FC5AB4DFCBCA2C5B46B478DC69100F45A6`.
- Public package includes both tested DLLs, exact source files, management script and BAT entrypoints, manifest, SHA256SUMS_PUBLIC, instructions, and validation summary. It excludes raw diagnostic logs, environment ownership records, and third-party/game DLLs. The separate complete frozen provenance package is retained in the project's private Library.
- The two DLLs remain physically separate; future single-DLL consolidation is a separate change/test, not part of this release.
- Note: the newly repackaged public ZIP was content/SHA verified, but this exact repackaging was not independently installed. The included DLLs and the management Hotfix were tested in the target environment.
