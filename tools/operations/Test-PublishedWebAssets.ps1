[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $PublishDirectory,

    [string] $ApplicationName = "ProjectManagement",

    [string[]] $CssAssets = @("app.css", "custom.css")
)

$ErrorActionPreference = "Stop"
$publishRoot = (Resolve-Path -LiteralPath $PublishDirectory).Path
$cssDirectory = Join-Path $publishRoot "wwwroot\css"
$manifestPath = Join-Path $publishRoot "$ApplicationName.staticwebassets.endpoints.json"
$brotliStreamType = [System.Type]::GetType(
    "System.IO.Compression.BrotliStream, System.IO.Compression.Brotli",
    $false)

function Assert-NonEmptyFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required published asset is missing: $Path"
    }

    if ((Get-Item -LiteralPath $Path).Length -le 0) {
        throw "Required published asset is empty: $Path"
    }
}

function Get-ByteHash {
    param(
        [Parameter(Mandatory = $true)]
        [byte[]] $Bytes
    )

    $sha256 = [System.Security.Cryptography.SHA256]::Create()

    try {
        return [BitConverter]::ToString($sha256.ComputeHash($Bytes)).Replace("-", "")
    }
    finally {
        $sha256.Dispose()
    }
}

function Get-DecompressedBytes {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [ValidateSet("br", "gzip")]
        [string] $Encoding
    )

    if ($Encoding -eq "br" -and $null -eq $brotliStreamType) {
        throw "Brotli decompression is not supported by this PowerShell runtime."
    }

    $inputStream = [System.IO.File]::OpenRead($Path)

    try {
        if ($Encoding -eq "br") {
            $compressionStream = [Activator]::CreateInstance(
                $brotliStreamType,
                [object[]] @($inputStream, [System.IO.Compression.CompressionMode]::Decompress))
        }
        else {
            $compressionStream = [System.IO.Compression.GZipStream]::new(
                $inputStream,
                [System.IO.Compression.CompressionMode]::Decompress)
        }

        try {
            $outputStream = [System.IO.MemoryStream]::new()

            try {
                $compressionStream.CopyTo($outputStream)
                return $outputStream.ToArray()
            }
            finally {
                $outputStream.Dispose()
            }
        }
        finally {
            $compressionStream.Dispose()
        }
    }
    finally {
        $inputStream.Dispose()
    }
}

function Assert-CompressedAssetMatches {
    param(
        [Parameter(Mandatory = $true)]
        [byte[]] $ExpectedBytes,

        [Parameter(Mandatory = $true)]
        [string] $CompressedPath,

        [Parameter(Mandatory = $true)]
        [ValidateSet("br", "gzip")]
        [string] $Encoding
    )

    Assert-NonEmptyFile -Path $CompressedPath
    $actualBytes = Get-DecompressedBytes -Path $CompressedPath -Encoding $Encoding

    if ((Get-ByteHash -Bytes $actualBytes) -ne (Get-ByteHash -Bytes $ExpectedBytes)) {
        throw "Published compressed asset does not match its source CSS: $CompressedPath"
    }
}

Assert-NonEmptyFile -Path $manifestPath
$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json

foreach ($assetName in $CssAssets) {
    $assetPath = "css/$assetName"
    $rawPath = Join-Path $cssDirectory $assetName
    $brotliPath = "$rawPath.br"
    $gzipPath = "$rawPath.gz"

    Assert-NonEmptyFile -Path $rawPath
    $rawBytes = [System.IO.File]::ReadAllBytes($rawPath)
    Assert-NonEmptyFile -Path $brotliPath

    if ($null -ne $brotliStreamType) {
        Assert-CompressedAssetMatches -ExpectedBytes $rawBytes -CompressedPath $brotliPath -Encoding "br"
    }

    Assert-CompressedAssetMatches -ExpectedBytes $rawBytes -CompressedPath $gzipPath -Encoding "gzip"

    foreach ($manifestAssetPath in @($assetPath, "$assetPath.br", "$assetPath.gz")) {
        $fingerprintedEndpoints = @($manifest.Endpoints | Where-Object {
            $_.AssetFile -eq $manifestAssetPath -and
            @($_.EndpointProperties | Where-Object { $_.Name -eq "fingerprint" }).Count -gt 0
        })

        if ($fingerprintedEndpoints.Count -eq 0) {
            throw "Published static asset manifest does not contain a fingerprinted endpoint for $manifestAssetPath"
        }
    }
}

if ($null -eq $brotliStreamType) {
    Write-Warning "Brotli files were checked for presence and size only because this PowerShell runtime cannot decompress Brotli."
}

Write-Host "Published web asset validation completed for $publishRoot"
