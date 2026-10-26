# PortalGamerX

Loja virtual de **keys de ativação de jogos** (produto 100% digital, sem frete), com **uma instalação independente por cliente** (sem multi-tenancy).
Stack: C# · ASP.NET Core MVC (.NET 10) · Razor · ASP.NET Core Identity · EF Core + Npgsql (PostgreSQL) · Bootstrap 5 · xUnit · Docker/Render.

## Funcionalidades

**Loja:** home com banners/destaques, catálogo de jogos (busca, filtro por plataforma, ordenação, paginação), página do jogo com galeria, loja de ativação (Steam, Epic, PlayStation Store...), região da key e passo a passo de ativação, carrinho (cookie para anônimos, persistido para logados, mescla ao entrar), cupom, checkout autenticado com endereço de cobrança, pagamento externo (Mercado Pago Checkout Pro) e área do cliente (pedidos **com as keys**, endereços, dados, senha). **Não há frete nem envio.**

**Entrega automática das keys:** o pedido reserva as keys no checkout; quando o pagamento é **confirmado** (consulta à API do provedor), as keys passam a "Vendidas", o pedido vai para **Entregue** e o cliente recebe e-mail (sensível: o corpo é apagado do banco após o envio) e vê as keys em *Minha conta › Pedidos*. Pedido não pago expira e devolve as keys ao estoque; reembolso/estorno de pedido já entregue **invalida** as keys.

**Painel (`/admin`, somente papel Admin):** dashboard, jogos (plataforma, loja de ativação, região, classificação indicativa, lançamento, instruções, imagens, SKU, promoção), categorias (gêneros), publicadoras, clientes, pedidos (cancelar, entregar keys, reembolso, NFS-e), **estoque de keys** (importação em lote, lista mascarada, revelar com auditoria, invalidar, histórico), cupons, banners, auditoria e **Configurações** (empresa, identidade visual, gerais, pagamentos, e-mail, fiscal).

**Segurança das keys:** cifradas com ASP.NET Core Data Protection (as chaves do anel ficam no banco: **faça backup do banco inteiro**, senão as keys ficam ilegíveis), nunca aparecem em listas, hash SHA-256 impede cadastro duplicado, saldo do jogo = keys disponíveis, reserva atômica no Postgres (`FOR UPDATE SKIP LOCKED`) sem vender a mesma key duas vezes.

**Integrações (interfaces + adaptadores):** `IPaymentProvider` → Mercado Pago · `IEmailProvider` → SMTP (MailKit, com fila/outbox) · `IFiscalProvider` → contrato de emissão de **NFS-e** com dados prontos; **nenhum provedor fiscal implementado ainda**.

## Estrutura

```
PortalGamerX.sln · global.json · Directory.Build.props · Dockerfile · render.yaml · .env.example
src/PortalGamerX.Web/
  Domain/            entidades, enums, regras puras (preço, cupom, estados, CPF/CNPJ)
  Data/              AppDbContext, DbInitializer, CLI (--migrate/--create-admin), resolução de DATABASE_URL
  Services/          catálogo, carrinho, checkout, pedidos, pagamento, e-mail, fiscal, estoque de keys, auditoria
  Integrations/      Payments/ Email/ Fiscal/
  Infrastructure/    DI, armazenamento de imagens, binder decimal, convenção de autorização do admin
  Controllers/       loja e conta        Areas/Admin/   painel (controllers + views)
  Views/ wwwroot/ ViewModels/
tests/PortalGamerX.Tests/   xUnit (lógica pura, InMemory e integração PostgreSQL opcional)
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
cd .\PortalGamerX

# 2) Restaurar e compilar
dotnet restore
dotnet build

# 3) Criar o banco (ajuste usuário/senha). Com psql:
psql -U postgres -c "CREATE DATABASE ecommercebase_dev;"
#    ...ou crie pelo pgAdmin. Com Docker:
# docker run --name eb-pg -e POSTGRES_PASSWORD=dev123 -p 5432:5432 -d postgres:16
# docker exec eb-pg psql -U postgres -c "CREATE DATABASE ecommercebase_dev;"

# 4) Guardar a conexão FORA do código (User Secrets)
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=ecommercebase_dev;Username=postgres;Password=SUA_SENHA" --project src/PortalGamerX.Web

# 5) Gerar a migration inicial (uma única vez; depois COMMITE a pasta Data/Migrations)
.\scripts\new-migration.ps1 -Name InitialCreate

# 6) Executar (em Development as migrations são aplicadas na inicialização)
dotnet run --project src/PortalGamerX.Web
#    Abra a URL exibida (ex.: http://localhost:5xxx)
```

> **Visual Studio (Console do Gerenciador de Pacotes):** selecione **Projeto padrão = PortalGamerX** (lista suspensa no topo do console), defina PortalGamerX como projeto de inicialização e rode `Add-Migration InitialCreate -OutputDir Data/Migrations` e depois `Update-Database`. Não é preciso banco acessível para gerar a migration, mas o projeto precisa compilar.

> **Migrations:** não acompanham este pacote porque só podem ser geradas com o SDK instalado. O passo 5 é obrigatório. Se `dotnet ef` pedir, o script instala `dotnet-ef` automaticamente. Para aplicar manualmente: `.\scripts\apply-migrations.ps1`.

### Criar o primeiro administrador (seguro)

Não existe senha padrão. Escolha **uma** forma:

```powershell
# A) Script interativo (a senha é digitada e nunca gravada em arquivo):
.\scripts\create-admin.ps1 -Email "voce@empresa.com" -Name "Seu Nome"

# B) Promover alguém que já se cadastrou na loja:
.\scripts\create-admin.ps1 -Email "voce@empresa.com" -PromoteExisting

# C) Bootstrap na inicialização (só cria se NÃO existir nenhum Admin) — use User Secrets, remova depois:
dotnet user-secrets set "Bootstrap:AdminEmail" "voce@empresa.com" --project src/PortalGamerX.Web
dotnet user-secrets set "Bootstrap:AdminPassword" "SenhaForte123" --project src/PortalGamerX.Web
```

Política de senha: mínimo 8 caracteres, com maiúscula, minúscula e dígito; bloqueio após 5 tentativas. Depois de entrar como Admin aparece o botão **PAINEL ADMIN** no topo; a proteção real é no servidor (`[Authorize(Policy="AdminOnly")]` em toda a área, reforçada por uma convenção global e coberta por teste).

### Visual Studio: primeiro acesso passo a passo
1. Clique com o direito em **PortalGamerX → Gerenciar Segredos do Usuário** e cole (troque os valores):
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
.\scripts\package.ps1   # gera ..\PortalGamerX.zip sem bin/obj/.env/storage
```

## Integrações que precisam de credenciais (Admin → Configurações)

| Integração | O que você precisa | Observação |
|---|---|---|
| Mercado Pago | Access Token, Public Key, **chave secreta do webhook** | Webhook: `https://SEU-DOMINIO/webhooks/mercadopago`. O pedido só vira "Pago" após o sistema **consultar o pagamento na API** do provedor. Teste primeiro em sandbox. |
| E-mail (SMTP) | Servidor, porta, usuário, senha, remetente | Envio assíncrono por fila com tentativas; há "enviar teste" na tela. |
| Fiscal | — | **Emissão não implementada** (falta o adaptador do provedor). Os parâmetros de NFS-e (item LC 116, CNAE, alíquota de ISS, código municipal) e as validações já estão prontos. |

Segredos são gravados **cifrados** (Data Protection, chaves persistidas no banco), nunca reexibidos e nunca registrados em log/auditoria. Campo em branco = manter o valor atual.

## Deploy no Render

Veja `docs/RENDER.md` (blueprint `render.yaml`, disco persistente para imagens, health check `/healthz`, migrations, primeiro admin, webhook).

## Verificação: o que foi e o que não foi verificado

**Feito (estático, sem SDK):** balanceamento de chaves/parênteses em todos os 113 arquivos `.cs`; conferência automatizada de que toda view referenciada por controller existe, de que todo `@model` aponta para um tipo existente, de que todo `asp-controller/asp-action` aponta para uma action existente e de que todo construtor de serviço/controller tem dependência registrada; revisão manual de segurança (`docs/SEGURANCA.md`).

**NÃO feito — precisa rodar na sua máquina:** compilação (`dotnet build`), execução dos testes, geração/aplicação das migrations, navegação real pelas telas, fluxo completo de pagamento (sandbox do Mercado Pago com webhook público), envio real de SMTP, deploy no Render. **Não afirmamos que algo funciona até você ter rodado.**

## Limitações conhecidas

Veja `docs/LIMITACOES.md` (sem variações de jogo, sem painel de keys por fornecedor, fiscal só contrato, Bootstrap via CDN sem SRI, imagens em disco local, etc.).

## Keys digitais

Fluxo do administrador: cadastre o jogo (plataforma, **loja de ativação**, **região**, instruções) → *Admin › Estoque de keys › Importar keys* (uma por linha, até 2.000 por lote; duplicadas são ignoradas) → o jogo fica disponível com o saldo de keys. Sem keys disponíveis o jogo aparece como indisponível.

Estados de uma key: **Disponível → Reservada** (pedido criado) **→ Vendida** (pagamento confirmado). Uma key pode ser **Invalidada** (defeituosa, já usada, reembolsada); invalidadas nunca voltam ao estoque. Se o pedido expirar/cancelar antes do pagamento, a key reservada volta a Disponível.

Se um pedido ficar **Pago sem entrega** (falha transitória), a tarefa de fundo tenta de novo a cada ciclo e o admin tem o botão **Entregar keys** no pedido.

## Fiscal (NFS-e)

Venda de key de ativação é **licenciamento/cessão de direito de uso de programa de computação** (item **01.05** da lista de serviços da LC 116/2003): incide **ISS** e o documento é **NFS-e**, não NF-e de mercadoria (o STF, nas ADIs 1945 e 5659, afastou o ICMS nesse tipo de operação). O ISS desse item é devido, em regra, no domicílio do tomador, por isso o checkout coleta o **endereço de cobrança**.

Em *Configurações › Fiscal* ficam: item da lista (padrão `01.05`), alíquota de ISS (0–5%), CNAE e código de tributação do município. O sistema valida CNPJ, inscrição municipal, cidade, UF e CEP da empresa e cidade/UF/CEP do cliente antes de pedir a emissão (`Domain/Rules/FiscalRules.cs`), e só permite pedir a nota de pedidos pagos e entregues. **Alíquota, códigos, regime e a transição da reforma tributária (IBS/CBS) variam por município e empresa: valide com o seu contador.**

## Atualizando um banco que já existia

Duas migrations em sequência: `20261025130000_ProdutoParaJogo` (renomeia produto → jogo, preservando dados) e `20261026120000_LojaDeKeysDigitais` (cria o inventário `GameKeys`, remove campos de mídia física/frete e renomeia o endereço do pedido para cobrança). **A segunda descarta dados de frete/rastreio dos pedidos antigos e zera o saldo de estoque antigo** (as keys precisam ser importadas). **Faça backup antes.** Aplique com:

```powershell
.\scripts\apply-migrations.ps1
```

Pedidos antigos "Aguardando pagamento" não têm keys reservadas: cancele-os (ou deixe expirarem). Se um deles for pago, ficará "Pago" sem entrega e você verá um alerta no pedido.
