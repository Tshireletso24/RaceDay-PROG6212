# ---- Build stage: restore, compile and publish the MVC app ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project file first so the restore step is cached between builds
COPY RaceDay.Web.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime stage: small image with only what is needed to run ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# The app listens on 8080 inside the container.
# ApiSettings__BaseUrl is where the RaceDay API is running (the host machine by default).
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ApiSettings__BaseUrl=http://host.docker.internal:5261

EXPOSE 8080
COPY --from=build /app/publish .

# Run as the non-root user built into the aspnet image
USER $APP_UID
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
