param([Parameter(Mandatory)][string]$PackPath)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assets = Join-Path $root 'src/SpriteLab.App/Assets'
$source = [IO.Path]::GetFullPath($PackPath)
$temp = $null
try {
    if (Test-Path -LiteralPath $source -PathType Leaf) {
        $temp = Join-Path ([IO.Path]::GetTempPath()) ('spritelab-icons-' + [Guid]::NewGuid().ToString('N'))
        Expand-Archive -LiteralPath $source -DestinationPath $temp
        $source = $temp
    }
    $icons = @(Get-ChildItem -LiteralPath $source -Filter SpriteLab.ico -File -Recurse)
    if ($icons.Count -ne 1) { throw 'Expected exactly one SpriteLab.ico in approved icon pack.' }
    $folder = $icons[0].Directory.FullName
    foreach ($name in @('SpriteLab.ico','SpriteLab-256.png','SpriteLab.svg')) {
        if (!(Test-Path -LiteralPath (Join-Path $folder $name) -PathType Leaf)) { throw "Missing $name" }
    }
    Copy-Item -LiteralPath (Join-Path $folder 'SpriteLab.ico') -Destination (Join-Path $assets 'SpriteLab.ico') -Force
    Copy-Item -LiteralPath (Join-Path $folder 'SpriteLab-256.png') -Destination (Join-Path $assets 'SpriteLab.png') -Force
    Copy-Item -LiteralPath (Join-Path $folder 'SpriteLab.svg') -Destination (Join-Path $assets 'SpriteLab.svg') -Force
    Write-Host 'Approved icon pack imported. Review and commit the three assets.'
} finally { if ($temp) { Remove-Item -LiteralPath $temp -Recurse -Force } }
