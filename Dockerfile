FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
# Localiza sozinho o projeto web (o que usa Microsoft.NET.Sdk.Web), seja qual for o nome
RUN WEB_PROJ=$(grep -rl --include=*.csproj "Microsoft.NET.Sdk.Web" . | head -n1) \
    && echo "Projeto web encontrado: $WEB_PROJ" \
    && dotnet publish "$WEB_PROJ" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_gcServer=0
COPY --from=build /app/publish .
# Render define a variável PORT; o Program.cs a respeita.
# Inicia a DLL principal, qualquer que seja o nome do projeto
ENTRYPOINT ["sh", "-c", "exec dotnet $(ls *.runtimeconfig.json | head -n1 | sed 's/\\.runtimeconfig\\.json$/.dll/')"]