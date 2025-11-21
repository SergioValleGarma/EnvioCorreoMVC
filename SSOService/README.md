# Para la migracion de la base de datos 
Add-Migration InitialCreate
Update-Database

# Swagger
http://localhost:7072/swagger

# Pasos para probar:
Asegúrate de que el SSO esté ejecutándose en https://localhost:7072
Abre http://localhost:3000

Prueba login con:
Email: admin@universidad.edu
Password: Admin123!