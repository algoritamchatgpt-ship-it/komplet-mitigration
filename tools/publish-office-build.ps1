param(
    [string]$OutputFolder = "",
    [string]$Runtime = "win-x64",
    [switch]$SingleFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputFolder)) {
    $OutputFolder = Join-Path $repoRoot "instalacije\AlgoritamOffice"
}

$projectPath = Join-Path $repoRoot "src\Algoritam.WPF\Algoritam.WPF.csproj"
if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "Projekat nije pronadjen: $projectPath"
}

if (Test-Path -LiteralPath $OutputFolder -PathType Container) {
    Remove-Item -LiteralPath $OutputFolder -Recurse -Force
}
New-Item -Path $OutputFolder -ItemType Directory -Force | Out-Null

$publishArgs = @(
    "publish",
    $projectPath,
    "-c", "Release",
    "-r", $Runtime,
    "--self-contained", "true",
    "-o", $OutputFolder
)

if ($SingleFile) {
    $publishArgs += "-p:PublishSingleFile=true"
    $publishArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
}

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish neuspesan."
}

$shortcutScriptSource = Join-Path $PSScriptRoot "create-network-shortcut.ps1"
if (Test-Path -LiteralPath $shortcutScriptSource -PathType Leaf) {
    Copy-Item -LiteralPath $shortcutScriptSource -Destination (Join-Path $OutputFolder "create-network-shortcut.ps1") -Force
}

$iconSource = Join-Path $repoRoot "src\Algoritam.WPF\Assets\app-icon-zarade.ico"
if (Test-Path -LiteralPath $iconSource -PathType Leaf) {
    Copy-Item -LiteralPath $iconSource -Destination (Join-Path $OutputFolder "app-icon-zarade.ico") -Force
}

# Kopiraj template foldere (data00, data01, F1) koji se koriste pri kreiranju
# nove lokalne instalacije (dugme "Kliknite ovde ako nemate FIN instalaciju")
$templatesSource = Join-Path $repoRoot "templates"
if (Test-Path -LiteralPath $templatesSource -PathType Container) {
    $templatesTarget = Join-Path $OutputFolder "templates"
    Write-Host "Kopiram templates ($templatesSource -> $templatesTarget)..."
    Copy-Item -LiteralPath $templatesSource -Destination $templatesTarget -Recurse -Force
    Write-Host "Templates kopirani."
} else {
    Write-Warning "Templates folder nije pronadjen: $templatesSource"
}

Write-Host "Publish zavrsen."
Write-Host "Output: $OutputFolder"
