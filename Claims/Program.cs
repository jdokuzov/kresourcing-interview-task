using Azure.Messaging.ServiceBus;
using Claims.Auditing;
using Claims.Contracts;
using Claims.Persistence;
using Claims.Services;
using Claims.Validation;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;
using Testcontainers.ServiceBus;

var builder = WebApplication.CreateBuilder(args);

// Start Testcontainers for SQL Server, MongoDB and the Service Bus emulator
var sqlContainer = new MsSqlBuilder(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        ? "mcr.microsoft.com/mssql/server:2022-latest"
        : "mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04")
    .Build();

// A replica set, because MongoDB transactions require one and the outbox relies on them.
var mongoContainer = new MongoDbBuilder("mongo:latest")
    .WithReplicaSet()
    .Build();

const string auditQueue = "audit-events"; // Declared in servicebus-emulator.json
var serviceBusContainer = new ServiceBusBuilder("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
    .WithAcceptLicenseAgreement(true)
    .WithConfig(Path.Combine(AppContext.BaseDirectory, "servicebus-emulator.json"))
    .Build();

await Task.WhenAll(sqlContainer.StartAsync(), mongoContainer.StartAsync(), serviceBusContainer.StartAsync());

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        x.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    });

builder.Services.AddDbContext<AuditContext>(options =>
    options.UseSqlServer(sqlContainer.GetConnectionString()));

// One client for the whole application: EF builds its internal services per client instance,
// so creating a client per DbContext makes requests fail after about 20 of them.
var mongoClient = new MongoClient(mongoContainer.GetConnectionString());
builder.Services.AddDbContext<ClaimsContext>(options =>
    options.UseMongoDB(mongoClient, builder.Configuration["MongoDb:DatabaseName"]!));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork>(services => services.GetRequiredService<ClaimsContext>());
builder.Services.AddScoped<IClaimsService, ClaimsService>();
builder.Services.AddScoped<ICoversService, CoversService>();
builder.Services.AddSingleton<IPremiumCalculator, PremiumCalculator>();

builder.Services.AddScoped<IValidator<CreateClaimRequest>, CreateClaimValidator>();
builder.Services.AddSingleton<IValidator<CreateCoverRequest>, CreateCoverValidator>();
builder.Services.AddExceptionHandler<RequestValidationExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<IAuditer, Auditer>();
builder.Services.AddScoped<AuditWriter>();
builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusContainer.GetConnectionString()));
builder.Services.AddSingleton(services => services.GetRequiredService<ServiceBusClient>().CreateSender(auditQueue));
builder.Services.AddSingleton(services => services.GetRequiredService<ServiceBusClient>().CreateProcessor(auditQueue));
builder.Services.AddHostedService<OutboxRelay>();
builder.Services.AddHostedService<AuditConsumer>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml")));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

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
