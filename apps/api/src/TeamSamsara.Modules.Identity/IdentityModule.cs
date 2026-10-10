// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/IdentityModule.cs
// Version : 1.9.0
// Latest commit: feat/email-change-endpoints
// Author : Gerrah
// Purpose : Registers the Identity module's services and maps its endpoints.

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

    // Registers settings, infrastructure, stores and handlers.
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        RegisterSettings(services, configuration);
        RegisterInfrastructure(services);
        RegisterRepositories(services);
        RegisterHandlers(services);
    }

    // Maps the signed-in endpoint group, plus password reset for callers who are signed out.
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(IdentityRoutes.Group)
            .RequireAuthorization();

        RegistrationEndpoints.Map(group);
        AuthenticationEndpoints.Map(group);
        ProfileEndpoints.Map(group);
        PasswordEndpoints.Map(group);
        EmailChangeEndpoints.Map(group);

        PasswordResetEndpoints.Map(endpoints);
    }

    #endregion

    #region Private Methods

    // Binds and validates settings; the app refuses to start if they are invalid.
    private static void RegisterSettings(IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<VerificationSettings>(configuration, VerificationSettings.SectionName);
        services.AddValidatedOptions<GuardDogSettings>(configuration, GuardDogSettings.SectionName);
        services.AddValidatedOptions<AuthenticationSettings>(configuration, AuthenticationSettings.SectionName);
        services.AddValidatedOptions<ProfileSettings>(configuration, ProfileSettings.SectionName);
        services.AddValidatedOptions<PasswordSettings>(configuration, PasswordSettings.SectionName);
    }

    // Registers shared services; TryAdd defers to registrations made elsewhere.
    private static void RegisterInfrastructure(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.TryAddSingleton<ICacheService, MemoryCacheService>();
        services.TryAddSingleton<IClock, SystemClock>();
    }

    // Registers the Firestore stores and the Firebase account gateway.
    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserStore, FirestoreUserStore>();
        services.AddScoped<IProfileStore, FirestoreProfileStore>();
        services.AddScoped<IVerificationCodeStore, FirestoreVerificationCodeStore>();
        services.AddScoped<IPasswordResetTokenStore, FirestorePasswordResetTokenStore>();
        services.AddScoped<IEmailChangeRequestStore, FirestoreEmailChangeRequestStore>();
        services.AddScoped<IAccountGateway, FirebaseAccountGateway>();
    }

    // Registers the module's services; SignInTracker backs both ISignInTracker and ISignInVerifier.
    private static void RegisterHandlers(IServiceCollection services)
    {
        services.AddScoped<IGuardDog, GuardDog>();
        services.AddScoped<IVerificationCodeService, VerificationCodeService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IPasswordResetDelivery, PasswordResetDelivery>();
        services.AddScoped<IEmailChangeService, EmailChangeService>();
        services.AddScoped<IEmailChangeDelivery, EmailChangeDelivery>();

        services.AddScoped<SignInTracker>();
        services.AddScoped<ISignInTracker>(provider => provider.GetRequiredService<SignInTracker>());
        services.AddScoped<ISignInVerifier>(provider => provider.GetRequiredService<SignInTracker>());
    }

    #endregion
}
