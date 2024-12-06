using Impinj_Reader.Hubs;
using Impinj_Reader.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://172.16.10.31:92") // URL del frontend
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Necesario para SignalR
    });
});

// Agregar servicios al contenedor
builder.Services.AddControllers();
builder.Services.AddSingleton<ReaderService>();
builder.Services.AddSingleton<ReaderSettings>();

// Servicio SignalR
builder.Services.AddSignalR();

// Configuración adicional
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configurar el pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseHttpsRedirection();

// Usar CORS con la política definida
app.UseCors("AllowSpecificOrigins");

app.UseRouting();
app.UseAuthorization();

// Configuración de endpoints
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<MessageHub>("/message"); // Mapea el hub
    endpoints.MapControllers(); // Mapea los controladores
});

// Ruta por defecto al archivo index.html
app.MapFallbackToFile("index.html");

app.Run();
