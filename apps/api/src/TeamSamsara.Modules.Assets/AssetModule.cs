// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Assets/AssetsModule.cs
// Version : 2.1.0
// Latest commit: feature/identity-profile
// Author : Gerrah
// Purpose : Registers the Assets module's services and HTTP endpoints.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamSamsara.Modules.Assets.Models;
using TeamSamsara.Modules.Assets.Repositories;
using TeamSamsara.Modules.Assets.Services;
using TeamSamsara.Shared.Assets;
using TeamSamsara.Shared.Configuration;
using TeamSamsara.Shared.Http;
using TeamSamsara.Shared.Modules;

namespace TeamSamsara.Modules.Assets;

public class AssetsModule : IModule
{
    #region Public Methods

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<AssetsPipelineSettings>(configuration, AssetsPipelineSettings.SectionName);
        services.AddValidatedOptions<AssetStorageRoutingSettings>(configuration, AssetStorageRoutingSettings.SectionName);
        services.AddValidatedOptions<MemberImageSettings>(configuration, MemberImageSettings.SectionName);

        services.AddScoped<IAssetMetadataStore, FirestoreAssetMetadataStore>();
        services.AddScoped<IAssetStorageService, AssetStorageService>();
        services.AddScoped<AssetService>();
        services.AddScoped<IMemberImageService, MemberImageService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/assets");

        // No cookie-based auth on this API, so there's no ambient credential for
        // a forged form submission to exploit - antiforgery protection doesn't apply here.
        group.MapPost("/", HandleUploadAsync)
            .DisableAntiforgery()
            .AddEndpointFilter<ApiKeyEndpointFilter<AssetsPipelineSettings>>();

        group.MapGet("/", HandleListAsync);
        group.MapGet("/{id}", HandleGetMetadataAsync);
        group.MapGet("/{id}/file", HandleGetFileAsync);

        group.MapDelete("/{id}", HandleDeleteAsync)
            .AddEndpointFilter<ApiKeyEndpointFilter<AssetsPipelineSettings>>();
    }

    #endregion

    #region Private Methods

    // Uploads a new asset from a multipart form (file + alt text)
    private static async Task<IResult> HandleUploadAsync(
        [FromForm] IFormFile file,
        [FromForm] string alt,
        AssetService assetService)
    {
        await using var stream = file.OpenReadStream();
        var metadata = await assetService.UploadAssetAsync(stream, file.FileName, file.ContentType, alt);

        return Results.Created($"/assets/{metadata.Id}", metadata);
    }

    // Lists all assets, optionally filtered by type (?type=Image)
    private static async Task<IResult> HandleListAsync([FromQuery] AssetType? type, AssetService assetService)
    {
        IReadOnlyList<AssetMetadata> assets = await assetService.ListAssetsAsync(type);

        return Results.Ok(assets);
    }

    // Returns an asset's metadata by id
    private static async Task<IResult> HandleGetMetadataAsync(string id, AssetService assetService)
    {
        var metadata = await assetService.GetAssetAsync(id);

        return metadata is null ? Results.NotFound() : Results.Ok(metadata);
    }

    // Returns an asset's file by id - proxied as a stream for image/file assets,
    // or a redirect to its own public URL for video
    private static async Task<IResult> HandleGetFileAsync(string id, AssetService assetService)
    {
        var file = await assetService.GetAssetFileAsync(id);

        if (file is null)
        {
            return Results.NotFound();
        }

        if (file.RedirectUrl is not null)
        {
            return Results.Redirect(file.RedirectUrl);
        }

        return Results.Stream(file.Content!, file.ContentType!);
    }

    // Deletes an asset's metadata and stored file by id
    private static async Task<IResult> HandleDeleteAsync(string id, AssetService assetService)
    {
        var deleted = await assetService.DeleteAssetAsync(id);

        return deleted ? Results.NoContent() : Results.NotFound();
    }

    #endregion
}
