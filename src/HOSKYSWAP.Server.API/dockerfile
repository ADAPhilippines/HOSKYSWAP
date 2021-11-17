FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /build
COPY src/ ./src
WORKDIR /build/src/HOSKYSWAP.Server.API
RUN dotnet restore
RUN dotnet publish -c Release -o /build/bin

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /build/bin .
ENTRYPOINT ["dotnet", "HOSKYSWAP.Server.API.dll"]