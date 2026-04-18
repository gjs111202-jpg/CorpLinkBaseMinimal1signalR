using CorpLinkBaseMinimal.Auth;
using CorpLinkBaseMinimal.Data;
using CorpLinkBaseMinimal.Hubs;
using CorpLinkBaseMinimal.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddDbContextFactory<MessengerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MessengerCorpLink")));

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 3;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<MessengerDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ResolveAuthCookieName(builder.Configuration);
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

builder.Services.AddScoped<IMessengerRepository, MessengerRepository>();
builder.Services.AddScoped<MessengerService>();
builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/login", async (HttpContext context, SignInManager<User> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var usernameOrEmail = form["UsernameOrEmail"].ToString();
    var password = form["Password"].ToString();
    var returnUrl = ReturnUrlHelper.Sanitize(form["ReturnUrl"].ToString());

    var user = await signInManager.UserManager.FindByNameAsync(usernameOrEmail);
    if (user is null)
        user = await signInManager.UserManager.FindByEmailAsync(usernameOrEmail);

    if (user is null)
        return Results.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}&error=credentials");

    var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);
    if (!result.Succeeded)
        return Results.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}&error=credentials");

    return Results.Redirect(returnUrl);
});

app.MapPost("/auth/register", async (HttpContext context, SignInManager<User> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var userName = form["UserName"].ToString();
    var displayName = form["DisplayName"].ToString();
    var email = form["Email"].ToString();
    var password = form["Password"].ToString();
    var returnUrl = ReturnUrlHelper.Sanitize(form["ReturnUrl"].ToString());

    var messengerService = context.RequestServices.GetRequiredService<MessengerService>();
    var result = await messengerService.CreateUserAsync(userName, displayName, email, password);

    if (!result.Success)
    {
        string errorParam = result.ErrorCode switch
        {
            RegistrationError.UsernameEmpty => "username_empty",
            RegistrationError.UsernameTaken => "username_taken",
            RegistrationError.EmailEmpty => "email_empty",
            RegistrationError.EmailInvalid => "email_invalid",
            RegistrationError.EmailTaken => "email_taken",
            RegistrationError.PasswordEmpty => "password_empty",
            RegistrationError.PasswordTooWeak => "password_too_weak",
            _ => "unknown"
        };
        return Results.Redirect($"/register?returnUrl={Uri.EscapeDataString(returnUrl)}&error={errorParam}");
    }

   
    if (result.User is null)
    {
        return Results.Redirect($"/register?returnUrl={Uri.EscapeDataString(returnUrl)}&error=unknown");
    }

    await signInManager.SignInAsync(result.User, isPersistent: false);
    return Results.Redirect(returnUrl);
});

app.MapPost("/auth/logout", async (SignInManager<User> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});

app.MapBlazorHub();
app.MapHub<ChatHub>("/chathub");
app.MapFallbackToPage("/_Host");

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MessengerDbContext>>();
    await using var dbContext = await dbFactory.CreateDbContextAsync();
    await dbContext.Database.MigrateAsync();
}

app.Run();

static string ResolveAuthCookieName(IConfiguration configuration)
{
    var explicitName = configuration["Auth:CookieName"];
    if (!string.IsNullOrWhiteSpace(explicitName))
        return explicitName.Trim();

    var urls = configuration["ASPNETCORE_URLS"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "";
    foreach (var raw in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (Uri.TryCreate(raw, UriKind.Absolute, out var uri) && uri is { Scheme: "http" or "https", Port: > 0 })
            return $".CorpLink.Auth.{uri.Port}";
    }

    return ".CorpLink.Auth";
}