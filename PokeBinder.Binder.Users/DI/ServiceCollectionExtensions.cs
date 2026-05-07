using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PokeBinder.Binders.DbContext.Entities;
using PokeBinder.Binders.Users.Stores;
using PokeBinder.Binders.Users.Totp;

namespace PokeBinder.Binders.Users.DI;

public static class ServiceCollectionExtensions
{
    public static IdentityBuilder AddBinderIdentity(this IServiceCollection services, Action<IdentityOptions>? configureOptions = null)
    {
        var builder = services
            .AddIdentityCore<User>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                configureOptions?.Invoke(options);
            })
            .AddRoles<Role>();

        services.AddScoped<IUserStore<User>, UserStore>();
        services.AddScoped<IRoleStore<Role>, RoleStore>();

        return builder;
    }

    public static IdentityBuilder AddPasswordlessLoginTokenProvider(this IdentityBuilder builder)
    {
        var userType = builder.UserType;
        var provider = typeof(PasswordlessLoginTokenProvider<>).MakeGenericType(userType);
        return builder.AddTokenProvider(PasswordlessConstants.ProviderName, provider);
    }

}
