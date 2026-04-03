using CorpLinkBaseMinimal.Data;
using CorpLinkBaseMinimal.Hubs;
using CorpLinkBaseMinimal.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddDbContextFactory<MessengerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IMessengerRepository, MessengerRepository>();
builder.Services.AddScoped<MessengerService>();

builder.Services.AddScoped<MessengerService>();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
    var dbContext=
        scope.ServiceProvider.GetRequiredService<MessengerDbContext>();
    dbContext.Database.Migrate();
}
//123
app.Run();