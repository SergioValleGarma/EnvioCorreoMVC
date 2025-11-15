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

// 3. REMOVER LA CONFIGURACIÓN DE KAFKA - Ya no la necesitamos
// builder.Configuration.AddJsonFile(
//     path: Path.Combine(builder.Environment.ContentRootPath, "Config", "kafkasettings.json"),
//     optional: false,
//     reloadOnChange: true
// );

// DEBUG: Verificar configuraciones cargadas
var rabbitMQConfig = builder.Configuration.GetSection("RabbitMQSettings");
Console.WriteLine($"[CONFIG] RabbitMQ Host: {rabbitMQConfig["HostName"]}");

// Registra las configuraciones
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQSettings"));

// REGISTRA HTTP CLIENT PARA KAFKA API
builder.Services.AddHttpClient<IKafkaApiClient, KafkaApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// REGISTRA LOS SERVICIOS DE MENSAJERÍA
builder.Services.AddSingleton<IMessageQueueService, RabbitMQPublisherService>();

// REMOVER EL SERVICIO DIRECTO DE KAFKA - ahora solo usamos el API
// builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

// REGISTRA EL CONSUMIDOR DE RABBITMQ
builder.Services.AddHostedService<EmailConsumerService>();

// REGISTRA EL SERVICIO DE CORREO
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

// ENDPOINTS DE PRUEBA PARA RABBITMQ
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

// Endpoint para verificar configuración
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

// Nuevo endpoint para probar Kafka API Client
app.MapGet("/debug-kafka-api", async (IKafkaApiClient kafkaClient, IServiceProvider serviceProvider) =>
{
    try
    {
        var kafkaApiClient = serviceProvider.GetService<IKafkaApiClient>();
        if (kafkaApiClient == null)
        {
            return Results.Problem("IKafkaApiClient is not registered in DI container");
        }

        // Test simple
        var testResult = await kafkaClient.SendGenericMessageAsync("test-topic", "Test message from API Client");

        return Results.Ok(new
        {
            kafkaApiClientRegistered = true,
            testMessageSent = testResult,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error checking Kafka API Client: {ex.Message}");
    }
});

app.Run();