# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY Directory.Build.props Directory.Packages.props FintechPayments.sln ./
COPY src/FintechPayments.Domain/FintechPayments.Domain.csproj src/FintechPayments.Domain/
COPY src/FintechPayments.Application/FintechPayments.Application.csproj src/FintechPayments.Application/
COPY src/FintechPayments.Infrastructure/FintechPayments.Infrastructure.csproj src/FintechPayments.Infrastructure/
COPY src/FintechPayments.Api/FintechPayments.Api.csproj src/FintechPayments.Api/
COPY tests/FintechPayments.UnitTests/FintechPayments.UnitTests.csproj tests/FintechPayments.UnitTests/
COPY tests/FintechPayments.IntegrationTests/FintechPayments.IntegrationTests.csproj tests/FintechPayments.IntegrationTests/
RUN dotnet restore FintechPayments.sln

COPY . .
RUN dotnet publish src/FintechPayments.Api/FintechPayments.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "FintechPayments.Api.dll"]
