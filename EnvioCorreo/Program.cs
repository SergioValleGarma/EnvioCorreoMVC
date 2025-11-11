using EnvioCorreo.Data;
using EnvioCorreo.Models;
using EnvioCorreo.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// 1. Carga la configuración de mailsettings.json
builder.Configuration.AddJsonFile(
    path: Path.Combine(builder.Environment.ContentRootPath, "Config", "mailsettings.json"),
    optional: false,
    reloadOnChange: true
);

// 2. CARGA LA CONFIGURACIÓN DE RABBITMQ
builder.Configuration.AddJsonFile(
    path: Path.Combine(builder.Environment.ContentRootPath, "Config", "rabbitmqsettings.json"),
    optional: false,
    reloadOnChange: true
);

// 3. ? CARGA LA CONFIGURACIÓN DE KAFKA
builder.Configuration.AddJsonFile(
    path: Path.Combine(builder.Environment.ContentRootPath, "Config", "kafkasettings.json"),
    optional: false,
    reloadOnChange: true
);

// DEBUG: Verificar configuraciones cargadas
var rabbitMQConfig = builder.Configuration.GetSection("RabbitMQSettings");
var kafkaConfig = builder.Configuration.GetSection("KafkaSettings");
Console.WriteLine($"[CONFIG] RabbitMQ Host: {rabbitMQConfig["HostName"]}");
Console.WriteLine($"[CONFIG] Kafka Servers: {kafkaConfig["BootstrapServers"]}");

// Registra las configuraciones
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQSettings"));
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("KafkaSettings")); // ? NUEVO

// ? REGISTRA LOS SERVICIOS DE MENSAJERÍA
builder.Services.AddSingleton<IMessageQueueService, RabbitMQPublisherService>();
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>(); // ? NUEVO

// ? REGISTRA EL CONSUMIDOR DE RABBITMQ
builder.Services.AddHostedService<EmailConsumerService>();

// ? REGISTRA EL SERVICIO DE CORREO
builder.Services.AddTransient<IEmailService, EmailService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Obtenemos la cadena de la configuración
var connectionString = builder.Configuration.GetConnectionString("UniversidadConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
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

// ? ENDPOINTS DE PRUEBA PARA KAFKA
app.MapGet("/test-kafka", async (IKafkaProducerService kafkaService) =>
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
            Message = "Mensaje de prueba para Kafka"
        };

        var success = await kafkaService.ProduceMatriculaLogAsync(testLog);

        return Results.Ok(new
        {
            message = "Test de Kafka ejecutado",
            kafkaSuccess = success,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error testing Kafka: {ex.Message}");
    }
});


// ? ENDPOINTS DE PRUEBA
// ? ENDPOINTS DE PRUEBA - ACTUALIZADO para usar las propiedades correctas
app.MapGet("/test-rabbitmq", (IMessageQueueService rabbitService) =>
{
    try
    {
        Console.WriteLine($"[TEST] Starting RabbitMQ test...");

        var testEvent = new EmailSentEvent
        {
            To = "test@example.com",
            Subject = "Test RabbitMQ Connection",
            Body = "This is a test message from RabbitMQ",
            SentDate = DateTime.UtcNow
        };

        Console.WriteLine($"[TEST] Publishing test message to RabbitMQ...");
        rabbitService.PublishEmailSentMessage(testEvent);
        Console.WriteLine($"[TEST] Message published successfully");

        return Results.Ok(new
        {
            message = "RabbitMQ test message published successfully",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TEST ERROR] {ex.Message}");
        Console.WriteLine($"[TEST ERROR STACK] {ex.StackTrace}");
        return Results.Problem($"Error testing RabbitMQ: {ex.Message}");
    }
});

// ? Endpoint para verificar configuración
app.MapGet("/debug-config", (IOptions<RabbitMQSettings> rabbitSettings) =>
{
    var config = rabbitSettings.Value;
    Console.WriteLine($"[DEBUG CONFIG] Host: {config.HostName}, Queue: {config.QueueName}");
    return Results.Ok(new
    {
        hostName = config.HostName,
        userName = config.UserName,
        queueName = config.QueueName,
        port = config.Port,
        configLoaded = config != null
    });
});

// ? Endpoint para verificar si el servicio está registrado
app.MapGet("/debug-service", (IServiceProvider serviceProvider) =>
{
    try
    {
        var service = serviceProvider.GetService<IMessageQueueService>();
        if (service == null)
        {
            Console.WriteLine($"[DEBUG SERVICE] IMessageQueueService is NULL - NOT REGISTERED");
            return Results.Problem("IMessageQueueService is not registered in DI container");
        }

        Console.WriteLine($"[DEBUG SERVICE] IMessageQueueService is properly registered");
        return Results.Ok(new { serviceRegistered = true, serviceType = service.GetType().Name });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DEBUG SERVICE ERROR] {ex.Message}");
        return Results.Problem($"Error checking service: {ex.Message}");
    }
});

app.Run();