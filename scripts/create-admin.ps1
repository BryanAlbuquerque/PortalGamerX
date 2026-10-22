<#
.SYNOPSIS
  Cria o primeiro administrador de forma segura (a senha é digitada, não fica em arquivo nem no histórico).
  Use -PromoteExisting para promover um usuário já cadastrado (só pede o e-mail).
#>
param(
    [Parameter(Mandatory = $true)][string]$Email,
    [string]$Name = 'Administrador',
    [switch]$PromoteExisting
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$env:ADMIN_EMAIL = $Email
$env:ADMIN_NAME = $Name
if (-not $PromoteExisting) {
    $secure = Read-Host 'Senha do administrador (mín. 8 caracteres, com maiúscula, minúscula e número)' -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { $env:ADMIN_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
}
try {
    $args = @('--create-admin')
    if ($PromoteExisting) { $args += '--promote-existing' }
    dotnet run --project src/EcommerceBase.Web/EcommerceBase.Web.csproj -- @args
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao criar/promover o administrador.' }
}
finally {
    Remove-Item Env:ADMIN_PASSWORD -ErrorAction SilentlyContinue
}
