using EcommerceBase.Web.Domain.Rules;
using EcommerceBase.Web.Infrastructure;
using EcommerceBase.Web.Integrations.Fiscal;
using EcommerceBase.Web.Services;
using EcommerceBase.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceBase.Web.Areas.Admin.Controllers;

/// <summary>
/// Menu Configurações. Segredos são gravados cifrados, nunca reexibidos e nunca registrados em log/auditoria.
/// Esta área NÃO permite editar a string de conexão do banco (somente por variável de ambiente).
/// </summary>
public class SettingsController : AdminBaseController
{
    private readonly IStoreSettingsService _store;
    private readonly IIntegrationSettings _cfg;
    private readonly IFileStorage _storage;
    private readonly IAuditService _audit;
    private readonly IPublicUrl _url;
    private readonly IPaymentService _payments;
    private readonly IShippingService _shipping;
    private readonly IEmailService _email;
    private readonly IFiscalService _fiscal;

    public SettingsController(IStoreSettingsService store, IIntegrationSettings cfg, IFileStorage storage, IAuditService audit, IPublicUrl url,
        IPaymentService payments, IShippingService shipping, IEmailService email, IFiscalService fiscal)
    {
        _store = store;
        _cfg = cfg;
        _storage = storage;
        _audit = audit;
        _url = url;
        _payments = payments;
        _shipping = shipping;
        _email = email;
        _fiscal = fiscal;
    }

    public IActionResult Index() => RedirectToAction(nameof(Company));

    // ================= Empresa =================
    public async Task<IActionResult> Company()
    {
        var s = await _store.GetAsync();
        return View(new CompanySettingsForm
        {
            LegalName = s.LegalName, TradeName = s.TradeName, Cnpj = s.Cnpj, StateRegistration = s.StateRegistration, MunicipalRegistration = s.MunicipalRegistration,
            Email = s.Email, Phone = s.Phone, PostalCode = s.PostalCode, Street = s.Street, Number = s.Number, Complement = s.Complement,
            District = s.District, City = s.City, State = s.State, Country = s.Country, Website = s.Website, SupportInfo = s.SupportInfo
        });
    }

    [HttpPost]
    public async Task<IActionResult> Company(CompanySettingsForm f)
    {
        var cnpj = Documents.OnlyDigits(f.Cnpj);
        // Valida apenas formato/dígitos verificadores: isso NÃO comprova a situação cadastral do CNPJ.
        if (cnpj.Length > 0 && !Documents.IsValidCnpj(cnpj)) ModelState.AddModelError(nameof(f.Cnpj), "CNPJ com formato ou dígitos verificadores inválidos.");
        if (!string.IsNullOrWhiteSpace(f.PostalCode) && !Documents.IsValidPostalCode(f.PostalCode)) ModelState.AddModelError(nameof(f.PostalCode), "CEP inválido.");
        if (!ModelState.IsValid) return View(f);

        await _store.SaveAsync(s =>
        {
            s.LegalName = f.LegalName.NullIfEmpty();
            s.TradeName = f.TradeName.NullIfEmpty();
            s.Cnpj = cnpj.Length == 0 ? null : cnpj;
            s.StateRegistration = f.StateRegistration.NullIfEmpty();
            s.MunicipalRegistration = f.MunicipalRegistration.NullIfEmpty();
            s.Email = f.Email.NullIfEmpty();
            s.Phone = f.Phone.NullIfEmpty();
            s.PostalCode = string.IsNullOrWhiteSpace(f.PostalCode) ? null : Documents.OnlyDigits(f.PostalCode);
            s.Street = f.Street.NullIfEmpty();
            s.Number = f.Number.NullIfEmpty();
            s.Complement = f.Complement.NullIfEmpty();
            s.District = f.District.NullIfEmpty();
            s.City = f.City.NullIfEmpty();
            s.State = f.State.NullIfEmpty()?.ToUpperInvariant();
            s.Country = f.Country.NullIfEmpty();
            s.Website = f.Website.NullIfEmpty();
            s.SupportInfo = f.SupportInfo.NullIfEmpty();
        });
        await _audit.LogAsync("settings.company");
        TempData["Success"] = "Dados da empresa salvos.";
        return RedirectToAction(nameof(Company));
    }

    // ================= Identidade visual =================
    public async Task<IActionResult> Visual()
    {
        var s = await _store.GetAsync();
        return View(new VisualSettingsForm
        {
            PrimaryColor = s.PrimaryColor, SecondaryColor = s.SecondaryColor, AccentColor = s.AccentColor, FooterText = s.FooterText,
            AboutText = s.AboutText, TermsText = s.TermsText, PrivacyText = s.PrivacyText, ShippingPolicyText = s.ShippingPolicyText,
            ReturnPolicyText = s.ReturnPolicyText, LogoPath = s.LogoPath, FaviconPath = s.FaviconPath
        });
    }

    [HttpPost]
    public async Task<IActionResult> Visual(VisualSettingsForm f)
    {
        var current = await _store.GetAsync();
        f.LogoPath = current.LogoPath;
        f.FaviconPath = current.FaviconPath;
        if (!ModelState.IsValid) return View(f);

        string? newLogo = null, newFavicon = null;
        if (f.LogoFile is { Length: > 0 })
        {
            var r = await _storage.SaveImageAsync(f.LogoFile, "branding");
            if (!r.Success) { ModelState.AddModelError(nameof(f.LogoFile), r.Error ?? "Imagem inválida."); return View(f); }
            newLogo = r.Path;
        }
        if (f.FaviconFile is { Length: > 0 })
        {
            var r = await _storage.SaveImageAsync(f.FaviconFile, "branding", allowIco: true);
            if (!r.Success) { ModelState.AddModelError(nameof(f.FaviconFile), r.Error ?? "Imagem inválida."); return View(f); }
            newFavicon = r.Path;
        }

        var oldLogo = current.LogoPath;
        var oldFavicon = current.FaviconPath;
        await _store.SaveAsync(s =>
        {
            s.PrimaryColor = f.PrimaryColor;
            s.SecondaryColor = f.SecondaryColor;
            s.AccentColor = f.AccentColor;
            s.FooterText = f.FooterText.NullIfEmpty();
            s.AboutText = f.AboutText.NullIfEmpty();
            s.TermsText = f.TermsText.NullIfEmpty();
            s.PrivacyText = f.PrivacyText.NullIfEmpty();
            s.ShippingPolicyText = f.ShippingPolicyText.NullIfEmpty();
            s.ReturnPolicyText = f.ReturnPolicyText.NullIfEmpty();
            if (newLogo != null) s.LogoPath = newLogo; else if (f.RemoveLogo) s.LogoPath = null;
            if (newFavicon != null) s.FaviconPath = newFavicon; else if (f.RemoveFavicon) s.FaviconPath = null;
        });
        if (newLogo != null || f.RemoveLogo) await _storage.DeleteAsync(oldLogo);
        if (newFavicon != null || f.RemoveFavicon) await _storage.DeleteAsync(oldFavicon);
        await _audit.LogAsync("settings.visual");
        TempData["Success"] = "Identidade visual salva.";
        return RedirectToAction(nameof(Visual));
    }

    // ================= Gerais =================
    public async Task<IActionResult> General()
    {
        var s = await _store.GetAsync();
        return View(new GeneralSettingsForm
        {
            StoreName = s.StoreName, Currency = s.Currency, TimeZoneId = s.TimeZoneId, LowStockThreshold = s.LowStockThreshold,
            PendingOrderExpirationMinutes = s.PendingOrderExpirationMinutes, MaxItemQuantityPerProduct = s.MaxItemQuantityPerProduct
        });
    }

    [HttpPost]
    public async Task<IActionResult> General(GeneralSettingsForm f)
    {
        if (f.Currency != "BRL") ModelState.AddModelError(nameof(f.Currency), "Nesta versão apenas BRL é suportado.");
        try { TimeZoneInfo.FindSystemTimeZoneById(f.TimeZoneId); }
        catch (Exception) { ModelState.AddModelError(nameof(f.TimeZoneId), "Fuso horário não reconhecido neste servidor."); }
        if (!ModelState.IsValid) return View(f);

        await _store.SaveAsync(s =>
        {
            s.StoreName = f.StoreName.Trim();
            s.Currency = f.Currency;
            s.TimeZoneId = f.TimeZoneId.Trim();
            s.LowStockThreshold = f.LowStockThreshold;
            s.PendingOrderExpirationMinutes = f.PendingOrderExpirationMinutes;
            s.MaxItemQuantityPerProduct = f.MaxItemQuantityPerProduct;
        });
        await _audit.LogAsync("settings.general");
        TempData["Success"] = "Configurações gerais salvas.";
        return RedirectToAction(nameof(General));
    }

    // ================= Pagamentos =================
    public async Task<IActionResult> Payments() => View(await BuildPaymentsAsync());

    private async Task<PaymentSettingsForm> BuildPaymentsAsync() => new()
    {
        Enabled = await _cfg.GetBoolAsync(SettingKeys.PaymentEnabled),
        Provider = await _cfg.GetOrDefaultAsync(SettingKeys.PaymentProvider, "MercadoPago"),
        Environment = await _cfg.GetOrDefaultAsync(SettingKeys.MpEnvironment, "sandbox"),
        PublicKey = await _cfg.GetAsync(SettingKeys.MpPublicKey),
        HasAccessToken = await _cfg.HasValueAsync(SettingKeys.MpAccessToken),
        HasWebhookSecret = await _cfg.HasValueAsync(SettingKeys.MpWebhookSecret),
        WebhookUrl = _url.Absolute("/webhooks/mercadopago")
    };

    [HttpPost]
    public async Task<IActionResult> Payments(PaymentSettingsForm f)
    {
        if (f.Environment is not ("sandbox" or "production")) ModelState.AddModelError(nameof(f.Environment), "Ambiente inválido.");
        if (f.Provider != "MercadoPago") ModelState.AddModelError(nameof(f.Provider), "Provedor inválido.");
        if (!ModelState.IsValid)
        {
            var rebuilt = await BuildPaymentsAsync();
            f.HasAccessToken = rebuilt.HasAccessToken; f.HasWebhookSecret = rebuilt.HasWebhookSecret; f.WebhookUrl = rebuilt.WebhookUrl;
            return View(f);
        }

        var by = CurrentUserName;
        await _cfg.SetAsync(SettingKeys.PaymentProvider, f.Provider, by);
        await _cfg.SetBoolAsync(SettingKeys.PaymentEnabled, f.Enabled, by);
        await _cfg.SetAsync(SettingKeys.MpEnvironment, f.Environment, by);
        await _cfg.SetAsync(SettingKeys.MpPublicKey, f.PublicKey, by);
        await _cfg.SetAsync(SettingKeys.MpAccessToken, f.AccessToken, by);
        await _cfg.SetAsync(SettingKeys.MpWebhookSecret, f.WebhookSecret, by);
        await _audit.LogAsync("settings.payments", details: $"habilitado={f.Enabled}; ambiente={f.Environment}; token alterado={!string.IsNullOrWhiteSpace(f.AccessToken)}; segredo webhook alterado={!string.IsNullOrWhiteSpace(f.WebhookSecret)}");
        TempData["Success"] = "Configurações de pagamento salvas.";
        return RedirectToAction(nameof(Payments));
    }

    [HttpPost]
    public async Task<IActionResult> TestPayments()
    {
        var r = await _payments.TestConnectionAsync();
        if (r.Success) TempData["Success"] = r.Value; else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Payments));
    }

    // ================= Frete =================
    public async Task<IActionResult> Shipping() => View(await BuildShippingAsync());

    private async Task<ShippingSettingsForm> BuildShippingAsync()
    {
        FlexibleDecimalModelBinder.TryParse(await _cfg.GetAsync(SettingKeys.ShippingFreeMinimum), out var free);
        return new ShippingSettingsForm
        {
            Provider = await _cfg.GetOrDefaultAsync(SettingKeys.ShippingProvider, "None"),
            OriginZip = Documents.FormatPostalCode(await _cfg.GetAsync(SettingKeys.ShippingOriginZip)),
            FreeMinimum = free > 0 ? free : null,
            HandlingDays = int.TryParse(await _cfg.GetAsync(SettingKeys.ShippingHandlingDays), out var h) ? h : 0,
            MeEnvironment = await _cfg.GetOrDefaultAsync(SettingKeys.ShippingMeEnvironment, "sandbox"),
            MeServices = await _cfg.GetAsync(SettingKeys.ShippingMeServices),
            MeContactEmail = await _cfg.GetAsync(SettingKeys.ShippingMeContactEmail),
            HasMeToken = await _cfg.HasValueAsync(SettingKeys.ShippingMeToken)
        };
    }

    [HttpPost]
    public async Task<IActionResult> Shipping(ShippingSettingsForm f)
    {
        if (f.Provider is not ("None" or "MelhorEnvio" or "Development")) ModelState.AddModelError(nameof(f.Provider), "Provedor inválido.");
        if (!string.IsNullOrWhiteSpace(f.OriginZip) && !Documents.IsValidPostalCode(f.OriginZip)) ModelState.AddModelError(nameof(f.OriginZip), "CEP inválido.");
        if (f.Provider != "None" && string.IsNullOrWhiteSpace(f.OriginZip)) ModelState.AddModelError(nameof(f.OriginZip), "Informe o CEP de origem.");
        if (f.MeEnvironment is not ("sandbox" or "production")) ModelState.AddModelError(nameof(f.MeEnvironment), "Ambiente inválido.");
        if (!string.IsNullOrWhiteSpace(f.MeServices) && !f.MeServices.All(c => char.IsDigit(c) || c == ',')) ModelState.AddModelError(nameof(f.MeServices), "Use apenas números separados por vírgula.");
        if (!ModelState.IsValid) { f.HasMeToken = await _cfg.HasValueAsync(SettingKeys.ShippingMeToken); return View(f); }

        var by = CurrentUserName;
        await _cfg.SetAsync(SettingKeys.ShippingProvider, f.Provider, by);
        await _cfg.SetAsync(SettingKeys.ShippingOriginZip, Documents.OnlyDigits(f.OriginZip), by);
        await _cfg.SetAsync(SettingKeys.ShippingFreeMinimum, f.FreeMinimum?.ToString(System.Globalization.CultureInfo.InvariantCulture), by);
        await _cfg.SetAsync(SettingKeys.ShippingHandlingDays, f.HandlingDays.ToString(), by);
        await _cfg.SetAsync(SettingKeys.ShippingMeEnvironment, f.MeEnvironment, by);
        await _cfg.SetAsync(SettingKeys.ShippingMeServices, f.MeServices, by);
        await _cfg.SetAsync(SettingKeys.ShippingMeContactEmail, f.MeContactEmail, by);
        await _cfg.SetAsync(SettingKeys.ShippingMeToken, f.MeToken, by);
        await _audit.LogAsync("settings.shipping", details: $"provedor={f.Provider}; token alterado={!string.IsNullOrWhiteSpace(f.MeToken)}");
        TempData["Success"] = "Configurações de frete salvas.";
        return RedirectToAction(nameof(Shipping));
    }

    [HttpPost]
    public async Task<IActionResult> TestShipping()
    {
        var r = await _shipping.TestConnectionAsync();
        if (r.Success) TempData["Success"] = r.Value; else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Shipping));
    }

    // ================= E-mail =================
    public async Task<IActionResult> Email() => View(await BuildEmailAsync());

    private async Task<EmailSettingsForm> BuildEmailAsync()
    {
        var f = new EmailSettingsForm
        {
            Host = await _cfg.GetAsync(SettingKeys.SmtpHost),
            Port = int.TryParse(await _cfg.GetAsync(SettingKeys.SmtpPort), out var p) ? p : 587,
            Security = await _cfg.GetOrDefaultAsync(SettingKeys.SmtpSecurity, "StartTls"),
            User = await _cfg.GetAsync(SettingKeys.SmtpUser),
            FromAddress = await _cfg.GetAsync(SettingKeys.EmailFromAddress),
            FromName = await _cfg.GetAsync(SettingKeys.EmailFromName),
            HasPassword = await _cfg.HasValueAsync(SettingKeys.SmtpPassword)
        };
        foreach (var kind in EmailKinds.All)
        {
            var def = EmailTemplates.Default(kind);
            f.Templates.Add(new EmailTemplateForm
            {
                Kind = kind,
                Label = EmailKinds.Label(kind),
                Optional = EmailKinds.Optional.Contains(kind),
                Notify = await _cfg.GetBoolAsync(SettingKeys.EmailNotify(kind), true),
                Subject = await _cfg.GetOrDefaultAsync(SettingKeys.EmailTemplateSubject(kind), def.Subject),
                Body = await _cfg.GetOrDefaultAsync(SettingKeys.EmailTemplateBody(kind), def.Body)
            });
        }
        return f;
    }

    [HttpPost]
    public async Task<IActionResult> Email(EmailSettingsForm f)
    {
        if (f.Security is not ("StartTls" or "SslOnConnect" or "None")) ModelState.AddModelError(nameof(f.Security), "Opção inválida.");
        foreach (var t in f.Templates)
            if (!EmailKinds.All.Contains(t.Kind)) ModelState.AddModelError(string.Empty, "Modelo inválido.");
        if (!ModelState.IsValid) { f.HasPassword = await _cfg.HasValueAsync(SettingKeys.SmtpPassword); return View(f); }

        var by = CurrentUserName;
        await _cfg.SetAsync(SettingKeys.EmailProvider, "Smtp", by);
        await _cfg.SetAsync(SettingKeys.SmtpHost, f.Host, by);
        await _cfg.SetAsync(SettingKeys.SmtpPort, f.Port.ToString(), by);
        await _cfg.SetAsync(SettingKeys.SmtpSecurity, f.Security, by);
        await _cfg.SetAsync(SettingKeys.SmtpUser, f.User, by);
        await _cfg.SetAsync(SettingKeys.SmtpPassword, f.Password, by);
        await _cfg.SetAsync(SettingKeys.EmailFromAddress, f.FromAddress, by);
        await _cfg.SetAsync(SettingKeys.EmailFromName, f.FromName, by);
        foreach (var t in f.Templates)
        {
            if (EmailKinds.Optional.Contains(t.Kind)) await _cfg.SetBoolAsync(SettingKeys.EmailNotify(t.Kind), t.Notify, by);
            await _cfg.SetAsync(SettingKeys.EmailTemplateSubject(t.Kind), t.Subject, by);
            await _cfg.SetAsync(SettingKeys.EmailTemplateBody(t.Kind), t.Body, by);
        }
        await _audit.LogAsync("settings.email", details: $"host={f.Host}; senha alterada={!string.IsNullOrWhiteSpace(f.Password)}");
        TempData["Success"] = "Configurações de e-mail salvas.";
        return RedirectToAction(nameof(Email));
    }

    [HttpPost]
    public async Task<IActionResult> TestEmail(string? to)
    {
        if (string.IsNullOrWhiteSpace(to) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(to))
        {
            TempData["Error"] = "Informe um e-mail de destino válido.";
            return RedirectToAction(nameof(Email));
        }
        var r = await _email.SendTestAsync(to.Trim());
        await _audit.LogAsync("settings.email.test", details: r.Success ? "enviado" : "falhou");
        if (r.Success) TempData["Success"] = "E-mail de teste enviado (confirmado pelo servidor SMTP)."; else TempData["Error"] = "Falha no envio: " + r.Error;
        return RedirectToAction(nameof(Email));
    }

    // ================= Fiscal =================
    public async Task<IActionResult> Fiscal() => View(await BuildFiscalAsync());

    private async Task<FiscalSettingsForm> BuildFiscalAsync() => new()
    {
        Provider = await _cfg.GetOrDefaultAsync(SettingKeys.FiscalProvider, "None"),
        Environment = await _cfg.GetOrDefaultAsync(SettingKeys.FiscalEnvironment, "homologacao"),
        DocumentType = await _cfg.GetOrDefaultAsync(SettingKeys.FiscalDocumentType, "NFe"),
        TaxRegime = await _cfg.GetAsync(SettingKeys.FiscalTaxRegime),
        Series = await _cfg.GetAsync(SettingKeys.FiscalSeries),
        HasToken = await _cfg.HasValueAsync(SettingKeys.FiscalToken),
        HasCertificatePassword = await _cfg.HasValueAsync(SettingKeys.FiscalCertificatePassword),
        ProviderConfigured = await _fiscal.IsConfiguredAsync()
    };

    [HttpPost]
    public async Task<IActionResult> Fiscal(FiscalSettingsForm f)
    {
        if (f.Environment is not ("homologacao" or "producao")) ModelState.AddModelError(nameof(f.Environment), "Ambiente inválido.");
        if (f.DocumentType is not ("NFe" or "NFCe")) ModelState.AddModelError(nameof(f.DocumentType), "Tipo inválido.");
        if (f.Provider != "None") ModelState.AddModelError(nameof(f.Provider), "Nenhum adaptador fiscal está implementado nesta versão: mantenha \"Nenhum\".");
        if (!ModelState.IsValid)
        {
            var rebuilt = await BuildFiscalAsync();
            f.HasToken = rebuilt.HasToken; f.HasCertificatePassword = rebuilt.HasCertificatePassword; f.ProviderConfigured = rebuilt.ProviderConfigured;
            return View(f);
        }
        var by = CurrentUserName;
        await _cfg.SetAsync(SettingKeys.FiscalProvider, f.Provider, by);
        await _cfg.SetAsync(SettingKeys.FiscalEnvironment, f.Environment, by);
        await _cfg.SetAsync(SettingKeys.FiscalDocumentType, f.DocumentType, by);
        await _cfg.SetAsync(SettingKeys.FiscalTaxRegime, f.TaxRegime, by);
        await _cfg.SetAsync(SettingKeys.FiscalSeries, f.Series, by);
        await _cfg.SetAsync(SettingKeys.FiscalToken, f.Token, by);
        await _cfg.SetAsync(SettingKeys.FiscalCertificatePassword, f.CertificatePassword, by);
        await _audit.LogAsync("settings.fiscal", details: $"ambiente={f.Environment}; documento={f.DocumentType}");
        TempData["Success"] = "Parâmetros fiscais salvos. " + NotConfiguredFiscalProvider.Message;
        return RedirectToAction(nameof(Fiscal));
    }
}
