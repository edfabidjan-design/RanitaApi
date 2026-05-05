FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RanitaApi/RanitaApi.csproj", "RanitaApi/"]
RUN dotnet restore "RanitaApi/RanitaApi.csproj"
COPY . .
WORKDIR "/src/RanitaApi"
RUN dotnet publish "RanitaApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ARG CACHEBUST=1
COPY --from=build /src/RanitaApi/wwwroot ./wwwroot
ENTRYPOINT ["dotnet", "RanitaApi.dll"]