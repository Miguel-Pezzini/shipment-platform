FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ShipmentPlatform.slnx ./
COPY src/ShipmentPlatform.Domain/ShipmentPlatform.Domain.csproj src/ShipmentPlatform.Domain/
COPY src/ShipmentPlatform.Application/ShipmentPlatform.Application.csproj src/ShipmentPlatform.Application/
COPY src/ShipmentPlatform.Infrastructure/ShipmentPlatform.Infrastructure.csproj src/ShipmentPlatform.Infrastructure/
COPY src/ShipmentPlatform.Api/ShipmentPlatform.Api.csproj src/ShipmentPlatform.Api/

RUN dotnet restore src/ShipmentPlatform.Api/ShipmentPlatform.Api.csproj

COPY src/ src/
RUN dotnet publish src/ShipmentPlatform.Api/ShipmentPlatform.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShipmentPlatform.Api.dll"]
