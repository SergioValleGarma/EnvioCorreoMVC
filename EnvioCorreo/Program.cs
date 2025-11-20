using EnvioCorreo.Data;
using EnvioCorreo.Models;
using EnvioCorreo.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 🔥 AGREGAR CORS PARA PERMITIR EL FRONTEND REACT
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // URL de tu frontend React
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 1. Carga SOLO la configuración de mailsettings.json
builder.Configuration.AddJsonFile(
    path: Path.Combine(builder.Environment.ContentRootPath, "Config", "mailsettings.json"),
    optional: false,
    reloadOnChange: true
);

// DEBUG: Verificar configuraciones cargadas
Console.WriteLine($"[CONFIG] Kafka API URL: {builder.Configuration["KafkaApiBaseUrl"]}");
Console.WriteLine($"[CONFIG] RabbitMQ API URL: {builder.Configuration["RabbitMQApiBaseUrl"]}");

// Registra SOLO la configuración necesaria
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

// REGISTRA HTTP CLIENTS PARA APIs EXTERNAS
builder.Services.AddHttpClient<IKafkaApiClient, KafkaApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IRabbitMQApiClient, RabbitMQApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// REGISTRA EL NUEVO SERVICIO QUE USA LA API
builder.Services.AddSingleton<IMessageQueueService, RabbitMQApiClientService>();

// ✅ IMPORTANTE: REACTIVAR EL CONSUMIDOR DE EMAILS
//builder.Services.AddHostedService<EmailConsumerService>();

// REGISTRA EL SERVICIO DE CORREO
builder.Services.AddTransient<IEmailService, EmailService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Obtenemos la cadena de la configuración
var connectionString = builder.Configuration.GetConnectionString("UniversidadConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// 🔥 USAR CORS - ESTA LÍNEA ES CRÍTICA
app.UseCors("AllowReactApp");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ENDPOINTS DE PRUEBA ACTUALIZADOS PARA USAR KAFKA API
app.MapGet("/test-kafka-api", async (IKafkaApiClient kafkaClient) =>
{
    try
    {
        var testLog = new MatriculaLogEvent
        {
            MatriculaId = 999,
            EstudianteId = 1,
            SeccionId = 1,
            Costo = 100.50m,
            MetodoPago = "Test",
            Estado = "TEST",
            FechaMatricula = DateTime.Today,
            EventType = "TEST_EVENT",
            Message = "Mensaje de prueba para Kafka API"
        };

        var success = await kafkaClient.SendMatriculaLogAsync(testLog);

        return Results.Ok(new
        {
            message = "Test de Kafka API ejecutado",
            kafkaApiSuccess = success,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error testing Kafka API: {ex.Message}");
    }
});

// ENDPOINTS DE PRUEBA PARA RABBITMQ API
app.MapGet("/test-rabbitmq-api", async (IRabbitMQApiClient rabbitClient) =>
{
    try
    {
        Console.WriteLine($"[TEST] Starting RabbitMQ API test...");

        var testEvent = new EmailSentEvent
        {
            To = "test@example.com",
            Subject = "Test RabbitMQ API Connection",
            Body = "This is a test message from RabbitMQ API",
            SentDate = DateTime.UtcNow
        };

        Console.WriteLine($"[TEST] Sending test message to RabbitMQ API...");
        var success = await rabbitClient.PublishEmailMessageAsync(testEvent);
        Console.WriteLine($"[TEST] Message sent to API: {success}");

        return Results.Ok(new
        {
            message = "RabbitMQ API test message sent successfully",
            apiSuccess = success,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TEST ERROR] {ex.Message}");
        return Results.Problem($"Error testing RabbitMQ API: {ex.Message}");
    }
});

// Endpoint para verificar servicios externos
app.MapGet("/debug-services", async (IKafkaApiClient kafkaClient, IRabbitMQApiClient rabbitClient) =>
{
    try
    {
        // Probar Kafka API
        var kafkaHealth = await kafkaClient.SendGenericMessageAsync("health-check", "Health test");

        // Probar RabbitMQ API
        var testEmail = new EmailSentEvent
        {
            To = "health@test.com",
            Subject = "Health Check",
            Body = "Health test message",
            SentDate = DateTime.UtcNow
        };
        var rabbitHealth = await rabbitClient.PublishEmailMessageAsync(testEmail);

        return Results.Ok(new
        {
            kafkaApiHealth = kafkaHealth,
            rabbitMQApiHealth = rabbitHealth,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error checking external services: {ex.Message}");
    }
});

// Endpoint para probar el envío de email directamente
app.MapGet("/test-email-direct", async (IEmailService emailService) =>
{
    try
    {
        Console.WriteLine($"[TEST EMAIL] Probando envío directo de email...");

        await emailService.SendEmailAsync(
            "test@example.com",
            "Test Directo - Mailtrap",
            "Este es un test directo del servicio de email"
        );

        return Results.Ok(new
        {
            message = "Email de prueba enviado directamente",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TEST EMAIL ERROR] {ex.Message}");
        Console.WriteLine($"[TEST EMAIL ERROR STACK] {ex.StackTrace}");
        return Results.Problem($"Error enviando email directo: {ex.Message}");
    }
});

// 🔥 ENDPOINT DE HEALTH CHECK PARA EL FRONTEND
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "Backend funcionando correctamente",
        timestamp = DateTime.UtcNow,
        version = "1.0.0"
    });
});

// 🔥 ENDPOINT RAIZ TAMBIÉN PARA HEALTH CHECK
app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        message = "Backend de Gestión de Matrículas",
        status = "Operacional",
        timestamp = DateTime.UtcNow
    });
});

// 🔥 ENDPOINT PARA LISTAR MATRÍCULAS
app.MapGet("/api/matricula", async (ApplicationDbContext context) =>
{
    try
    {
        var matriculas = await context.Matriculas
            .Include(m => m.Estudiante)
            .OrderByDescending(m => m.MatriculaId)
            .Select(m => new
            {
                MatriculaId = m.MatriculaId,
                EstudianteId = m.EstudianteId,
                SeccionId = m.SeccionId,
                Costo = m.Costo,
                MetodoPago = m.MetodoPago,
                Estado = m.Estado,
                FechaMatricula = m.FechaMatricula,
                NombreCompletoEstudiante = m.Estudiante.Nombre + " " + m.Estudiante.Apellido
            })
            .ToListAsync();

        return Results.Ok(matriculas);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Error al obtener matrículas: {ex.Message}");
        return Results.Problem($"Error al obtener matrículas: {ex.Message}");
    }
});

// 🔥 ENDPOINT PARA OBTENER UNA MATRÍCULA POR ID
app.MapGet("/api/matricula/{id}", async (ApplicationDbContext context, int id) =>
{
    try
    {
        var matricula = await context.Matriculas
            .Include(m => m.Estudiante)
            .Where(m => m.MatriculaId == id)
            .Select(m => new
            {
                MatriculaId = m.MatriculaId,
                EstudianteId = m.EstudianteId,
                SeccionId = m.SeccionId,
                Costo = m.Costo,
                MetodoPago = m.MetodoPago,
                Estado = m.Estado,
                FechaMatricula = m.FechaMatricula,
                NombreCompletoEstudiante = m.Estudiante.Nombre + " " + m.Estudiante.Apellido
            })
            .FirstOrDefaultAsync();

        if (matricula == null)
        {
            return Results.NotFound($"Matrícula con ID {id} no encontrada");
        }

        return Results.Ok(matricula);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Error al obtener matrícula {id}: {ex.Message}");
        return Results.Problem($"Error al obtener matrícula: {ex.Message}");
    }
});

// 🔥 ENDPOINT PARA LISTAR ESTUDIANTES
app.MapGet("/api/estudiante", async (ApplicationDbContext context) =>
{
    try
    {
        var estudiantes = await context.Estudiantes
            .OrderBy(e => e.EstudianteId)
            .Select(e => new
            {
                EstudianteId = e.EstudianteId,
                Nombre = e.Nombre,
                Apellido = e.Apellido,
                Email = e.Email,
                Telefono = e.Telefono,
                Direccion = e.Direccion
            })
            .ToListAsync();

        return Results.Ok(estudiantes);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Error al obtener estudiantes: {ex.Message}");
        return Results.Problem($"Error al obtener estudiantes: {ex.Message}");
    }
});

// 🔥 ENDPOINT RAIZ PARA HEALTH CHECK DEL FRONTEND
app.MapGet("/api", () =>
{
    return Results.Ok(new
    {
        message = "API de Gestión de Matrículas - Backend funcionando",
        status = "Operacional",
        timestamp = DateTime.UtcNow,
        endpoints = new
        {
            matriculas = "/api/matricula",
            estudiantes = "/api/estudiante",
            auth = "/api/auth",
            health = "/health"
        }
    });
});

app.Run();