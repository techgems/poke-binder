# The production image for the web app. The build context is the repository root, not this folder,
# because the web project references its sibling projects:
#
#   docker build -f PokeBinder/Dockerfile -t pokebinder-app .
#
# Run it with the host's Databases/ folder mounted at /Databases (see the runtime stage).

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
COPY PokeBinder.Features/PokeBinder.Features.csproj PokeBinder.Features/
COPY PokeBinder.TcgCatalog.DbContext/PokeBinder.TcgCatalog.DbContext.csproj PokeBinder.TcgCatalog.DbContext/
RUN dotnet restore PokeBinder/PokeBinder.csproj

COPY PokeBinder/ PokeBinder/
COPY PokeBinder.Binder.DbContext/ PokeBinder.Binder.DbContext/
COPY PokeBinder.Binder.Users/ PokeBinder.Binder.Users/
COPY PokeBinder.Features/ PokeBinder.Features/
COPY PokeBinder.TcgCatalog.DbContext/ PokeBinder.TcgCatalog.DbContext/

# What the spa stage built replaces whatever copy of it the checkout had.
RUN rm -rf PokeBinder/wwwroot/BinderBuilderSvelte
COPY --from=spa /src/PokeBinder/wwwroot/BinderBuilderSvelte PokeBinder/wwwroot/BinderBuilderSvelte
COPY --from=spa /src/PokeBinder/wwwroot/css/binder-shell.css PokeBinder/wwwroot/css/binder-shell.css

RUN dotnet publish PokeBinder/PokeBinder.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

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

EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "PokeBinder.dll"]
