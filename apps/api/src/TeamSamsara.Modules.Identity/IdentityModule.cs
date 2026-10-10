// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/IdentityModule.cs
// Version : 1.4.0
// Latest commit: feat/password-reset-token-store
// Author : Gerrah
// Purpose : Registers the Identity module's services and maps its HTTP endpoints. Every
// endpoint requires a signed-in caller (any access level): registration is used by accounts
// that are not yet members, and login checks run before a sign-in is verified. The profile
// and password endpoints are further limited to verified members.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeamSamsara.Modules.Identity.Endpoints;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Shared.Authentication;
using TeamSamsara.Shared.Caching;
using TeamSamsara.Shared.Configuration;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Modules;

namespace TeamSamsara.Modules.Identity;

public class IdentityModule : IModule
{
    #region Public Methods

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        RegisterSettings(services, configuration);
        RegisterInfrastructure(services);
        RegisterRepositories(services);
        RegisterHandlers(services);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(IdentityRoutes.Group)
            .RequireAuthorization();

        RegistrationEndpoints.Map(group);
        AuthenticationEndpoints.Map(group);
        ProfileEndpoints.Map(group);
        PasswordEndpoints.Map(group);
    }

    #endregion

    #region Private Methods

    // Binds and validates the module's settings (the app refuses to start if they are invalid)
    private static void RegisterSettings(IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<VerificationSettings>(configuration, VerificationSettings.SectionName);
        services.AddValidatedOptions<GuardDogSettings>(configuration, GuardDogSettings.SectionName);
        services.AddValidatedOptions<AuthenticationSettings>(configuration, AuthenticationSettings.SectionName);
        services.AddValidatedOptions<ProfileSettings>(configuration, ProfileSettings.SectionName);
        services.AddValidatedOptions<PasswordSettings>(configuration, PasswordSettings.SectionName);
    }

    // Shared building blocks the module relies on. TryAdd leaves them alone if another
    // module (or the host) registers its own.
    private static void RegisterInfrastructure(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.TryAddSingleton<ICacheService, MemoryCacheService>();
        services.TryAddSingleton<IClock, SystemClock>();
    }

    // Data access: Firestore stores and the Firebase account gateway
    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserStore, FirestoreUserStore>();
        services.AddScoped<IProfileStore, FirestoreProfileStore>();
        services.AddScoped<IVerificationCodeStore, FirestoreVerificationCodeStore>();
        services.AddScoped<IPasswordResetTokenStore, FirestorePasswordResetTokenStore>();
        services.AddScoped<IAccountGateway, FirebaseAccountGateway>();
    }

    // The module's logic. SignInTracker is one object exposed as two interfaces: Identity
    // records verified sign-ins through ISignInTracker, and the authentication handler in
    // Shared checks them through ISignInVerifier.
    private static void RegisterHandlers(IServiceCollection services)
    {
        services.AddScoped<IGuardDog, GuardDog>();
        services.AddScoped<IVerificationCodeService, VerificationCodeService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IPasswordService, PasswordService>();

        services.AddScoped<SignInTracker>();
        services.AddScoped<ISignInTracker>(provider => provider.GetRequiredService<SignInTracker>());
        services.AddScoped<ISignInVerifier>(provider => provider.GetRequiredService<SignInTracker>());
    }

    #endregion
}
