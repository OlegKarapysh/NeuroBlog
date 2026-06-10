# syntax=docker/dockerfile:1

# ---- Build stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached unless a .csproj changes).
COPY NeuroBlog.slnx ./
COPY NeuroBlog/NeuroBlog.csproj NeuroBlog/
COPY NeuroBlog.Server/NeuroBlog.Server.csproj NeuroBlog.Server/
COPY NeuroBlog.Shared/NeuroBlog.Shared.csproj NeuroBlog.Shared/
RUN dotnet restore NeuroBlog.Server/NeuroBlog.Server.csproj

# Copy the rest and publish the server (bundles the Blazor WASM client).
COPY . .
RUN dotnet publish NeuroBlog.Server/NeuroBlog.Server.csproj -c Release -o /app/publish

# ---- Runtime stage ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "NeuroBlog.Server.dll"]
