param(
    [Parameter(Mandatory = $true)]
    [string]$AppFolder,

    [string]$ShareName = "AlgoritamApp",
    [string]$ExeRelativePath = "Algoritam.WPF.exe",
    [string]$ShortcutName = "Algoritam.lnk",
    [switch]$CreatePublicDesktopShortcut
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $AppFolder -PathType Container)) {
    throw "AppFolder ne postoji: $AppFolder"
}

$exePath = Join-Path -Path $AppFolder -ChildPath $ExeRelativePath
if (-not (Test-Path -LiteralPath $exePath -PathType Leaf)) {
    throw "Exe nije pronadjen: $exePath"
}

$share = Get-SmbShare -Name $ShareName -ErrorAction SilentlyContinue
if ($null -eq $share) {
    New-SmbShare -Name $ShareName -Path $AppFolder -ChangeAccess "Everyone" | Out-Null
}
else {
    if (-not [string]::Equals($share.Path, $AppFolder, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Share '$ShareName' vec postoji na putanji '$($share.Path)'."
    }
}

$acl = Get-Acl -LiteralPath $AppFolder
$everyone = New-Object System.Security.Principal.NTAccount("Everyone")
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $everyone,
    "Modify",
    "ContainerInherit, ObjectInherit",
    "None",
    "Allow")
$acl.SetAccessRule($rule)
Set-Acl -LiteralPath $AppFolder -AclObject $acl

$uncExePath = "\\$env:COMPUTERNAME\\$ShareName\\$ExeRelativePath"

if ($CreatePublicDesktopShortcut) {
    $publicDesktop = [Environment]::GetFolderPath("CommonDesktopDirectory")
    $shortcutPath = Join-Path -Path $publicDesktop -ChildPath $ShortcutName
    $wsh = New-Object -ComObject WScript.Shell
    $shortcut = $wsh.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $uncExePath
    $shortcut.WorkingDirectory = Split-Path -Path $uncExePath -Parent
    $shortcut.IconLocation = $uncExePath
    $shortcut.Save()
}

Write-Host "Mrezni share je spreman."
Write-Host "UNC putanja aplikacije: $uncExePath"
if ($CreatePublicDesktopShortcut) {
    Write-Host "Kreiran je Public Desktop shortcut: $ShortcutName"
}
