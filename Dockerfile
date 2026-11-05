FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json ./
COPY Claims/Claims.csproj Claims/
RUN dotnet restore Claims/Claims.csproj
COPY Claims/ Claims/
RUN dotnet publish Claims/Claims.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Claims.dll"]
