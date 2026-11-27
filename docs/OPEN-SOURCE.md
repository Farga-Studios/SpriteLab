# Public repository transition

SpriteLab source is public and original project code uses MIT. A complete dependency, asset and history review is still required. Do not claim that the source history has been audited. Rotate any exposed credential found during the review.

The currently pinned updater package is FargaStudios.Suite.Updater 0.2.0 in GitHub Packages. The existing authorized CI remains unchanged. GitHub's NuGet registry requires authentication even for public packages. For anonymous builds, publish an approved MIT-licensed updater package to nuget.org from DevelopmentSuite and then switch the source mapping and pinned version in a tested PR. Do not switch before the package is available.

Approved brand SVG is stored in src/SpriteLab.App/Assets/SpriteLab.svg. Import the exact PNG/ICO files from the separately prepared SpriteLab-Icon-Pack.zip with scripts/Import-IconPack.ps1 -PackPath <zip path>. The binary assets remain unchanged until imported. Build and inspect the title bar, taskbar, Explorer and installer at small sizes.

No release or signed update has been published as part of this transition.
