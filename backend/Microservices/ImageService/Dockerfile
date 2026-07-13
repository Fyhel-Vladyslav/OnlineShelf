# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
#FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
#USER $APP_UID
#WORKDIR /app
#EXPOSE 8080
#EXPOSE 8081


# This stage is used to build the service project
#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#ARG BUILD_CONFIGURATION=Release
#WORKDIR /src
#COPY ["ImageService/ImageService.csproj", "ImageService/"]
#RUN dotnet restore "./ImageService/ImageService.csproj"
#COPY . .
#WORKDIR "/src/ImageService"
#RUN dotnet build "./ImageService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "./ImageService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "ImageService.dll"]


# Використовуємо aspnet:8.0 для рантайму
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Відкриваємо порт 80 для gRPC (внутрішній)
EXPOSE 8080

# Етап збірки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Копіюємо проект та відновлюємо залежності
COPY ["ImageService/ImageService.csproj", "ImageService/"]
RUN dotnet restore "./ImageService/ImageService.csproj"
COPY . .
WORKDIR "/src/ImageService"
RUN dotnet build "./ImageService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Етап публікації
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ImageService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Фінальний етап
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Створюємо папку для фото та даємо права (якщо все ж використовуєш USER)
# Але для початку я закоментую USER $APP_UID, щоб уникнути проблем з правами на Windows Volume
# USER $APP_UID 

RUN mkdir -p /app/images && chmod 777 /app/images

ENTRYPOINT ["dotnet", "ImageService.dll"]