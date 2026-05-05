FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RanitaApi/RanitaApi.csproj", "RanitaApi/"]
RUN dotnet restore "RanitaApi/RanitaApi.csproj"
COPY . .
WORKDIR "/src/RanitaApi"
RUN dotnet build "RanitaApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RanitaApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RanitaApi.dll"]