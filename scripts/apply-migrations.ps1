<#
.SYNOPSIS
  Aplica as migrations no banco configurado (DATABASE_URL ou ConnectionStrings__Default).
.EXAMPLE
  $env:DATABASE_URL = 'postgres://postgres:senha@localhost:5432/ecommercebase_dev'
  .\scripts\apply-migrations.ps1
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
dotnet run --project src/EcommerceBase.Web/EcommerceBase.Web.csproj -- --migrate
if ($LASTEXITCODE -ne 0) { throw 'Falha ao aplicar as migrations.' }
