param([string]$TargetPath,[string]$ProjectDir)

[xml]$csproj = Get-Content "$ProjectDir\ClientPlugin.csproj"

$ProjectDir = (Resolve-Path $ProjectDir).Path

$asmbVersion 	 = ($csproj.Project.PropertyGroup | Where-Object AssemblyVersion).AssemblyVersion
$fileVersion     = ($csproj.Project.PropertyGroup | Where-Object FileVersion).FileVersion
$parts = $fileVersion.Split('.')
$versionMMB = "$($parts[0]).$($parts[1]).$($parts[2])"

Write-Host "Updating Directory.Build.props version numbers to $version"

$repoRoot = Split-Path $ProjectDir -Parent
$propsFile = Join-Path $repoRoot "Directory.Build.props"

$content = Get-Content $propsFile -Raw

$content = $content -replace '<Version>.*?</Version>', "<Version>$versionMmb</Version>"
$content = $content -replace '<AssemblyVersion>.*?</AssemblyVersion>', "<AssemblyVersion>$asmbVersion</AssemblyVersion>"
$content = $content -replace '<FileVersion>.*?</FileVersion>', "<FileVersion>$fileVersion</FileVersion>"

Set-Content $propsFile $content -NoNewline

Write-Host "Updated Directory.Build.props with version number $versionMmb"