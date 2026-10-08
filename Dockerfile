# -----------------------------
# Build stage
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY DocumentFormatter.Domain/DocumentFormatter.Domain.csproj DocumentFormatter.Domain/
COPY DocumentFormatter.Application/DocumentFormatter.Application.csproj DocumentFormatter.Application/
COPY DocumentFormatter.Infrastructure/DocumentFormatter.Infrastructure.csproj DocumentFormatter.Infrastructure/
COPY DocumentFormatter.Server/DocumentFormatter.Server.csproj DocumentFormatter.Server/

RUN dotnet restore DocumentFormatter.Server/DocumentFormatter.Server.csproj

COPY . .

RUN dotnet publish DocumentFormatter.Server/DocumentFormatter.Server.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false


# -----------------------------
# Runtime stage
# -----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "DocumentFormatter.Server.dll"]
