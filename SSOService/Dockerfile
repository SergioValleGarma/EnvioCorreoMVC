FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SSOService.csproj", "."]
RUN dotnet restore "SSOService.csproj"
COPY . .
RUN dotnet build "SSOService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SSOService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SSOService.dll"]