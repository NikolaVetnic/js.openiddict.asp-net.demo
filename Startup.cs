﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AuthorizationServer;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/account/login";
            });

        services.AddDbContext<DbContext>(options =>
        {
            // Configure the context to use an in-memory store.
            options.UseInMemoryDatabase(nameof(DbContext));

            // Register the entity sets needed by OpenIddict.
            options.UseOpenIddict();
        });

        services.AddOpenIddict()

        // Register the OpenIddict core components.
        .AddCore(options =>
        {
            // Configure OpenIddict to use the EF Core stores/models.
            options.UseEntityFrameworkCore()
                .UseDbContext<DbContext>();
        })

        // Register the OpenIddict server components.
        .AddServer(options =>
        {
            options
                // authorization code flow
                .AllowAuthorizationCodeFlow()
                .RequireProofKeyForCodeExchange()
                // client credentials flow
                .AllowClientCredentialsFlow()
                // refresh tokens flow
                .AllowRefreshTokenFlow();

            // ENMESHED SETTINGS
            options.AddDevelopmentSigningCertificate();
            options.AddDevelopmentEncryptionCertificate();
            options.AllowPasswordFlow();
            options.SetAccessTokenLifetime(TimeSpan.FromSeconds(300));

            options
                .SetAuthorizationEndpointUris("/connect/authorize")
                .SetTokenEndpointUris("/connect/token")
                .SetUserinfoEndpointUris("/connect/userinfo");

            // Encryption and signing of tokens
            options
                .AddEphemeralEncryptionKey()
                .AddEphemeralSigningKey()
                .DisableAccessTokenEncryption();

            // Register scopes (permissions)
            options.RegisterScopes("api");

            // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
            options
                .UseAspNetCore()
                .EnableTokenEndpointPassthrough()
                .EnableAuthorizationEndpointPassthrough()
                .EnableUserinfoEndpointPassthrough()
                // ENMESHED SETTINGS
                .DisableTransportSecurityRequirement();

            options.DisableTokenStorage();
        })
        .AddValidation(options =>
        {
            // Import the configuration (like valid issuer and the signing certificate) from the local OpenIddict server instance.
            options.UseLocalServer();
            options.UseAspNetCore();
        });

        services.AddHostedService<TestData>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
        });
    }
}

