using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace PokeBinder.Binders.Users.Totp;

public class PasswordlessLoginTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public PasswordlessLoginTokenProviderOptions()
    {
        // update the defaults
        Name = PasswordlessConstants.ProviderName;
        TokenLifespan = TimeSpan.FromMinutes(15);
    }
}
