
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY ["EnvioCorreo.csproj", "."]
RUN dotnet restore "EnvioCorreo.csproj"


COPY . .
WORKDIR "/src"

RUN dotnet publish "EnvioCorreo.csproj" -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080


COPY --from=build /app/publish .


COPY --from=build /src/Config ./Config 


ENV ASPNETCORE_URLS=http://+:8080


ENTRYPOINT ["dotnet", "EnvioCorreo.dll"]