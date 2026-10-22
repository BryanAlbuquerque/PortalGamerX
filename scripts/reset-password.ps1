<#
.SYNOPSIS
  Redefine a senha de um usuário existente (ex.: administrador que perdeu o acesso). A senha é digitada, não gravada.
.EXAMPLE
  .\scripts\reset-password.ps1 -Email "ecommerceAdmin@exemplo.com"
#>
param([Parameter(Mandatory = $true)][string]$Email)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$env:ADMIN_EMAIL = $Email
$secure = Read-Host 'Nova senha (mín. 8 caracteres, com maiúscula, minúscula e número)' -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try { $env:ADMIN_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
try {
    dotnet run --project src/EcommerceBase.Web/EcommerceBase.Web.csproj -- --reset-password
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao redefinir a senha.' }
}
finally { Remove-Item Env:ADMIN_PASSWORD -ErrorAction SilentlyContinue }
