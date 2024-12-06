FROM mcr.microsoft.com/dotnet/sdk:6.0-bookworm-slim AS build-env

# ART_USER and ART_PASS referenced in nuget.config as ENV for Artifactory access
# pass from Jenkins as 'docker build --build-arg ART_USER=${ART_USER} --build-arg ART_PASS=${ART_PASS} -t ${CONTAINER} -f "${WORKSPACE}/WLSUser/Dockerfile"' in Jenkins .
ARG ART_USER
ARG ART_PASS
ARG ART_URL

WORKDIR /app

# Copy csproj and restore as distinct layers
COPY . /app
RUN set -x ;\
    set -o errexit ;\
    dotnet nuget locals all --clear ;\
    dotnet nuget add source \
        --name crossknowledge/phoenix "$ART_URL" \
        --username "$ART_USER" \
        --password "$ART_PASS" \
        --store-password-in-clear-text \
    ;\
    dotnet restore WLSUser ;\
    dotnet publish WLSUser -c Release -o /app/out --no-restore

# compile efbundle
ARG PATH="$PATH:/root/.dotnet/tools"
ARG MYSQL_VERSION="8.0.26"
ARG PRIVATE_KEY_FILE="/app/usersapi.pem"
RUN set -x ;\
    set -o errexit ;\
    dotnet tool install --global dotnet-ef --version 6.* ;\
    dotnet ef migrations bundle \
        --project WLSUser.Infrastructure \
        --startup-project WLSUser \
        --self-contained \
        -r linux-x64 \
        -o /app/out/efbundle \
        --force

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0-bookworm-slim
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "WLSUser.dll"]
