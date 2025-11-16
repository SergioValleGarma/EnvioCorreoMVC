FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["RabbitMQApiService.csproj", "."]
RUN dotnet restore "RabbitMQApiService.csproj"

COPY . .
RUN dotnet publish "RabbitMQApiService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/mailsettings.json . 
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "RabbitMQApiService.dll"]