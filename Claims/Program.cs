using Claims.Auditing;
using Claims.Persistence;
using Claims.Services;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;

var builder = WebApplication.CreateBuilder(args);

// Start Testcontainers for SQL Server and MongoDB
var sqlContainer = new MsSqlBuilder(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        ? "mcr.microsoft.com/mssql/server:2022-latest"
        : "mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04")
    .Build();

var mongoContainer = new MongoDbBuilder("mongo:latest")
    .Build();

await sqlContainer.StartAsync();
await mongoContainer.StartAsync();

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<AuditContext>(options =>
    options.UseSqlServer(sqlContainer.GetConnectionString()));

// One client for the whole application: EF builds its internal services per client instance,
// so creating a client per DbContext makes requests fail after about 20 of them.
var mongoClient = new MongoClient(mongoContainer.GetConnectionString());
builder.Services.AddDbContext<ClaimsContext>(options =>
    options.UseMongoDB(mongoClient, builder.Configuration["MongoDb:DatabaseName"]!));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IClaimsService, ClaimsService>();
builder.Services.AddScoped<ICoversService, CoversService>();
builder.Services.AddSingleton<IPremiumCalculator, PremiumCalculator>();

builder.Services.AddScoped<IAuditer, Auditer>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml")));

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

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
    context.Database.Migrate();
}

app.Run();

public partial class Program { }
