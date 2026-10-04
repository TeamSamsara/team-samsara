// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Storage/PostgresFileStorageService.cs
// Version : 1.1.0
// Latest commit: feature/asset-storage-routing
// Author : Gerrah
// Purpose : Provides the Postgres implementation of IStorageService.

using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;

namespace TeamSamsara.Shared.Storage;

public class PostgresFileStorageService : IStorageService
{
    #region Fields

    private readonly NpgsqlDataSource _dataSource;

    #endregion

    #region Constructors

    // Initializes the service with its Postgres data source
    public PostgresFileStorageService(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    #endregion

    #region Public Methods

    // Creates the schema and table if they don't already exist
    public async Task EnsureSchemaCreatedAsync()
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText =
            $"""
             CREATE SCHEMA IF NOT EXISTS {PostgresFileStorageSchema.SchemaName};
             CREATE TABLE IF NOT EXISTS {PostgresFileStorageSchema.QualifiedTableName} (
                 {PostgresFileStorageSchema.Columns.RelativePath} TEXT PRIMARY KEY,
                 {PostgresFileStorageSchema.Columns.Content} BYTEA NOT NULL,
                 {PostgresFileStorageSchema.Columns.ContentType} TEXT NOT NULL
             );
             """;

        await command.ExecuteNonQueryAsync();
    }

    // Uploads a file and returns its storage path
    public async Task<string> UploadAsync(string relativePath, Stream content, string contentType)
    {
        await using var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText =
            $"""
             INSERT INTO {PostgresFileStorageSchema.QualifiedTableName}
                 ({PostgresFileStorageSchema.Columns.RelativePath}, {PostgresFileStorageSchema.Columns.Content}, {PostgresFileStorageSchema.Columns.ContentType})
             VALUES (@relativePath, @content, @contentType)
             ON CONFLICT ({PostgresFileStorageSchema.Columns.RelativePath})
             DO UPDATE SET {PostgresFileStorageSchema.Columns.Content} = EXCLUDED.{PostgresFileStorageSchema.Columns.Content},
                            {PostgresFileStorageSchema.Columns.ContentType} = EXCLUDED.{PostgresFileStorageSchema.Columns.ContentType};
             """;

        command.Parameters.AddWithValue("relativePath", relativePath);
        command.Parameters.AddWithValue("content", bytes);
        command.Parameters.AddWithValue("contentType", contentType);

        await command.ExecuteNonQueryAsync();

        return relativePath;
    }

    // Downloads a file's content by its storage path
    public async Task<Stream> DownloadAsync(string relativePath)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText =
            $"SELECT {PostgresFileStorageSchema.Columns.Content} FROM {PostgresFileStorageSchema.QualifiedTableName} " +
            $"WHERE {PostgresFileStorageSchema.Columns.RelativePath} = @relativePath";
        command.Parameters.AddWithValue("relativePath", relativePath);

        var result = await command.ExecuteScalarAsync();

        if (result is not byte[] bytes)
        {
            throw new FileNotFoundException($"No file found at path '{relativePath}'.");
        }

        return new MemoryStream(bytes);
    }

    // Deletes a file by its storage path
    public async Task DeleteAsync(string relativePath)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText =
            $"DELETE FROM {PostgresFileStorageSchema.QualifiedTableName} " +
            $"WHERE {PostgresFileStorageSchema.Columns.RelativePath} = @relativePath";
        command.Parameters.AddWithValue("relativePath", relativePath);

        await command.ExecuteNonQueryAsync();
    }

    // Generates a temporary signed URL for a stored file.
    // Not applicable to Postgres storage - no public URL concept.
    public Task<string> GetSignedUrlAsync(string relativePath, TimeSpan expiry)
    {
        throw new NotImplementedException(
            "Postgres-backed storage has no public URL to sign - callers must use DownloadAsync instead.");
    }

    // Generates a permanent public URL for a stored file.
    // Not applicable to Postgres storage - no public URL concept.
    public Task<string> GetPublicUrlAsync(string relativePath)
    {
        throw new NotImplementedException(
            "Postgres-backed storage has no public URL - callers must use DownloadAsync instead.");
    }

    #endregion
}
