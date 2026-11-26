using Microsoft.AspNetCore.DataProtection;

namespace IdentityService.Security.Authorize;

public static class IdentityAuthorizeExtensions
{
    public static WebApplicationBuilder AddIdentityAuthorizeFlow(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<DataProtectionOptions>()
            .BindConfiguration(DataProtectionOptions.SectionName);

        var keysDirectory = builder.Configuration[$"{DataProtectionOptions.SectionName}:KeysDirectory"];
        var dataProtection = builder.Services.AddDataProtection();
        if (!string.IsNullOrWhiteSpace(keysDirectory))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysDirectory));
        }

        builder.Services
            .AddAuthentication()
            .AddCookie(IdentityIdpAuthentication.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "tb.idp.session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;
            });

        builder.Services.AddScoped<AuthorizeRequestService>();
        builder.Services.AddScoped<AuthorizationCodeIssuer>();
        builder.Services.AddScoped<AuthorizeInteractionService>();
        builder.Services.AddRazorPages();
        return builder;
    }
}