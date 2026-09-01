using Microsoft.AspNetCore.Identity;
using PokeBinder.Binders.DbContext.Entities;
using PokeBinder.Binders.Users.DI;
using PokeBinder.Features.DI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var applicationConnectionString = builder.Configuration.GetConnectionString("Application")
    ?? throw new InvalidOperationException("Connection string 'Application' not found.");

var tcgCatalogConnectionString = builder.Configuration.GetConnectionString("TcgCatalog")
    ?? throw new InvalidOperationException("Connection string 'TcgCatalog' not found.");

builder.Services.AddFeatures(applicationConnectionString, tcgCatalogConnectionString);

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

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "PokeBinder API v1"));
}
else
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
app.MapControllers();

app.Run();
