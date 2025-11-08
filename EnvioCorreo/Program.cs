using EnvioCorreo.Data;
using EnvioCorreo.Models;
using EnvioCorreo.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // Necesario
using Microsoft.Extensions.Hosting; // Necesario

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    path: Path.Combine(builder.Environment.ContentRootPath, "Config", "mailsettings.json"),
    optional: false, // El archivo es obligatorio. Si no está, fallará el inicio.
    reloadOnChange: true // Permite que la configuración se actualice si el archivo cambia.
);

// 2. ?? Carga la nueva configuración de la Cadena de Conexión
builder.Configuration.AddJsonFile(
    path: Path.Combine(builder.Environment.ContentRootPath, "Config", "dbconnection.json"),
    optional: false, // Hacemos que la conexión sea obligatoria
    reloadOnChange: true
);

// Registra la configuración de MailSettings
builder.Services.Configure<MailSettings>(
    builder.Configuration.GetSection("MailSettings"));

// Registra el servicio de correo
builder.Services.AddTransient<IEmailService, EmailService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

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
