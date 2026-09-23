# File : /team-samsara/apps/api/src/TeamSamsara.Api/Dockerfile
# Purpose : Multi-stage build for Render. Build stage uses the full SDK;
# final stage uses only the runtime image, keeping the deployed container small.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY apps/api/TeamSamsara.sln .
COPY apps/api/src/TeamSamsara.Api/TeamSamsara.Api.csproj apps/api/src/TeamSamsara.Api/
COPY apps/api/src/TeamSamsara.Shared/TeamSamsara.Shared.csproj apps/api/src/TeamSamsara.Shared/
COPY apps/api/src/TeamSamsara.Modules.Identity/TeamSamsara.Modules.Identity.csproj apps/api/src/TeamSamsara.Modules.Identity/
COPY apps/api/src/TeamSamsara.Modules.Content/TeamSamsara.Modules.Content.csproj apps/api/src/TeamSamsara.Modules.Content/
COPY apps/api/src/TeamSamsara.Modules.Media/TeamSamsara.Modules.Media.csproj apps/api/src/TeamSamsara.Modules.Media/
COPY apps/api/src/TeamSamsara.Modules.Catalog/TeamSamsara.Modules.Catalog.csproj apps/api/src/TeamSamsara.Modules.Catalog/
COPY apps/api/src/TeamSamsara.Modules.Orders/TeamSamsara.Modules.Orders.csproj apps/api/src/TeamSamsara.Modules.Orders/
COPY apps/api/src/TeamSamsara.Modules.PingPong/TeamSamsara.Modules.PingPong.csproj apps/api/src/TeamSamsara.Modules.PingPong/
COPY Directory.Packages.props .
COPY global.json .

RUN dotnet restore apps/api/src/TeamSamsara.Api/TeamSamsara.Api.csproj

COPY apps/api/src/ apps/api/src/

RUN dotnet publish apps/api/src/TeamSamsara.Api/TeamSamsara.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "TeamSamsara.Api.dll"]
