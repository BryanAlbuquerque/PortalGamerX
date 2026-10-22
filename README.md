# Ecommerce Base

Base de e-commerce **reutilizável**, com **uma instalação independente por cliente** (sem multi-tenancy).
Stack: C# · ASP.NET Core MVC (.NET 10) · Razor · ASP.NET Core Identity · EF Core + Npgsql (PostgreSQL) · Bootstrap 5 · xUnit · Docker/Render.

## Funcionalidades

**Loja:** home com banners/destaques, catálogo (busca, filtros, ordenação, paginação), página de produto com galeria, carrinho (cookie para anônimos, persistido para logados, mescla ao entrar), cupom, cálculo de frete por CEP, checkout autenticado com endereço, pagamento externo (Mercado Pago Checkout Pro) e área do cliente (pedidos, cancelamento, endereços, dados, senha).

**Painel (`/admin`, somente papel Admin):** dashboard, produtos (imagens, SKU, NCM, promoção), categorias, marcas, clientes, pedidos (máquina de estados, rastreio, reembolso, fiscal), estoque (movimentações com histórico), cupons, banners, auditoria e **Configurações** (empresa, identidade visual, gerais, pagamentos, frete, e-mail, fiscal).

**Integrações (interfaces + adaptadores):** `IPaymentProvider` → Mercado Pago · `IShippingProvider` → Melhor Envio (+ simulado só em desenvolvimento) · `IEmailProvider` → SMTP (MailKit, com fila/outbox) · `IFiscalProvider` → **somente contrato** (nenhum provedor fiscal implementado).

## Estrutura

```
EcommerceBase.sln · global.json · Directory.Build.props · Dockerfile · render.yaml · .env.example
src/EcommerceBase.Web/
  Domain/            entidades, enums, regras puras (preço, cupom, estados, CPF/CNPJ)
  Data/              AppDbContext, DbInitializer, CLI (--migrate/--create-admin), resolução de DATABASE_URL
  Services/          catálogo, carrinho, checkout, pedidos, pagamento, frete, e-mail, fiscal, estoque, auditoria
  Integrations/      Payments/ Shipping/ Email/ Fiscal/
  Infrastructure/    DI, armazenamento de imagens, binder decimal, convenção de autorização do admin
  Controllers/       loja e conta        Areas/Admin/   painel (controllers + views)
  Views/ wwwroot/ ViewModels/
tests/EcommerceBase.Tests/   xUnit (lógica pura, InMemory e integração PostgreSQL opcional)
scripts/   new-migration.ps1 · apply-migrations.ps1 · create-admin.ps1 · test.ps1 · package.ps1
docs/      RENDER.md · SEGURANCA.md · LIMITACOES.md
```

## Pré-requisitos (Windows)

- [.NET SDK 10](https://dotnet.microsoft.com/download) (`dotnet --version` deve mostrar 10.x; o `global.json` exige 10.0.100 ou superior).
- PostgreSQL 15+ (instalador oficial ou Docker). `psql` no PATH é opcional.
- PowerShell 5.1+ ou 7+.

## Passo a passo (PowerShell)

```powershell
# 1) Entre na pasta do projeto
cd .\EcommerceBase

# 2) Restaurar e compilar
dotnet restore
dotnet build

# 3) Criar o banco (ajuste usuário/senha). Com psql:
psql -U postgres -c "CREATE DATABASE ecommercebase_dev;"
#    ...ou crie pelo pgAdmin. Com Docker:
# docker run --name eb-pg -e POSTGRES_PASSWORD=dev123 -p 5432:5432 -d postgres:16
# docker exec eb-pg psql -U postgres -c "CREATE DATABASE ecommercebase_dev;"

# 4) Guardar a conexão FORA do código (User Secrets)
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=ecommercebase_dev;Username=postgres;Password=SUA_SENHA" --project src/EcommerceBase.Web

# 5) Gerar a migration inicial (uma única vez; depois COMMITE a pasta Data/Migrations)
.\scripts\new-migration.ps1 -Name InitialCreate

# 6) Executar (em Development as migrations são aplicadas na inicialização)
dotnet run --project src/EcommerceBase.Web
#    Abra a URL exibida (ex.: http://localhost:5xxx)
```

> **Visual Studio (Console do Gerenciador de Pacotes):** selecione **Projeto padrão = EcommerceBase.Web** (lista suspensa no topo do console), defina EcommerceBase.Web como projeto de inicialização e rode `Add-Migration InitialCreate -OutputDir Data/Migrations` e depois `Update-Database`. Não é preciso banco acessível para gerar a migration, mas o projeto precisa compilar.

> **Migrations:** não acompanham este pacote porque só podem ser geradas com o SDK instalado. O passo 5 é obrigatório. Se `dotnet ef` pedir, o script instala `dotnet-ef` automaticamente. Para aplicar manualmente: `.\scripts\apply-migrations.ps1`.

### Criar o primeiro administrador (seguro)

Não existe senha padrão. Escolha **uma** forma:

```powershell
# A) Script interativo (a senha é digitada e nunca gravada em arquivo):
.\scripts\create-admin.ps1 -Email "voce@empresa.com" -Name "Seu Nome"

# B) Promover alguém que já se cadastrou na loja:
.\scripts\create-admin.ps1 -Email "voce@empresa.com" -PromoteExisting

# C) Bootstrap na inicialização (só cria se NÃO existir nenhum Admin) — use User Secrets, remova depois:
dotnet user-secrets set "Bootstrap:AdminEmail" "voce@empresa.com" --project src/EcommerceBase.Web
dotnet user-secrets set "Bootstrap:AdminPassword" "SenhaForte123" --project src/EcommerceBase.Web
```

Política de senha: mínimo 8 caracteres, com maiúscula, minúscula e dígito; bloqueio após 5 tentativas. Depois de entrar como Admin aparece o botão **PAINEL ADMIN** no topo; a proteção real é no servidor (`[Authorize(Policy="AdminOnly")]` em toda a área, reforçada por uma convenção global e coberta por teste).

### Visual Studio: primeiro acesso passo a passo
1. Clique com o direito em **EcommerceBase.Web → Gerenciar Segredos do Usuário** e cole (troque os valores):
```json
{
  "ConnectionStrings": { "Default": "Host=localhost;Port=5432;Database=ecommercebase_dev;Username=postgres;Password=SENHA_DO_POSTGRES" },
  "Bootstrap": { "AdminEmail": "voce@empresa.com", "AdminPassword": "SenhaForte123", "AdminName": "Seu Nome" }
}
```
2. Rode as migrations (`Add-Migration` / `Update-Database`) e inicie o projeto com o perfil **http** ou **https** (ambiente Development vem do `launchSettings.json`).
3. Acesse `/conta/entrar`, entre com o e-mail e a senha acima; o botão **PAINEL ADMIN** aparece no topo (ou acesse `/admin`).
4. Depois do primeiro acesso, **apague o bloco `Bootstrap`** dos segredos.

### Variáveis de ambiente

Veja `.env.example`. Principais: `DATABASE_URL` **ou** `ConnectionStrings__Default`, `App__PublicBaseUrl`, `Storage__RootPath`, `Database__MigrateOnStartup`, `Bootstrap__AdminEmail/Password`, `Identity__RequireConfirmedEmail`, `PORT`. Em Development a confirmação de e-mail está desligada (para testar sem SMTP); em produção é obrigatória.

### Testes

```powershell
.\scripts\test.ps1            # restaura, compila (Release) e roda todos os testes
# Opcional: testes de integração com PostgreSQL (use um banco DEDICADO — o schema é recriado!)
$env:TEST_DATABASE_URL = "Host=localhost;Database=ecommercebase_test;Username=postgres;Password=SUA_SENHA"
.\scripts\test.ps1
```

Cobrem: dinheiro/preços, cupons, máquina de estados, CPF/CNPJ/CEP/slug, validação de imagens por assinatura, binder decimal (`1.234,56`), `DATABASE_URL`, assinatura HMAC do webhook e mapeamento de status do Mercado Pago, modelos de e-mail (escape de HTML), **autorização de todos os controllers do admin (reflexão)**, cupons via EF InMemory e — com `TEST_DATABASE_URL` — concorrência de estoque (nunca vende além do saldo) e constraints.

### Gerar o ZIP do código-fonte

```powershell
.\scripts\package.ps1   # gera ..\EcommerceBase.zip sem bin/obj/.env/storage
```

## Integrações que precisam de credenciais (Admin → Configurações)

| Integração | O que você precisa | Observação |
|---|---|---|
| Mercado Pago | Access Token, Public Key, **chave secreta do webhook** | Webhook: `https://SEU-DOMINIO/webhooks/mercadopago`. O pedido só vira "Pago" após o sistema **consultar o pagamento na API** do provedor. Teste primeiro em sandbox. |
| Melhor Envio | Token, CEP de origem, e-mail técnico | Cotação apenas (não gera etiqueta/postagem). Em Development há frete **simulado**, rotulado como tal. |
| E-mail (SMTP) | Servidor, porta, usuário, senha, remetente | Envio assíncrono por fila com tentativas; há "enviar teste" na tela. |
| Fiscal | — | **Não implementado**: só estrutura. Ver `docs/LIMITACOES.md`. |

Segredos são gravados **cifrados** (Data Protection, chaves persistidas no banco), nunca reexibidos e nunca registrados em log/auditoria. Campo em branco = manter o valor atual.

## Deploy no Render

Veja `docs/RENDER.md` (blueprint `render.yaml`, disco persistente para imagens, health check `/healthz`, migrations, primeiro admin, webhook).

## Verificação: o que foi e o que não foi verificado

**Feito (estático, sem SDK):** balanceamento de chaves/parênteses em todos os 113 arquivos `.cs`; conferência automatizada de que toda view referenciada por controller existe, de que todo `@model` aponta para um tipo existente, de que todo `asp-controller/asp-action` aponta para uma action existente e de que todo construtor de serviço/controller tem dependência registrada; revisão manual de segurança (`docs/SEGURANCA.md`).

**NÃO feito — precisa rodar na sua máquina:** compilação (`dotnet build`), execução dos testes, geração/aplicação das migrations, navegação real pelas telas, fluxo completo de pagamento (sandbox do Mercado Pago com webhook público), cotação real do Melhor Envio, envio real de SMTP, deploy no Render. **Não afirmamos que algo funciona até você ter rodado.**

## Limitações conhecidas

Veja `docs/LIMITACOES.md` (sem variações de produto, fiscal só contrato, Bootstrap via CDN sem SRI, imagens em disco local, etc.).
