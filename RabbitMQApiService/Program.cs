using RabbitMQApiService.Models;
using RabbitMQApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// RabbitMQ Configuration
builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection("RabbitMQ"));

// Cargar configuración de MailSettings
builder.Configuration.AddJsonFile("mailsettings.json", optional: true, reloadOnChange: true);

// Registrar servicios
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();
builder.Services.AddSingleton<IEmailService, MailtrapEmailService>();
builder.Services.AddHostedService<EmailConsumerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();