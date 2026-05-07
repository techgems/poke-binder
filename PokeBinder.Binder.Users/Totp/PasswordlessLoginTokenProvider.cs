using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokeBinder.Binders.Users.Totp;

public class PasswordlessLoginTokenProvider<TUser> : DataProtectorTokenProvider<TUser> where TUser : class
{
    public PasswordlessLoginTokenProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<PasswordlessLoginTokenProviderOptions> options,
        ILogger<PasswordlessLoginTokenProvider<TUser>> logger)
        : base(dataProtectionProvider, options, logger)
    {
    }
}