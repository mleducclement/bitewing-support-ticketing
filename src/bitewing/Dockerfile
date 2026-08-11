FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM node:20-alpine AS client
WORKDIR /client
COPY web/package*.json ./
RUN npm ci
COPY web/ ./
RUN npm run build -- --outDir dist --emptyOutDir

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["bitewing/bitewing.csproj", "bitewing/"]
RUN dotnet restore "bitewing/bitewing.csproj"
COPY . .
COPY --from=client /client/dist ./bitewing/wwwroot
WORKDIR "/src/bitewing"
RUN dotnet build "./bitewing.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./bitewing.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "bitewing.dll"]
