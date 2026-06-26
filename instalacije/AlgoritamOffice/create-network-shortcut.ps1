param(
    [Parameter(Mandatory = $true)]
    [string]$TargetUncExePath,

    [string]$ShortcutName = "Algoritam.lnk",

    [string]$IconPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $TargetUncExePath.StartsWith("\\")) {
    throw "TargetUncExePath mora biti UNC putanja (primer: \\HOST\\AlgoritamApp\\Algoritam.WPF.exe)."
}

$desktop = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path -Path $desktop -ChildPath $ShortcutName

$resolvedIcon = $TargetUncExePath
if (-not [string]::IsNullOrWhiteSpace($IconPath)) {
    $resolvedIcon = $IconPath
} else {
    $candidate = Join-Path -Path (Split-Path -Path $TargetUncExePath -Parent) -ChildPath "app-icon-zarade.ico"
    if (Test-Path -LiteralPath $candidate -PathType Leaf) {
        $resolvedIcon = $candidate
    }
}

$wsh = New-Object -ComObject WScript.Shell
$shortcut = $wsh.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $TargetUncExePath
$shortcut.WorkingDirectory = Split-Path -Path $TargetUncExePath -Parent
$shortcut.IconLocation = "$resolvedIcon,0"
$shortcut.Save()

Write-Host "Kreirana ikonica: $shortcutPath"
