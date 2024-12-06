FROM mcr.microsoft.com/dotnet/sdk:6.0-bookworm-slim AS build-env


# ART_USER and ART_PASS referenced in nuget.config as ENV for Artifactory access
# pass from Jenkins as 'docker build --build-arg ART_USER=${ART_USER} --build-arg ART_PASS=${ART_PASS} -t ${CONTAINER} -f "${WORKSPACE}/WLSUser.Tests/Dockerfile"' in Jenkins .
ARG ART_USER
ARG ART_PASS
ARG ART_URL

WORKDIR /app

COPY . ./

RUN dotnet nuget add source --name crossknowledge/phoenix $ART_URL --username $ART_USER --password $ART_PASS --store-password-in-clear-text && \
    dotnet restore

RUN dotnet build -c Release --no-restore
