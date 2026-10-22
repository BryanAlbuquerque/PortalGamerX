# Deploy no Render (uma instalação por cliente)

1. Suba o código para um repositório Git privado (o `.gitignore` já exclui `bin/`, `obj/`, `.env` e `storage/`).
2. No Render: **New → Blueprint** e aponte para o repositório (usa o `render.yaml`) — ou crie manualmente um **PostgreSQL** e um **Web Service (Docker)**.
3. Variáveis de ambiente (veja `.env.example`):
   - `DATABASE_URL` — ligada automaticamente ao banco pelo blueprint.
   - `App__PublicBaseUrl` — `https://seu-servico.onrender.com` (ou o domínio próprio). **Obrigatória**: é usada em links de e-mail, retorno do pagamento e webhook.
   - `Storage__RootPath=/var/data/storage` — pasta no **disco persistente** (sem disco, as imagens enviadas se perdem a cada deploy).
   - `Bootstrap__AdminEmail` / `Bootstrap__AdminPassword` — somente para o primeiro acesso (veja abaixo).
4. **Migrations**: o banco precisa do schema antes do primeiro uso. Opções:
   - Defina `Database__MigrateOnStartup=true` no primeiro deploy e volte para `false` depois; ou
   - No Shell do serviço: `dotnet EcommerceBase.Web.dll --migrate`.
   (As migrations precisam estar commitadas — veja `scripts/new-migration.ps1` no README.)
5. **Primeiro administrador**: defina `Bootstrap__AdminEmail` e `Bootstrap__AdminPassword`, faça o deploy, entre, e **remova a senha das variáveis** em seguida. O bootstrap só cria o usuário se não existir nenhum Admin. Alternativa sem senha em variável: Shell do serviço com `ADMIN_EMAIL=... ADMIN_PASSWORD=... dotnet EcommerceBase.Web.dll --create-admin`.
6. Health check: `/healthz` (verifica o banco).
7. **Backups**: ative os backups do PostgreSQL do Render (planos pagos) e agende cópia do disco `/var/data/storage`.
8. Planos gratuitos do Render hibernam e não oferecem disco persistente: use plano pago para produção.

## Webhook do Mercado Pago
Em *Suas integrações → Webhooks* cadastre `https://SEU-DOMINIO/webhooks/mercadopago`, evento **Pagamentos**, e copie a **chave secreta** para Admin → Configurações → Pagamentos. Sem a chave, notificações são rejeitadas. O sistema sempre consulta o pagamento na API do Mercado Pago antes de marcar o pedido como pago.
