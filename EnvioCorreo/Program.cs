using EnvioCorreo.Data;
using EnvioCorreo.Models;
using EnvioCorreo.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// 1. Carga la configuración de mailsettings.json (se mantiene)
builder.Configuration.AddJsonFile(
    path: Path.Combine(builder.Environment.ContentRootPath, "Config", "mailsettings.json"),
    optional: false,
    reloadOnChange: true
);

// 2. IMPORTANTE: Hemos ELIMINADO la carga explícita de dbconnection.json.
//    Esto asegura que la aplicación busque la cadena de conexión:
//    a) En appsettings.json (si existe)
//    b) En las variables de entorno (Docker Compose) <--- ¡ESTA ES LA QUE USARÁ!
/*
builder.Configuration.AddJsonFile(
    path: Path.Combine(builder.Environment.ContentRootPath, "Config", "dbconnection.json"),
    optional: false,
    reloadOnChange: true
);
*/


// Registra la configuración de MailSettings
builder.Services.Configure<MailSettings>(
    builder.Configuration.GetSection("MailSettings"));

// Registra el servicio de correo
builder.Services.AddTransient<IEmailService, EmailService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Obtenemos la cadena de la configuración (que ahora usará la variable de entorno de Docker)
var connectionString = builder.Configuration.GetConnectionString("UniversidadConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();