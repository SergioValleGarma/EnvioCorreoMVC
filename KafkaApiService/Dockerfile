FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["KafkaApiService.csproj", "."]
RUN dotnet restore "KafkaApiService.csproj"

COPY . .
RUN dotnet publish "KafkaApiService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "KafkaApiService.dll"]