FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/FamilyOs.Api/FamilyOs.Api.csproj", "src/FamilyOs.Api/"]
COPY ["src/FamilyOs.Application/FamilyOs.Application.csproj", "src/FamilyOs.Application/"]
COPY ["src/FamilyOs.Domain/FamilyOs.Domain.csproj", "src/FamilyOs.Domain/"]
COPY ["src/FamilyOs.Infrastructure/FamilyOs.Infrastructure.csproj", "src/FamilyOs.Infrastructure/"]
COPY ["src/FamilyOs.Infrastructure.Ai/FamilyOs.Infrastructure.Ai.csproj", "src/FamilyOs.Infrastructure.Ai/"]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
RUN dotnet restore src/FamilyOs.Api/FamilyOs.Api.csproj
COPY . .
RUN dotnet publish src/FamilyOs.Api/FamilyOs.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FamilyOs.Api.dll"]
