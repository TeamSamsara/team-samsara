// File : /Team-Samsara/apps/api/src/TeamSamsara.Shared/Storage/PostgresFileStorageSchema
// Version: 1.0.0
// Latest commit : features/assets-module
// Author : Gerrah
// Purpose : Postgres schema for file storage implementation

namespace TeamSamsara.Shared.Storage;

public class PostgresFileStorageSchema
{
    public const string SchemaName = "assets";
    public const string TableName = "files";
    public const string QualifiedTableName = $"{SchemaName}.{TableName}";

    public static class Columns
    {
        public const string RelativePath = "relative_path";
        public const string Content = "content";
        public const string ContentType = "content_type";
    }
}
