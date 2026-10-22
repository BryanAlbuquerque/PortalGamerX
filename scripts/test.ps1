<#
.SYNOPSIS
  Restaura, compila e executa os testes. Para incluir os testes com PostgreSQL, defina antes
  $env:TEST_DATABASE_URL (banco DEDICADO a testes: o schema é apagado e recriado!).
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
dotnet restore
dotnet build --no-restore -c Release
if ($LASTEXITCODE -ne 0) { throw 'Falha na compilação.' }
dotnet test --no-build -c Release
