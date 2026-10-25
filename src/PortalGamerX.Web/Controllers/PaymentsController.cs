using System.Text.Json;
using PortalGamerX.Data;
using PortalGamerX.Infrastructure;
using PortalGamerX.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Controllers;

public class PaymentsController : Controller
{
    private readonly IPaymentService _payments;
    private readonly AppDbContext _db;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService payments, AppDbContext db, ILogger<PaymentsController> logger)
    {
        _payments = payments;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Retorno do navegador após o pagamento. NÃO prova pagamento: se vier payment_id, o servidor consulta o provedor
    /// de forma autenticada (mesmo caminho do webhook) e só então o pedido pode ser marcado como pago.
    /// </summary>
    [Authorize]
    [HttpGet("pagamento/{result:regex(^(sucesso|falha|pendente)$)}")]
    public async Task<IActionResult> Return(string result, string? payment_id, string? collection_id, string? external_reference)
    {
        var paymentId = payment_id ?? collection_id;
        if (!string.IsNullOrWhiteSpace(paymentId) && paymentId.All(char.IsDigit))
        {
            var r = await _payments.ReconcileAsync(paymentId, "return");
            if (!r.Success) _logger.LogWarning("Reconciliação no retorno falhou: {Error}", r.Error);
        }

        var userId = User.GetUserId()!.Value;
        Guid? orderId = null;
        if (!string.IsNullOrWhiteSpace(external_reference))
        {
            orderId = await _db.Orders.Where(o => o.Number == external_reference && o.UserId == userId)
                .Select(o => (Guid?)o.Id).FirstOrDefaultAsync();
        }

        TempData[result == "falha" ? "Error" : "Info"] = result switch
        {
            "sucesso" => "Recebemos o retorno do pagamento. O pedido é confirmado assim que o provedor confirmar a aprovação.",
            "pendente" => "Pagamento pendente. Assim que for confirmado, atualizaremos o pedido.",
            _ => "O pagamento não foi concluído. Você pode tentar novamente na página do pedido."
        };
        return orderId.HasValue
            ? RedirectToAction("OrderDetails", "Manage", new { id = orderId.Value })
            : RedirectToAction("Orders", "Manage");
    }

    /// <summary>Webhook do Mercado Pago. Exige assinatura válida; o estado real vem da consulta autenticada à API.</summary>
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("webhook")]
    [HttpPost("webhooks/mercadopago")]
    public async Task<IActionResult> MercadoPagoWebhook()
    {
        string? dataId = Request.Query["data.id"].FirstOrDefault();
        string? type = Request.Query["type"].FirstOrDefault() ?? Request.Query["topic"].FirstOrDefault();

        if (dataId == null || type == null)
        {
            var body = await ReadBodyAsync(16 * 1024);
            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    if (type == null && root.TryGetProperty("type", out var t)) type = t.ToString();
                    if (dataId == null && root.TryGetProperty("data", out var d) && d.TryGetProperty("id", out var id)) dataId = id.ToString();
                }
                catch (JsonException) { return BadRequest(); }
            }
        }

        var outcome = await _payments.HandleWebhookAsync(
            type, dataId,
            Request.Headers["x-signature"].FirstOrDefault(),
            Request.Headers["x-request-id"].FirstOrDefault());

        return outcome switch
        {
            WebhookOutcome.Processed or WebhookOutcome.Ignored => Ok(),
            WebhookOutcome.Unauthorized => Unauthorized(),
            WebhookOutcome.BadRequest => BadRequest(),
            _ => StatusCode(500) // o provedor tentará novamente
        };
    }

    private async Task<string> ReadBodyAsync(int maxBytes)
    {
        using var reader = new StreamReader(Request.Body);
        var buffer = new char[maxBytes];
        var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
        return new string(buffer, 0, read);
    }
}
