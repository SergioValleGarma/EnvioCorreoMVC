# EnvioCorreo
git clone https://github.com/SergioValleGarma/EnvioCorreoMVC.git



# migracion para base de Datos
dotnet ef migrations remove

dotnet ef migrations add AddMatriculaEsquema
dotnet ef database update

# Dockerizar 
sergio@DESKTOP-KSJLU0B:/mnt/c/Users/Sergio/Downloads/EnvioCorreo/EnvioCorreo/EnvioCorreo$ docker-compose down -v

docker compose up --build -d

