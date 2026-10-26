<#
.SYNOPSIS
  Remove os arquivos ANTIGOS que sobraram no seu repositório depois de extrair o PortalGamerX.zip por cima da pasta antiga.
  Eles causam erros como: "O nome do tipo ou do namespace 'StockMoveForm' / 'ShippingForm' / 'ShippingSettingsForm' / 'Product' não pode ser encontrado".

.DESCRIPTION
  Rode UMA vez, na raiz do repositório (a pasta que contém PortalGamerX.sln):
      powershell -ExecutionPolicy Bypass -File .\scripts\limpar-arquivos-antigos.ps1
  Depois: feche o Visual Studio, apague as pastas bin/obj (feito aqui) e rode "dotnet build".
#>
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (-not (Test-Path ".\PortalGamerX.sln")) { throw "Rode este script dentro do repositório (PortalGamerX.sln não encontrado em $root)." }

# 1) Pastas inteiras do projeto antigo (EcommerceBase.*)
$pastas = @(
  "src\EcommerceBase.Web", "src\EcommerceBase.Application", "src\EcommerceBase.Domain", "src\EcommerceBase.Infrastructure",
  "tests\EcommerceBase.Tests"
)
foreach ($p in $pastas) {
  if (Test-Path $p) { Remove-Item $p -Recurse -Force; Write-Host "Removida pasta: $p" }
}

# 2) Arquivos obsoletos dentro das pastas novas (produtos, frete e movimentação manual de estoque)
$arquivos = @(
  "src\PortalGamerX.Web\Areas\Admin\Controllers\ProductsController.cs",
  "src\PortalGamerX.Web\Areas\Admin\Views\Products\Form.cshtml",
  "src\PortalGamerX.Web\Areas\Admin\Views\Products\Index.cshtml",
  "src\PortalGamerX.Web\Areas\Admin\Views\Settings\Shipping.cshtml",
  "src\PortalGamerX.Web\Areas\Admin\Views\Stock\Move.cshtml",
  "src\PortalGamerX.Web\Infrastructure\ShippingJson.cs",
  "src\PortalGamerX.Web\Integrations\Shipping\DevelopmentShippingProvider.cs",
  "src\PortalGamerX.Web\Integrations\Shipping\IShippingProvider.cs",
  "src\PortalGamerX.Web\Integrations\Shipping\MelhorEnvioProvider.cs",
  "src\PortalGamerX.Web\Services\ShippingService.cs",
  "src\PortalGamerX.Web\Views\Shared\_ProductCard.cshtml"
)
foreach ($f in $arquivos) {
  if (Test-Path $f) { Remove-Item $f -Force; Write-Host "Removido: $f" }
}
foreach ($d in @("src\PortalGamerX.Web\Areas\Admin\Views\Products", "src\PortalGamerX.Web\Integrations\Shipping")) {
  if ((Test-Path $d) -and -not (Get-ChildItem $d -Recurse -File)) { Remove-Item $d -Recurse -Force; Write-Host "Removida pasta vazia: $d" }
}

# 3) Compilações antigas (evitam erros "fantasmas" de views/classes que não existem mais)
Get-ChildItem -Path . -Recurse -Directory -Include bin,obj -ErrorAction SilentlyContinue | ForEach-Object { Remove-Item $_.FullName -Recurse -Force }
Write-Host "Pastas bin/obj removidas."

Write-Host ""
Write-Host "Pronto. Agora rode: dotnet build"
