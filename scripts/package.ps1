<#
.SYNOPSIS
  Gera um ZIP do código-fonte sem bin/obj, .env, uploads ou credenciais.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path (Split-Path -Parent $root) 'EcommerceBase.zip'
if (Test-Path $out) { Remove-Item $out }
$tmp = Join-Path $env:TEMP ("eb-" + [guid]::NewGuid())
New-Item -ItemType Directory $tmp | Out-Null
robocopy $root $tmp /E /XD bin obj .git .vs .idea node_modules storage /XF .env *.user /NFL /NDL /NJH /NJS | Out-Null
Compress-Archive -Path "$tmp\*" -DestinationPath $out
Remove-Item -Recurse -Force $tmp
Write-Host "Gerado: $out"
