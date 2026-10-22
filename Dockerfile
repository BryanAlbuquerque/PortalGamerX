FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY src/EcommerceBase.Web/EcommerceBase.Web.csproj src/EcommerceBase.Web/
RUN dotnet restore src/EcommerceBase.Web/EcommerceBase.Web.csproj
COPY src/ src/
RUN dotnet publish src/EcommerceBase.Web/EcommerceBase.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_gcServer=0
COPY --from=build /app/publish .
# Render define a variável PORT; o Program.cs a respeita.
ENTRYPOINT ["dotnet", "EcommerceBase.Web.dll"]
