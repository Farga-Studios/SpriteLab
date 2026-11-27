param(
 [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
 [Parameter(Mandatory)][string]$Installer,
 [Parameter(Mandatory)][string]$PrivateKeyPath,
 [Parameter(Mandatory)][string]$NotesPath,
 [Parameter(Mandatory)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$private = [IO.Path]::GetFullPath($PrivateKeyPath)
if ($private.StartsWith($root + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Private key must remain outside repository.' }
if ([Version]::Parse($Version).ToString(3) -cne $Version -or [Version]::Parse($Version) -le [Version]'0.0.0') { throw 'Invalid version.' }
$file = Get-Item -LiteralPath $Installer
if ($file.Name -cne "SpriteLab-$Version-win-x64-setup.exe" -or $file.Length -lt 1 -or $file.Length -gt 536870912) { throw 'Invalid installer filename or size.' }
$notes = [IO.File]::ReadAllText([IO.Path]::GetFullPath($NotesPath))
if ($notes.Length -gt 20000) { throw 'Release notes exceed limit.' }
$manifest = [ordered]@{product='SpriteLab';channel='stable';version=$Version;file_name=$file.Name;bytes=$file.Length;sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash;notes=$notes}
$data = [Text.Encoding]::UTF8.GetBytes(($manifest | ConvertTo-Json -Compress))
if ($data.Length -gt 65536) { throw 'Manifest exceeds client limit.' }
$rsa = [Security.Cryptography.RSA]::Create()
$trust = [Security.Cryptography.RSA]::Create()
try {
    $rsa.ImportFromPem([IO.File]::ReadAllText($private))
    $trust.ImportFromPem([IO.File]::ReadAllText((Join-Path $root 'src/SpriteLab.App/Updates/PublicKey.pem')))
    if ($rsa.KeySize -lt 3072 -or $rsa.ExportSubjectPublicKeyInfoPem() -cne $trust.ExportSubjectPublicKeyInfoPem()) { throw 'Signing key does not match embedded public key.' }
    $sig = $rsa.SignData($data,[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pkcs1)
    if (-not $trust.VerifyData($data,$sig,[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pkcs1)) { throw 'Signature verification failed.' }
    New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
    [IO.File]::WriteAllBytes((Join-Path $OutputDirectory 'update.json'),$data)
    [IO.File]::WriteAllBytes((Join-Path $OutputDirectory 'update.json.sig'),$sig)
} finally { $rsa.Dispose(); $trust.Dispose() }
