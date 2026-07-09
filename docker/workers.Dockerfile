FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/FamilyOs.Workers/FamilyOs.Workers.csproj", "src/FamilyOs.Workers/"]
COPY ["src/FamilyOs.Application/FamilyOs.Application.csproj", "src/FamilyOs.Application/"]
COPY ["src/FamilyOs.Domain/FamilyOs.Domain.csproj", "src/FamilyOs.Domain/"]
COPY ["src/FamilyOs.Infrastructure/FamilyOs.Infrastructure.csproj", "src/FamilyOs.Infrastructure/"]
COPY ["src/FamilyOs.Infrastructure.Ai/FamilyOs.Infrastructure.Ai.csproj", "src/FamilyOs.Infrastructure.Ai/"]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
RUN --mount=type=cache,id=nuget-workers,target=/root/.nuget/packages \
    --mount=type=cache,id=dotnet-sdk-workers,target=/root/.dotnet \
    dotnet restore src/FamilyOs.Workers/FamilyOs.Workers.csproj -r linux-x64
COPY . .
RUN --mount=type=cache,id=nuget-workers,target=/root/.nuget/packages \
    --mount=type=cache,id=dotnet-sdk-workers,target=/root/.dotnet \
    dotnet publish src/FamilyOs.Workers/FamilyOs.Workers.csproj \
      -c Release -o /app/publish --no-restore \
      -r linux-x64 --no-self-contained \
      -p:DebugType=none -p:DebugSymbols=false \
      /maxcpucount

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    tesseract-ocr \
    tesseract-ocr-hun \
    tesseract-ocr-eng \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FamilyOs.Workers.dll"]
