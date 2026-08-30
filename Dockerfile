ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT=src/ShipmentPlatform.Api/ShipmentPlatform.Api.csproj
WORKDIR /src

COPY ShipmentPlatform.slnx ./
COPY src/ShipmentPlatform.Domain/ShipmentPlatform.Domain.csproj src/ShipmentPlatform.Domain/
COPY src/ShipmentPlatform.Application/ShipmentPlatform.Application.csproj src/ShipmentPlatform.Application/
COPY src/ShipmentPlatform.Infrastructure/ShipmentPlatform.Infrastructure.csproj src/ShipmentPlatform.Infrastructure/
COPY src/ShipmentPlatform.Api/ShipmentPlatform.Api.csproj src/ShipmentPlatform.Api/
COPY src/ShipmentPlatform.OutboxWorker/ShipmentPlatform.OutboxWorker.csproj src/ShipmentPlatform.OutboxWorker/
COPY src/ShipmentPlatform.ConsumerWorker/ShipmentPlatform.ConsumerWorker.csproj src/ShipmentPlatform.ConsumerWorker/

RUN dotnet restore ${PROJECT}

COPY src/ src/
RUN dotnet publish ${PROJECT} -c Release -o /app/publish --no-restore

FROM ${RUNTIME_IMAGE} AS final
ARG DLL=ShipmentPlatform.Api.dll
WORKDIR /app
COPY --from=build /app/publish .
ENV DLL=${DLL}
ENTRYPOINT ["sh", "-c", "exec dotnet \"$DLL\""]
