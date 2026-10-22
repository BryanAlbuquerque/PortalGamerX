<#
.SYNOPSIS
  Gera uma migration do EF Core (Npgsql). Não precisa de banco acessível.
.EXAMPLE
  .\scripts\new-migration.ps1 -Name InitialCreate
#>
param(
    [Parameter(Mandatory = $true)][string]$Name
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
    Write-Host 'Instalando a ferramenta dotnet-ef (global)...'
    dotnet tool install --global dotnet-ef --version "10.*"
    $env:PATH += ";$env:USERPROFILE\.dotnet\tools"
}

dotnet ef migrations add $Name `
    --project src/EcommerceBase.Web/EcommerceBase.Web.csproj `
    --startup-project src/EcommerceBase.Web/EcommerceBase.Web.csproj `
    --output-dir Data/Migrations
if ($LASTEXITCODE -ne 0) { throw 'Falha ao gerar a migration.' }
Write-Host "Migration '$Name' criada em src/EcommerceBase.Web/Data/Migrations. Revise o código gerado antes de aplicar."
