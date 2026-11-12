using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using SistemaEntradaNotaFiscal.Data;
using SistemaEntradaNotaFiscal.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddDbContext<HmlContext>(options =>
    options.UseSqlServer(
            Environment.GetEnvironmentVariable("HMLCONTEXT")
        )
);

builder.Configuration["Senior:Url"] ??= builder.Configuration["SENIOR_URL"];
builder.Configuration["Senior:Usuario"] ??= builder.Configuration["SENIOR_USUARIO"];
builder.Configuration["Senior:Senha"] ??= builder.Configuration["SENIOR_SENHA"];
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<NotasEntradasService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProdutoService>();
builder.Services.AddScoped<ProcessamentoNotaService>();
builder.Services.AddScoped<ReabilitacaoNotaService>();
builder.Services.AddScoped<ISeniorNotaFiscalService, SeniorNotaFiscalService>();



builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => {
        o.LoginPath = "/Auth";
        o.AccessDeniedPath = "/Auth";
        o.Cookie.Name = "SENF.Auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;  // troque por Always quando houver HTTPS
        o.ExpireTimeSpan = TimeSpan.FromMinutes(120);
        o.SlidingExpiration = true;

        // Chamadas AJAX (o popup de itens) recebem 401 em vez de um redirecionamento para a tela de login
        o.Events.OnRedirectToLogin = ctx => {
            if (ctx.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            else
                ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
