using EcommerceBase.Web.Domain;
using EcommerceBase.Web.Domain.Entities;
using EcommerceBase.Web.Domain.Rules;
using EcommerceBase.Web.Infrastructure;
using EcommerceBase.Web.Services;
using EcommerceBase.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EcommerceBase.Web.Controllers;

[Route("conta")]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IEmailService _email;
    private readonly ICartService _cart;
    private readonly IPublicUrl _url;
    private readonly IConfiguration _cfg;
    private readonly ILogger<AccountController> _logger;

    public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IEmailService email,
        ICartService cart, IPublicUrl url, IConfiguration cfg, ILogger<AccountController> logger)
    {
        _users = users;
        _signIn = signIn;
        _email = email;
        _cart = cart;
        _url = url;
        _cfg = cfg;
        _logger = logger;
    }

    // ---------- Login ----------
    [HttpGet("entrar")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginVm { ReturnUrl = returnUrl });

    [HttpPost("entrar")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginVm model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.FindByEmailAsync(model.Email.Trim());
        if (user != null && !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Esta conta está desativada. Entre em contato com a loja.");
            return View(model);
        }

        var anonToken = Request.Cookies[CartService.CookieName];
        // UserName == Email; usa sempre o e-mail informado para não revelar se a conta existe.
        var result = await _signIn.PasswordSignInAsync(user?.UserName ?? model.Email.Trim(), model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded && user != null)
        {
            user.LastLoginAtUtc = DateTime.UtcNow;
            await _users.UpdateAsync(user);
            await _cart.MergeAnonymousIntoUserAsync(user.Id, anonToken);
            Response.Cookies.Delete(CartService.CookieName);
            return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl! : "/");
        }
        if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, "Muitas tentativas. Conta bloqueada temporariamente; tente novamente em alguns minutos.");
        else if (result.IsNotAllowed)
            ModelState.AddModelError(string.Empty, "Confirme seu e-mail antes de entrar. Não recebeu? Use \"Reenviar confirmação\".");
        else
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
        return View(model);
    }

    [HttpPost("sair")]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("acesso-negado")]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // ---------- Cadastro (sempre Customer) ----------
    [HttpGet("cadastro")]
    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost("cadastro")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterVm model)
    {
        var doc = Documents.OnlyDigits(model.Document);
        if (!string.IsNullOrEmpty(doc) && !Documents.IsValidCpfOrCnpj(doc))
            ModelState.AddModelError(nameof(model.Document), "CPF/CNPJ inválido.");
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            Document = string.IsNullOrEmpty(doc) ? null : doc,
            PhoneNumber = model.Phone.NullIfEmpty(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var created = await _users.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            foreach (var e in created.Errors)
            {
                var msg = e.Code is "DuplicateUserName" or "DuplicateEmail"
                    ? "Este e-mail já está cadastrado. Tente entrar ou recuperar a senha."
                    : e.Description;
                ModelState.AddModelError(string.Empty, msg);
            }
            return View(model);
        }

        // Papel definido EXCLUSIVAMENTE aqui, no servidor. Nada vindo do formulário influencia o papel.
        var role = await _users.AddToRoleAsync(user, Roles.Customer);
        if (!role.Succeeded)
        {
            await _users.DeleteAsync(user);
            _logger.LogError("Falha ao atribuir papel Customer: {Errors}", string.Join(";", role.Errors.Select(x => x.Code)));
            ModelState.AddModelError(string.Empty, "Não foi possível concluir o cadastro. Tente novamente.");
            return View(model);
        }

        await SendConfirmationAsync(user);

        if (_cfg.GetValue("Identity:RequireConfirmedEmail", true))
            return View("RegisterConfirmation", user.Email);

        await _signIn.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Manage");
    }

    private async Task SendConfirmationAsync(ApplicationUser user)
    {
        var token = await _users.GenerateEmailConfirmationTokenAsync(user);
        var code = StringExtensions.Base64UrlEncode(token);
        var link = _url.Absolute(Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code })!);
        await _email.EnqueueAsync(EmailKinds.ConfirmEmail, user.Email!, user.FullName,
            new Dictionary<string, string> { ["Link"] = link }, sensitive: true);
    }

    [HttpGet("confirmar-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string? userId, string? code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code)) return View("ConfirmEmailResult", false);
        var user = await _users.FindByIdAsync(userId);
        if (user == null) return View("ConfirmEmailResult", false);
        try
        {
            var result = await _users.ConfirmEmailAsync(user, StringExtensions.Base64UrlDecode(code));
            return View("ConfirmEmailResult", result.Succeeded);
        }
        catch (FormatException)
        {
            return View("ConfirmEmailResult", false);
        }
    }

    [HttpGet("reenviar-confirmacao")]
    [AllowAnonymous]
    public IActionResult ResendConfirmation() => View(new ForgotPasswordVm());

    [HttpPost("reenviar-confirmacao")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendConfirmation(ForgotPasswordVm model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _users.FindByEmailAsync(model.Email.Trim());
        if (user != null && user.IsActive && !await _users.IsEmailConfirmedAsync(user)) await SendConfirmationAsync(user);
        return View("ForgotPasswordConfirmation"); // mesma resposta, exista a conta ou não
    }

    // ---------- Recuperação de senha ----------
    [HttpGet("esqueci-senha")]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordVm());

    [HttpPost("esqueci-senha")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVm model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _users.FindByEmailAsync(model.Email.Trim());
        if (user != null && user.IsActive && await _users.IsEmailConfirmedAsync(user))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var code = StringExtensions.Base64UrlEncode(token);
            var link = _url.Absolute(Url.Action(nameof(ResetPassword), "Account", new { userId = user.Id, code })!);
            await _email.EnqueueAsync(EmailKinds.ResetPassword, user.Email!, user.FullName,
                new Dictionary<string, string> { ["Link"] = link }, sensitive: true);
        }
        return View("ForgotPasswordConfirmation");
    }

    [HttpGet("redefinir-senha")]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? userId, string? code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code)) return BadRequest("Link inválido.");
        return View(new ResetPasswordVm { UserId = userId, Code = code });
    }

    [HttpPost("redefinir-senha")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(ResetPasswordVm model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _users.FindByIdAsync(model.UserId);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Link inválido ou expirado. Solicite uma nova redefinição.");
            return View(model);
        }
        try
        {
            var result = await _users.ResetPasswordAsync(user, StringExtensions.Base64UrlDecode(model.Code), model.Password);
            if (result.Succeeded) return View("ResetPasswordConfirmation");
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Code == "InvalidToken" ? "Link inválido ou expirado. Solicite uma nova redefinição." : e.Description);
        }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, "Link inválido ou expirado. Solicite uma nova redefinição.");
        }
        return View(model);
    }
}
