param(
    [ValidateSet('1.6', '1.7', 'All')]
    [string]$Version = 'All',
    [string]$Configuration = 'Release',
    [switch]$Deploy,
    [string]$RimWorldPath = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld'
)

$ErrorActionPreference = 'Stop'
$versions = if ($Version -eq 'All') { @('1.6', '1.7') } else { @($Version) }
foreach ($modVersion in $versions) {
    & dotnet build "$PSScriptRoot\$modVersion\Source\BetterMiliraFallenAngel.csproj" -c $Configuration "-p:RimWorldPath=$RimWorldPath" --nologo
    if ($LASTEXITCODE -ne 0) { throw "RimWorld $modVersion build failed." }
}

if ($Deploy) {
    if ($Version -ne 'All') { throw 'Deploy requires -Version All so both supported versions are built.' }
    $resolvedGame = (Resolve-Path -LiteralPath $RimWorldPath).Path
    $destination = [IO.Path]::GetFullPath((Join-Path $resolvedGame 'Mods\BetterMiliraFallenAngel'))
    $expectedDestination = [IO.Path]::GetFullPath((Join-Path $resolvedGame 'Mods\BetterMiliraFallenAngel'))
    $backupRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'Build\backups'))
    if ($destination -ne $expectedDestination -or !$backupRoot.StartsWith($PSScriptRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Deployment paths are outside the intended mod directories.'
    }
    $backup = $null
    if (Test-Path -LiteralPath $destination) {
        New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
        $backup = Join-Path $backupRoot ('installed-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
        Move-Item -LiteralPath $destination -Destination $backup
        Write-Host "Previous installed mod backed up to $backup"
    }
    try {
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        foreach ($folder in @('About', 'Common', 'Textures')) {
            Copy-Item -LiteralPath (Join-Path $PSScriptRoot $folder) -Destination $destination -Recurse
        }
        Copy-Item -LiteralPath "$PSScriptRoot\LoadFolders.xml" -Destination $destination
        foreach ($modVersion in $versions) {
            $assemblyDirectory = Join-Path $destination "$modVersion\Assemblies"
            New-Item -ItemType Directory -Path $assemblyDirectory -Force | Out-Null
            Copy-Item -LiteralPath "$PSScriptRoot\$modVersion\Assemblies\BetterMiliraFallenAngel.dll" -Destination $assemblyDirectory
        }
    }
    catch {
        if ($backup -and (Test-Path -LiteralPath $backup)) {
            $failedDeployment = $backup + '-failed-deployment'
            if (Test-Path -LiteralPath $destination) { Move-Item -LiteralPath $destination -Destination $failedDeployment }
            Move-Item -LiteralPath $backup -Destination $destination
        }
        throw
    }
    Write-Host "Deployed to $destination"
}
