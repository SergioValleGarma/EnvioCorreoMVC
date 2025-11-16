# EnvioCorreo
git clone https://github.com/SergioValleGarma/EnvioCorreoMVC.git



# migracion para base de Datos
dotnet ef migrations remove

dotnet ef migrations add AddMatriculaEsquema
dotnet ef database update

# Dockerizar 
sergio@DESKTOP-KSJLU0B:/mnt/c/Users/Sergio/Downloads/EnvioCorreo/EnvioCorreo/EnvioCorreo$ docker-compose down -v

docker compose up --build -d

# postman
POST   http://localhost:7069/api/Matricula/registrar
{
    "EstudianteId": 14, 
    "SeccionId": 1, 
    "Costo": 200.00,
    "MetodoPago": "PAYPAL"
}

# Primero crear red docker
docker network create app-network

# RabbitMQ
http://localhost:15672/#/
usuario: guest
contraseña: guest

# Kafka
http://localhost:9000/

## limpiar docker

# Para todos los contenedores
docker-compose down

# Limpia el cache de construcción
docker system prune -f

# Limpia volúmenes no utilizados
docker volume prune -f

# Limpia imágenes no utilizadas
docker image prune -f