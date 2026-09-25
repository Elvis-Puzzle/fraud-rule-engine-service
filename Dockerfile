# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# Trust any extra root CA dropped in certs/ (e.g. a corporate TLS-inspecting proxy) before
# restoring packages. Safe to run even when the folder is empty — see certs/README.md.
COPY certs/ /usr/local/share/ca-certificates/extra/
RUN find /usr/local/share/ca-certificates/extra -type f -name '*.pem' \
      -exec sh -c 'cp "$1" "${1%.pem}.crt"' _ {} \; ; \
    update-ca-certificates

COPY src/fraud-rule-engine-service/fraud-rule-engine-service.csproj fraud-rule-engine-service/
RUN dotnet restore fraud-rule-engine-service/fraud-rule-engine-service.csproj

COPY src/fraud-rule-engine-service/ fraud-rule-engine-service/
RUN dotnet publish fraud-rule-engine-service/fraud-rule-engine-service.csproj \
    -c Release -o /app --no-restore

# --- runtime ---
# Debian-based ("noble"), not Alpine, on both stages — Confluent.Kafka's native librdkafka
# library has no musl/arm64 build, so it needs glibc.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
ENV ASPNETCORE_URLS=http://+:8080

WORKDIR /app
COPY --from=build /app .

# mcr.microsoft.com/dotnet/aspnet images ship a built-in non-root "app" user (uid $APP_UID).
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "fraud-rule-engine-service.dll"]
