using CorpLinkBaseMinimal.Data;
using CorpLinkBaseMinimal.Hubs;
using CorpLinkBaseMinimal.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddDbContextFactory<MessengerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MessengerDb")));

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

app.MapBlazorHub();
app.MapHub<ChatHub>("/chathub");
app.MapFallbackToPage("/_Host");

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MessengerDbContext>>();
    using var dbContext = factory.CreateDbContext();
    dbContext.Database.Migrate();
}

app.Run();