using EnvioCorreo.Data;
using EnvioCorreo.Models;
using EnvioCorreo.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();