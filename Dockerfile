#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS with-node
RUN apt-get update
RUN apt-get install curl
RUN curl -sL https://deb.nodesource.com/setup_20.x | bash
RUN apt-get -y install nodejs


FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Web.Server/Web.Server.csproj", "Web.Server/"]
RUN dotnet restore "./Web.Server/Web.Server.csproj"
COPY . .
WORKDIR "/src/Web.Server"
RUN dotnet build "./Web.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Web.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Superheater.Web.Server.dll"]
