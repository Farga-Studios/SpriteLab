param([Parameter(Mandatory)][string]$PrivateKeyPath)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$private = [IO.Path]::GetFullPath($PrivateKeyPath)
$public = Join-Path $root 'src/SpriteLab.App/Updates/PublicKey.pem'
if ($private.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Private key must be outside the repository.' }
if ((Test-Path -LiteralPath $private) -or (Test-Path -LiteralPath $public)) { throw 'Existing key: refusing to overwrite.' }
$rsa = [Security.Cryptography.RSA]::Create(3072)
try {
    $stream = [IO.File]::Open($private, [IO.FileMode]::CreateNew)
    try {
        $bytes = [Text.Encoding]::ASCII.GetBytes($rsa.ExportPkcs8PrivateKeyPem())
        $stream.Write($bytes, 0, $bytes.Length)
    } finally { $stream.Dispose() }
    New-Item -ItemType Directory -Force (Split-Path $public) | Out-Null
    [IO.File]::WriteAllText($public, $rsa.ExportSubjectPublicKeyInfoPem())
    Write-Host 'Public key generated. Commit only PublicKey.pem. Protect the private key with restricted filesystem permissions and an encrypted backup.'
} finally { $rsa.Dispose() }
