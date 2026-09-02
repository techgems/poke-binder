using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using PokeBinder.Binders.DbContext.Entities;
using PokeBinder.Binders.Users.DI;
using PokeBinder.Features.CardImages;
using PokeBinder.Features.DI;
using ViteDotNet;
using ViteDotNet.NPM;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var applicationConnectionString = builder.Configuration.GetConnectionString("Application")
    ?? throw new InvalidOperationException("Connection string 'Application' not found.");

var tcgCatalogConnectionString = builder.Configuration.GetConnectionString("TcgCatalog")
    ?? throw new InvalidOperationException("Connection string 'TcgCatalog' not found.");

builder.Services.AddFeatures(applicationConnectionString, tcgCatalogConnectionString);

// Card art still sits on the machine that ran the ETL, recorded as absolute file paths. LocalRoot
// is the prefix stripped off those paths and BaseUrl is what replaces it, so pointing BaseUrl at a
// CDN later is the whole migration.
var cardImageRoot = builder.Configuration["CardImages:LocalRoot"] ?? string.Empty;
var cardImageBaseUrl = builder.Configuration["CardImages:BaseUrl"] ?? "/cardImages";

builder.Services.AddSingleton(new CardImageUrls(cardImageRoot, cardImageBaseUrl));

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
builder.Services.AddViteIntegration(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "PokeBinder API v1"));
    app.RunViteDevServer();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Temporary: while BaseUrl is still app-relative, serve the ETL's image folder ourselves so the
// art resolves in development. Once BaseUrl points at a CDN this stops mapping anything.
if (!cardImageBaseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
    && Directory.Exists(cardImageRoot))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(cardImageRoot),
        RequestPath = cardImageBaseUrl,
    });
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapControllers();

app.Run();
