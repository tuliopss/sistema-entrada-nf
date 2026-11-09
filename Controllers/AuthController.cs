using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEntradaNotaFiscal.Models;
using SistemaEntradaNotaFiscal.Services;

namespace SistemaEntradaNotaFiscal.Controllers {
    [AllowAnonymous]
    public class AuthController : Controller {
        // Nome da claim que guarda o código de conexão do ERP.
        // Para ler em outros pontos: User.FindFirst(AuthController.ClaimCodigoConexao)?.Value
        public const string ClaimCodigoConexao = "CodigoConexao";

        private readonly AuthService _authService;

        public AuthController(AuthService authService) {
            _authService = authService;
        }

        [HttpGet("Auth")]
        public IActionResult Auth(string? returnUrl = null) {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToLocal(returnUrl);

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost("Auth")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Auth(LoginViewModel model, string? returnUrl = null) {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var usuario = model.Usuario.Trim();
            var result = await _authService.DoLoginAsync(usuario, model.Senha);

            if (!result.Success) {
                ModelState.AddModelError(string.Empty, result.Error ?? "Não foi possível entrar.");
                return View(model);   // a senha nunca é devolvida ao navegador
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, usuario),
                new(ClaimCodigoConexao, result.ConnectionCode!)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return RedirectToLocal(returnUrl);
        }

        [HttpPost("Auth/Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout() {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Auth));
        }

        // Só redireciona para endereços do próprio site (evita open redirect).
        private IActionResult RedirectToLocal(string? returnUrl) {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("ListarCapasNotas", "NotasEntradas");
        }
    }
}
