# Signed updates — candidate procedure

Requires PowerShell 7. Use a restricted, preferably encrypted local directory outside all repositories for private keys. Never send private keys to chat, commit them, or place them in CI artifacts. Generate once with scripts/New-UpdateKey.ps1 -PrivateKeyPath <external path>. Review and commit only src/SpriteLab.App/Updates/PublicKey.pem. Rebuild baseline with embedded trust key. Back up the private key securely; losing it prevents signing new updates for existing clients.

For a newer version build installer and portable ZIP using CI; validate installer on Windows. Create signed metadata locally using scripts/New-UpdateManifest.ps1 with Version, Installer, PrivateKeyPath, NotesPath and OutputDirectory. Upload exact tested assets to release vX.Y.Z; verify public download hash and availability before committing update.json and update.json.sig together in channels/stable. Never replace released assets or publish an unsigned index. No automatic publication.

SpriteLab is now public, so stable release assets and raw signed metadata can be accessed anonymously once published. The separate NuGet build dependency is still private; do not confuse build-time access with runtime updates. Do not embed access tokens. Missing key disables updates. Authenticode is separate and deferred.

Acceptance: baseline installation, new signed version, save/cancel, installer waits for old process, relaunch, preserved settings and projects, tampered manifest and installer rejection, offline behavior, portable manual update. Automatic rollback remains unimplemented; retain verified previous installer and back up project data. Never claim rollback or Windows acceptance until tested.
