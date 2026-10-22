# Limitações conhecidas e itens pendentes

- **Nada foi compilado/executado pelo autor.** Veja README → Verificação.
- **Migrations não incluídas**: gere com `scripts/new-migration.ps1` e commite.
- **Fiscal: apenas contrato.** Não há emissão de NF-e/NFC-e. Exige provedor/SEFAZ, certificado digital, regime tributário e NCM/CFOP/CST corretos validados por contador. A validação de CNPJ confere só os dígitos, não a situação cadastral.
- **Sem variações de produto** (tamanho/cor): cada SKU é um produto.
- **Frete**: apenas cotação (Melhor Envio). Não compra etiqueta nem acompanha postagem; o rastreio é informado manualmente. O frete simulado só existe em Development.
- **Pagamento**: Checkout Pro (redirecionamento). Sem cartão transparente/Pix próprio. Pagamento aprovado após o pedido ter sido cancelado/expirado é sinalizado para **revisão manual** (reembolso).
- **Cupons**: o limite por cliente de usuários anônimos só é aplicado no checkout (quando há login).
- **Imagens** em disco local (`Storage__RootPath`): no Render exige disco persistente. Não há CDN nem redimensionamento.
- **E-mail**: SMTP apenas; fila em banco processada por serviço em segundo plano na mesma instância.
- **Bootstrap por CDN** sem SRI; sem build de front-end.
- **Testes de integração com PostgreSQL** só rodam com `TEST_DATABASE_URL`; EF InMemory não suporta `ExecuteUpdate`/transações, por isso estoque e checkout concorrentes só são cobertos no teste com PostgreSQL. Não há testes automatizados de interface.
- **Moeda**: apenas BRL. **Idioma**: apenas pt-BR.
- **Escala**: pensado para uma loja por instalação; cache de configurações em memória (instância única).

- **Formulários HTML**: todo `<form method="post">` com `action="..."` escrito à mão precisa de `asp-antiforgery="true"` (ou `@Html.AntiForgeryToken()`), pois o Razor não gera o token nesse caso e o POST seria recusado.
