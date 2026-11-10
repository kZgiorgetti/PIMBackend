# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# restaura primeiro (cache de camadas)
COPY src/Pim.Domain/Pim.Domain.csproj src/Pim.Domain/
COPY src/Pim.Application/Pim.Application.csproj src/Pim.Application/
COPY src/Pim.Infrastructure/Pim.Infrastructure.csproj src/Pim.Infrastructure/
COPY src/Pim.Api/Pim.Api.csproj src/Pim.Api/
RUN dotnet restore src/Pim.Api/Pim.Api.csproj
COPY src/ src/
RUN dotnet publish src/Pim.Api/Pim.Api.csproj -c Release -o /app --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Pim.Api.dll"]
