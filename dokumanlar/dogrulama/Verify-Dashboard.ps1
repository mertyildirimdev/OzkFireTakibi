$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = Join-Path $repo 'OzkFireTakibi.Dashboard/OzkFireTakibi.Dashboard.csproj'
$output = Join-Path ([IO.Path]::GetTempPath()) ('ozk-dashboard-checks-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
$bin = Join-Path $output 'app'
dotnet build $project --no-restore --nologo --output $bin -p:UseAppHost=false
if ($LASTEXITCODE -ne 0) { throw 'Derleme başarısız.' }
$dotnetRoot = Split-Path (Get-Command dotnet).Source
$sdk = (dotnet --version).Trim()
$runtime = Get-ChildItem (Join-Path $dotnetRoot 'shared/Microsoft.AspNetCore.App') -Directory |
    Where-Object { $_.Name -match '^10\.0\.\d+$' } | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$core = Join-Path $dotnetRoot "shared/Microsoft.NETCore.App/$($runtime.Name)"
$inMemory = Join-Path $env:USERPROFILE '.nuget/packages/microsoft.entityframeworkcore.inmemory/10.0.11/lib/net10.0/Microsoft.EntityFrameworkCore.InMemory.dll'
if (!(Test-Path -LiteralPath $inMemory)) { throw 'Doğrulama için NuGet önbelleğinde EF Core InMemory 10.0.11 gerekli.' }
Get-ChildItem -LiteralPath $bin -Filter '*.dll' | Copy-Item -Destination $output
Copy-Item -LiteralPath $inMemory -Destination $output
$refs = @(Get-ChildItem -LiteralPath $core -Filter '*.dll') + @(Get-ChildItem -LiteralPath $runtime.FullName -Filter '*.dll') + @(Get-ChildItem -LiteralPath $output -Filter '*.dll')
$refs = $refs | Group-Object Name | ForEach-Object { $_.Group[0] }
$refs = $refs | Where-Object { try { [Reflection.AssemblyName]::GetAssemblyName($_.FullName) | Out-Null; $true } catch { $false } }
$arguments = @('/nologo', '/target:exe', '/langversion:preview', '/nullable:enable', '/main:DashboardChecks', "/out:`"$output/Checks.dll`"")
$arguments += $refs | ForEach-Object { "/reference:`"$($_.FullName)`"" }
$arguments += "`"$PSScriptRoot/DashboardChecks.cs`""
$responseFile = Join-Path $output 'compile.rsp'
[IO.File]::WriteAllLines($responseFile, $arguments)
dotnet (Join-Path $dotnetRoot "sdk/$sdk/Roslyn/bincore/csc.dll") "@$responseFile"
if ($LASTEXITCODE -ne 0) { throw 'Doğrulama kodu derlenemedi.' }
@{ runtimeOptions = @{ tfm = 'net10.0'; framework = @{ name = 'Microsoft.AspNetCore.App'; version = $runtime.Name } } } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'Checks.runtimeconfig.json') -Encoding UTF8
dotnet (Join-Path $output 'Checks.dll') $output
if ($LASTEXITCODE -ne 0) { throw 'Doğrulama başarısız.' }
Write-Output "Örnek arayüz çıktısı: $output/home.html"
