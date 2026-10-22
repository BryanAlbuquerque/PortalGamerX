# Revisão de segurança (estática)

Revisão feita lendo o código; **não substitui** teste de invasão nem execução real.

## Autenticação e autorização
- Identity com senha forte (8+, maiúscula/minúscula/dígito), e-mail único, bloqueio após 5 falhas (15 min), e-mail confirmado obrigatório em produção.
- Papéis `Admin` e `Customer`. Cadastro público **sempre** cria `Customer`; nunca há escolha de papel pelo cliente.
- Todo controller da área Admin herda de `AdminBaseController` com `[Authorize(Policy="AdminOnly")]`, reforçado pela `AdminAreaAuthorizationConvention` (cobre controllers esquecidos). Teste por reflexão garante ambos.
- O botão "PAINEL ADMIN" é apenas conveniência visual; a proteção é no servidor.
- Primeiro admin: sem senha padrão; via CLI/script interativo ou bootstrap por variável (somente se não existir Admin).
- Redirecionamento pós-login restrito a URLs locais (anti open-redirect).
- Cliente só acessa os próprios pedidos/endereços (consultas filtradas por usuário).

## Entrada e saída
- Anti-forgery automático em todos os POST (`AutoValidateAntiforgeryToken` global).
- Razor codifica HTML por padrão; textos livres (descrições, políticas) são exibidos como texto, sem `Html.Raw`. Modelos de e-mail codificam os valores inseridos.
- Buscas com `ILike` escapam `%`, `_` e `\`. Acesso a dados via EF Core (parametrizado); sem SQL concatenado.
- Upload: limite de 5 MB, tipo definido pela **assinatura binária** (JPG/PNG/GIF/WEBP; ICO só para favicon), nome gerado (GUID), **SVG não aceito**, pastas de destino em lista fixa.
- Cabeçalhos: CSP (sem scripts inline), `X-Content-Type-Options`, `X-Frame-Options`/`frame-ancestors`, `Referrer-Policy`, `Permissions-Policy`, HSTS. Cookies `HttpOnly`/`Secure`/`SameSite`.
- Rate limiting em login/cadastro/recuperação, cálculo de frete e webhook.

## Dinheiro, estoque e pedidos
- Preços e totais sempre recalculados no servidor a partir do banco; o navegador nunca define preço, desconto ou frete.
- Estoque: baixa atômica (`UPDATE … WHERE estoque >= qtd`) dentro de transação + `CHECK (estoque >= 0)`. Pedido expirado/cancelado devolve estoque.
- Idempotência do checkout (chave única) contra clique duplo/reenvio.
- Admin não pode marcar "Pago" manualmente; só o pagamento confirmado pelo provedor.

## Pagamento
- Webhook: assinatura HMAC-SHA256 validada em tempo constante; sem segredo configurado o webhook é rejeitado.
- O status nunca é confiado ao corpo da notificação: o sistema consulta o pagamento na API, confere valor e referência do pedido, e deduplica por `EventKey` única.
- Sem dados de cartão no sistema (Checkout Pro redireciona ao provedor).

## Segredos
- Credenciais de integração cifradas com Data Protection (chaves no banco), nunca exibidas, nunca em log/auditoria. `.env`, `appsettings.Local.json`, `secrets.json` e `storage/` no `.gitignore`.
- A tela de Configurações **não** edita a string de conexão do banco.

## Pontos de atenção (ação sua)
- Bootstrap CSS/JS vem de CDN **sem SRI**: para produção rígida, hospede localmente ou adicione `integrity`.
- Revise os textos legais (termos, privacidade, trocas): o sistema não fornece modelos jurídicos nem garante conformidade com a LGPD.
- Faça backup do banco **e** do disco de imagens; perder as chaves de Data Protection (tabela no banco) invalida cookies e segredos cifrados.
- Reinicie/rotacione credenciais expostas e use HTTPS sempre (o Render fornece certificado).
