using Microsoft.AspNetCore.Identity;
using PokeBinder.Binders.DbContext.DI;
using PokeBinder.Binders.DbContext.Entities;
using PokeBinder.Binders.Users.DI;
using PokeBinder.TcgCatalog.DataAccess.DI;
using PokeBinder.TcgCatalog.Domain.DI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var applicationConnectionString = builder.Configuration.GetConnectionString("Application")
    ?? throw new InvalidOperationException("Connection string 'Application' not found.");
builder.Services.AddBinderDataAccess(applicationConnectionString);

builder.Services
    .AddBinderIdentity(options => options.SignIn.RequireConfirmedAccount = true)
    .AddSignInManager()
    .AddPasswordlessLoginTokenProvider()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

var tcgCatalogConnectionString = builder.Configuration.GetConnectionString("TcgCatalog")
    ?? throw new InvalidOperationException("Connection string 'TcgCatalog' not found.");


builder.Services.AddTcgCatalogDataAccess(tcgCatalogConnectionString);
builder.Services.AddTcgCatalogDomain();

builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
