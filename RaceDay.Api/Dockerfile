FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY RaceDay.Api/RaceDay.Api.csproj RaceDay.Api/
RUN dotnet restore RaceDay.Api/RaceDay.Api.csproj
COPY RaceDay.Api/ RaceDay.Api/
WORKDIR /src/RaceDay.Api
RUN dotnet publish RaceDay.Api.csproj --configuration Release --output /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN mkdir -p /app/DataProtection-Keys \
	&& chown -R $APP_UID:$APP_UID /app/DataProtection-Keys
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "RaceDay.Api.dll"]
