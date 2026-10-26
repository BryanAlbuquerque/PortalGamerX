FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
# Publica explicitamente o projeto web (evita pegar, por engano, pastas antigas que sobrarem no repositório)
RUN dotnet publish src/PortalGamerX.Web/PortalGamerX.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_gcServer=0
COPY --from=build /app/publish .
# Render define a variável PORT; o Program.cs a respeita.
ENTRYPOINT ["dotnet", "PortalGamerX.dll"]