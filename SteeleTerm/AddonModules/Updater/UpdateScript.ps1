param
(
    [Parameter(Mandatory=$true)][string]$toolId,
    [switch]$skipVersion,
    [int]$pidToWait,
    [string]$pkgDir,
    [string]$csprojPath,
    [string]$oldVersion,
    [string]$newVersion
)
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
{
    Start-Process powershell -ArgumentList "-File `"$PSCommandPath`" -toolId `"$toolId`" -pidToWait $pidToWait -pkgDir `"$pkgDir`" -csprojPath `"$csprojPath`" -oldVersion `"$oldVersion`" -newVersion `"$newVersion`"$(if ($skipVersion) { ' -skipVersion' })" -Verb RunAs
    exit
}
Write-Host "⌛ Waiting for $toolId process PID=$pidToWait to exit..."
while (Get-Process -Id $pidToWait -ErrorAction SilentlyContinue) { Start-Sleep -Milliseconds 200 }
Write-Host "✅ $toolId process exited. Proceeding with update..."
Write-Host "🏗️ Moving new package to local nupkg install directory..."
$latest = Get-ChildItem -Path $pkgDir -Filter "$toolId.*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($latest)
{
    $localSource = ([xml](Get-Content "$env:APPDATA\NuGet\NuGet.Config")).configuration.packageSources.add | Where-Object { $_.key -eq "local" } | Select-Object -ExpandProperty value
    Get-ChildItem -Path $localSource -Filter "$toolId.*.nupkg" | Remove-Item -Force
    Copy-Item $latest.FullName -Destination $localSource
    Write-Host "📦 Copied $($latest.Name) to local nupkg install directory"
}
else
{
    Write-Host "❌ No nupkg found in $pkgDir"
    exit 1
}
Write-Host "⚙️ Updating $toolId..."
Write-Host "🧠 Executing: dotnet tool update --global $toolId"
& dotnet tool update --global $toolId
if ($LASTEXITCODE -eq 0)
{
    $timestamp = Get-Date -Format "dd-MM-yyyy HH:mm:ss"
    Write-Host "✅ $toolId successfully updated to latest build at $timestamp"
}
else
{
    Write-Host "❌ $toolId update failed with exit code $LASTEXITCODE"
    if (-not $skipVersion)
    {
        $proj = $csprojPath
        $text = Get-Content $proj -Raw
        $text = $text -replace "<Version>$newVersion</Version>", "<Version>$oldVersion</Version>"
        Set-Content $proj $text -Encoding UTF8
        Write-Host "↩️ Restored version number: $newVersion → $oldVersion"
    }
}