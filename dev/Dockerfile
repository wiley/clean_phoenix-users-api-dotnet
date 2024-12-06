FROM mcr.microsoft.com/dotnet/sdk:6.0-bookworm-slim

SHELL ["/bin/bash", "-o", "pipefail", "-o", "errexit", "-o", "xtrace", "-c"]
ARG DEBIAN_FRONTEND=noninteractive
RUN \
    apt-get update ;\
    apt-get install -y -q --no-install-recommends \
        # enables debug with VSCode, we need the command line "ps"
        procps \
    ;\
    # Option '-m' to create home directory see https://askubuntu.com/a/393470
    # How to set bash as shell, see comments in the same answer
    useradd -m -s $(which bash) developer ;\
    # Create working directory and change ownership.
    # Ownership will be preserved when mounting volume to /app
    # See https://stackoverflow.com/a/56990338/411428
    # and https://github.com/docker/compose/issues/3270#issuecomment-543603959
    mkdir -p /app/run ;\
    chown developer:developer /app/run ;\
    \
    dotnet tool install --tool-path /usr/local/bin dotnet-ef --version 6.* ;\
    \
     # cleaning
    apt-get autoremove -y ;\
    apt-get -y clean ;\
    rm -rf \
        /var/lib/apt/lists/* \
        /tmp/* \
        /var/tmp/* \
        /usr/share/doc/*

RUN \
    mkdir -p /nuget_cache /nuget_packages ;\
    chown developer:developer /nuget_cache /nuget_packages
ENV NUGET_PACKAGES "/nuget_packages"
ENV NUGET_HTTP_CACHE_PATH "/nuget_cache"

COPY ./entrypoint.dev.sh /app/run/entrypoint.dev.sh
ENTRYPOINT ["/app/run/entrypoint.dev.sh"]

USER developer
