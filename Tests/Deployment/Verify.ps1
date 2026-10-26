$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixture = Join-Path $tempRoot ('BetterMiliraDeployment-' + [guid]::NewGuid().ToString('N'))
$source = Join-Path $fixture 'Source'
$game = Join-Path $fixture 'Game'
$installed = Join-Path $game 'Mods\BetterMiliraFallenAngel'

# Exercise the real deployment script with fixture files; compilation is covered separately.
function dotnet { $global:LASTEXITCODE = 0 }
function Assert-Id([string]$expected, [string]$message) {
    $path = Join-Path $installed 'About\PublishedFileId.txt'
    if (!(Test-Path -LiteralPath $path) -or [IO.File]::ReadAllText($path) -cne $expected) {
        throw $message
    }
}

try {
    foreach ($folder in @('About', 'Common', 'Textures', '1.6\Assemblies', '1.7\Assemblies')) {
        New-Item -ItemType Directory -Path (Join-Path $source $folder) -Force | Out-Null
    }
    New-Item -ItemType Directory -Path (Join-Path $installed 'About') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $repository 'build.ps1') -Destination $source
    [IO.File]::WriteAllText((Join-Path $source 'About\About.xml'), '<ModMetaData />')
    [IO.File]::WriteAllText((Join-Path $source 'LoadFolders.xml'), '<loadFolders />')
    foreach ($version in @('1.6', '1.7')) {
        [IO.File]::WriteAllText((Join-Path $source "$version\Assemblies\BetterMiliraFallenAngel.dll"), "fixture-$version")
    }

    $oldId = "1234567890`r`n"
    [IO.File]::WriteAllText((Join-Path $installed 'About\PublishedFileId.txt'), $oldId)
    & (Join-Path $source 'build.ps1') -Deploy -RimWorldPath $game
    Assert-Id $oldId 'Deployment lost the installed Workshop ID when the source had no ID.'
    foreach ($version in @('1.6', '1.7')) {
        if (!(Test-Path -LiteralPath (Join-Path $installed "$version\Assemblies\BetterMiliraFallenAngel.dll"))) {
            throw "Deployment omitted the $version assembly."
        }
    }
    $savedId = Get-ChildItem -LiteralPath (Join-Path $source 'Build\backups') -Recurse -Filter PublishedFileId.txt
    if ($savedId.Count -ne 1 -or [IO.File]::ReadAllText($savedId.FullName) -cne $oldId) {
        throw 'The original Workshop ID was not preserved in the installation backup.'
    }

    [IO.File]::WriteAllText((Join-Path $source 'About\PublishedFileId.txt'), '9876543210')
    & (Join-Path $source 'build.ps1') -Deploy -RimWorldPath $game
    Assert-Id $oldId 'A source Workshop ID replaced the existing installed association.'

    $newGame = Join-Path $fixture 'NewGame'
    New-Item -ItemType Directory -Path $newGame -Force | Out-Null
    & (Join-Path $source 'build.ps1') -Deploy -RimWorldPath $newGame
    $installed = Join-Path $newGame 'Mods\BetterMiliraFallenAngel'
    Assert-Id '9876543210' 'A first installation did not copy the source Workshop ID.'
    Write-Host 'Deployment checks passed: existing ID, backup, both assemblies, conflicting source ID, first installation.'
}
finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    if (!$resolvedFixture.StartsWith($tempRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing to remove a fixture outside the temporary directory.'
    }
    if (Test-Path -LiteralPath $resolvedFixture) {
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
