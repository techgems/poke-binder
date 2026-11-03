using Microsoft.AspNetCore.Identity;
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

// Card art is served from a CDN in front of the card-image bucket, never by the app. Cards store
// the object key and BaseUrl is the CDN it is appended to.
var cardImageBaseUrl = builder.Configuration["CardImages:BaseUrl"]
    ?? throw new InvalidOperationException("CardImages:BaseUrl is not set.");

builder.Services.AddSingleton(new CardImageUrls(cardImageBaseUrl));

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

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapControllers();

app.Run();
