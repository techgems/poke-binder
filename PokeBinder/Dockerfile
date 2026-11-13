# The production image for the web app. The build context is the repository root, not this folder,
# because the web project references its sibling projects:
#
#   docker build -f PokeBinder/Dockerfile -t pokebinder-app .
#
# Run it with the host's Databases/ folder mounted at /Databases (see the runtime stage), with
# access to the pokebinder-db-backups bucket, which Litestream replicates the databases into, and
# with Bird__ApiKey set, which the app will not start without. Mail only goes out once
# Bird__FromMail is set as well.

# --- The SPA bundle and the binder shell stylesheet ------------------------------------------------
# Both are npm builds out of the SPA folder, and they are separate artifacts: building one does not
# refresh the other. site.tailwind.css is not rebuilt here -- it comes from the standalone Tailwind
# CLI, which this image does not carry, so the committed sheet is the one that ships.
FROM node:24-bookworm-slim AS spa
WORKDIR /src/PokeBinder/BinderBuilderSvelte

COPY PokeBinder/BinderBuilderSvelte/package.json PokeBinder/BinderBuilderSvelte/package-lock.json ./
RUN npm ci

COPY PokeBinder/BinderBuilderSvelte/ ./
# The shell sheet scans the Razor trees through the @source lines in src/app.css, so they have to be
# in this stage too, or every Razor-only class silently drops out of the sheet.
COPY PokeBinder/Pages ../Pages
COPY PokeBinder/Components ../Components

RUN npm run build && npm run build:shell-css

# --- The .NET publish ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first, so the restore layer survives a change that only touches code.
COPY PokeBinder/PokeBinder.csproj PokeBinder/
COPY PokeBinder.Binder.DbContext/PokeBinder.Binder.DbContext.csproj PokeBinder.Binder.DbContext/
COPY PokeBinder.Binder.Users/PokeBinder.Binder.Users.csproj PokeBinder.Binder.Users/
COPY PokeBinder.Emails/PokeBinder.Emails.csproj PokeBinder.Emails/
COPY PokeBinder.Features/PokeBinder.Features.csproj PokeBinder.Features/
COPY PokeBinder.TcgCatalog.DbContext/PokeBinder.TcgCatalog.DbContext.csproj PokeBinder.TcgCatalog.DbContext/
RUN dotnet restore PokeBinder/PokeBinder.csproj

COPY PokeBinder/ PokeBinder/
COPY PokeBinder.Binder.DbContext/ PokeBinder.Binder.DbContext/
COPY PokeBinder.Binder.Users/ PokeBinder.Binder.Users/
COPY PokeBinder.Emails/ PokeBinder.Emails/
COPY PokeBinder.Features/ PokeBinder.Features/
COPY PokeBinder.TcgCatalog.DbContext/ PokeBinder.TcgCatalog.DbContext/

# What the spa stage built replaces whatever copy of it the checkout had.
RUN rm -rf PokeBinder/wwwroot/BinderBuilderSvelte
COPY --from=spa /src/PokeBinder/wwwroot/BinderBuilderSvelte PokeBinder/wwwroot/BinderBuilderSvelte
COPY --from=spa /src/PokeBinder/wwwroot/css/binder-shell.css PokeBinder/wwwroot/css/binder-shell.css

RUN dotnet publish PokeBinder/PokeBinder.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# --- Litestream ------------------------------------------------------------------------------------
# The release binary, checked against hashes pinned here rather than against the release's own
# checksums.txt, which would only prove the download matches whatever the same server sent. A new
# version means a new LITESTREAM_VERSION and both hashes from that release's checksums.txt.
FROM alpine:3.22 AS litestream
ARG TARGETARCH
ARG LITESTREAM_VERSION=0.5.17
RUN set -eu; \
    case "$TARGETARCH" in \
      amd64) arch=x86_64; sha=cfb371176d164437ae869f8351cfde49bd1804ae71c61923f75c9cba9c9c006d ;; \
      arm64) arch=arm64;  sha=f8ca4a050095c1efbda2c4365172e61bf9d955ea0d9ac42f448b52e51819baa5 ;; \
      *) echo "no Litestream build for $TARGETARCH" >&2; exit 1 ;; \
    esac; \
    wget -qO /tmp/litestream.tar.gz \
      "https://github.com/benbjohnson/litestream/releases/download/v$LITESTREAM_VERSION/litestream-$LITESTREAM_VERSION-linux-$arch.tar.gz"; \
    echo "$sha  /tmp/litestream.tar.gz" | sha256sum -c -; \
    tar -xzf /tmp/litestream.tar.gz -C /usr/local/bin litestream

# --- Runtime ---------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# The SQLite files stay on the host. appsettings.json names them as ../Databases, which from /app is
# /Databases: mount the host's folder there and the databases outlive every rebuild of the image.
VOLUME /Databases

# The image only ever runs behind nginx, which terminates TLS. Without the forwarded headers the app
# sees every request as plain HTTP, and the HTTPS redirect sends it straight back round.
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

COPY --from=litestream /usr/local/bin/litestream /usr/local/bin/litestream
COPY PokeBinder/litestream.yml /etc/litestream.yml
ENV DATABASES_DIR=/Databases \
    LITESTREAM_BUCKET=pokebinder-db-backups

EXPOSE 8080
USER $APP_UID
# Litestream starts first, restores any database /Databases is missing, then runs the app as its
# child and replicates for as long as it lives; the container stops when the app does. Credentials
# come from the EC2's instance role, or AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY.
ENTRYPOINT ["litestream", "replicate", "-exec", "dotnet PokeBinder.dll"]
