FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["JanGet.Api/JanGet.Api.csproj", "JanGet.Api/"]
RUN dotnet restore "JanGet.Api/JanGet.Api.csproj"
COPY . .
WORKDIR "/src/JanGet.Api"
RUN dotnet build "JanGet.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JanGet.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JanGet.Api.dll"]
