# EnvioCorreo
git clone https://github.com/SergioValleGarma/EnvioCorreoMVC.git



# migracion para base de Datos
dotnet ef migrations remove

dotnet ef migrations add AddMatriculaEsquema
dotnet ef database update

# Dockerizar 
sergio@DESKTOP-KSJLU0B:/mnt/c/Users/Sergio/Downloads/EnvioCorreo/EnvioCorreo/EnvioCorreo$ docker-compose down -v

docker compose up --build -d

# para crear la base de datos en el Docker
docker compose stop envio_correo_app
docker compose run --rm app dotnet ef database update

docker compose up -d

# segundo intento para la base de datos en docker
docker compose down

docker compose up --build -d
docker compose run --rm ef-tools dotnet ef database update

docker-compose run --rm ef-tools sh -c "dotnet tool install --global dotnet-ef --version 8.0.5 && /root/.dotnet/tools/dotnet-ef database update --project .."

# este es el ultimo intento docker-compose run --rm ef-tools sh -c "sleep 20 && dotnet tool install --global dotnet-ef --version 8.0.5 && /root/.dotnet/tools/dotnet-ef database update --project .."
