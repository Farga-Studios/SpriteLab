# SpriteLab recovery acceptance

Use a disposable Windows account or VM and copies of user data. Never modify stable channel metadata for a private test. Keep all signing private keys outside repositories.

## Required scenarios

| Scenario | Expected result |
| --- | --- |
| Prepared transaction without handoff | Explicit cancellation allowed |
| Successful signed installation | Matching completion receipt, target version, functional checks and explicit confirmation |
| Missing, malformed or stale receipt | Confirmation and rollback blocked |
| Interrupted installation | Preserve journal; no automatic installer execution |
| Retained previous installer with matching signed metadata | Explicit consent and compatible backed-up data required before rollback |
| Missing or altered previous package | No execution; manual recovery |
| Successful rollback | Previous version, fresh completion receipt and functional checks required |
| Portable build | No automatic installation |
| Concurrent update attempt | Exclusive session lock prevents overlapping transaction changes |

## Manual recovery

Close the installer and application, verify no setup processes remain, back up documents/projects and preferences, and preserve the update directory and journal for diagnosis. Obtain a known-good installer from the official release and independently verify its signed metadata and hash. If the previous package cannot be authenticated or data compatibility is uncertain, do not downgrade. Seek maintainer assistance. Do not delete or edit a journal to force the updater to proceed.

The completion receipt is advisory evidence of setup reaching ssDone, not proof that all processes have exited or a substitute for package authentication. Older installers do not emit receipts. A first tracked update may not have a retained previous package. Actual Windows verification is mandatory, including setup cancellation, process exit, receipt timing and data preservation.

## Release gate

Record exact build commit, installer SHA256, baseline and target versions and actual outcomes. CI compilation is not Windows acceptance. No merge or publication until owner approval.
