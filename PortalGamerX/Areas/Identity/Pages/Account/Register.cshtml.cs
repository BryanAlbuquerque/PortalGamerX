using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

using PortalGamerX.Models;

namespace PortalGamerX.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [MaxLength(100)]
            [Display(Name = "Nome")]
            public string Nome { get; set; } = string.Empty;

            [Required]
            [MaxLength(100)]
            [Display(Name = "Sobrenome")]
            public string Sobrenome { get; set; } = string.Empty;

            [MaxLength(14)]
            [Display(Name = "CPF")]
            public string CPF { get; set; }

            [Required]
            [MaxLength(20)]
            [Display(Name = "Telefone")]
            public string Telefone { get; set; }

            [Display(Name = "Data de Nascimento")]
            public DateTime? DataNascimento { get; set; }

            [Required]
            [Display(Name = "Aceito os Termos de Uso")]
            public bool AceitouTermosUso { get; set; }

            public DateTime? DataAceiteTermos { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(
                100,
                ErrorMessage = "A {0} deve ter no mínimo {2} e no máximo {1} caracteres.",
                MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Senha")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmar senha")]
            [Compare(
                "Password",
                ErrorMessage = "A senha e a confirmação de senha não são iguais.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;

            ExternalLogins =
                (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins =
                (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .ToList();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Cria o ApplicationUser
            var user = CreateUser();

            // Dados padrão do Identity
            await _userStore.SetUserNameAsync(
                user,
                Input.Email,
                CancellationToken.None);

            await _emailStore.SetEmailAsync(
                user,
                Input.Email,
                CancellationToken.None);

            // Dados personalizados do ApplicationUser
            user.Nome = Input.Nome;
            user.Sobrenome = Input.Sobrenome;
            user.CPF = Input.CPF;
            user.Telefone = Input.Telefone;
            user.DataNascimento = Input.DataNascimento;
            user.AceitouTermosUso = Input.AceitouTermosUso;

            // Só registra a data se o usuário realmente aceitou os termos
            if (Input.AceitouTermosUso)
            {
                user.DataAceiteTermos = DateTime.Now;
            }
            else
            {
                user.DataAceiteTermos = null;
            }

            // Cria o usuário no banco
            var result = await _userManager.CreateAsync(
                user,
                Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "Usuário criou uma nova conta com senha.");

                var userId =
                    await _userManager.GetUserIdAsync(user);

                var code =
                    await _userManager.GenerateEmailConfirmationTokenAsync(user);

                code =
                    WebEncoders.Base64UrlEncode(
                        Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new
                    {
                        area = "Identity",
                        userId = userId,
                        code = code,
                        returnUrl = returnUrl
                    },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Confirme seu e-mail",
                    $"Confirme sua conta clicando " +
                    $"<a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>" +
                    $"aqui</a>.");

                if (_userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    return RedirectToPage(
                        "RegisterConfirmation",
                        new
                        {
                            email = Input.Email,
                            returnUrl = returnUrl
                        });
                }
                else
                {
                    await _signInManager.SignInAsync(
                        user,
                        isPersistent: false);

                    return LocalRedirect(returnUrl);
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Não foi possível criar uma instância de " +
                    $"'{nameof(ApplicationUser)}'. " +
                    $"Verifique se a classe possui um construtor " +
                    $"sem parâmetros.");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException(
                    "O Identity configurado não suporta e-mail.");
            }

            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}