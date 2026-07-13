FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1.  оп≥юЇмо файл проекту ≥ в≥дновлюЇмо пакети
COPY ["ShelfsService/ShelfsService.csproj", "ShelfsService/"]
RUN dotnet restore "./ShelfsService/ShelfsService.csproj"

# 2. ¬»ѕ–ј¬Ћ≈ЌЌя:  оп≥юЇмо “≤Ћ№ » папку нашого серв≥су, а не весь кор≥нь!
# ÷е повн≥стю ≥золюЇ файли в≥д сус≥дн≥х м≥кросерв≥с≥в
COPY ShelfsService/ ShelfsService/

# 3. ѕереходимо в папку проекту ≥ б≥лдимо
WORKDIR "/src/ShelfsService"
RUN dotnet build "./ShelfsService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ShelfsService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "ShelfsService.dll"]