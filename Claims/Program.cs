using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Claims;
using Claims.Auditing;
using Claims.Contracts;
using Claims.Persistence;
using Claims.Services;
using Claims.Validation;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Local development and tests run against containers. Everywhere else, configuration comes from the host.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddInMemoryCollection(await LocalInfrastructure.StartAsync());
}

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        x.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    });

builder.Services.AddDbContext<AuditContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetRequiredSection("ConnectionStrings:AuditDatabase").Value,
        sql => sql.EnableRetryOnFailure()));

// One client for the whole application: EF builds its internal services per client instance,
// so creating a client per DbContext makes requests fail after about 20 of them.
var mongoClient = new MongoClient(builder.Configuration.GetRequiredSection("ConnectionStrings:ClaimsDatabase").Value);
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
// In Azure the namespace is set and access goes through a managed identity; the emulator needs a connection string.
var serviceBusNamespace = builder.Configuration["ServiceBus:FullyQualifiedNamespace"];
builder.Services.AddSingleton(_ => serviceBusNamespace is null
    ? new ServiceBusClient(builder.Configuration.GetRequiredSection("ConnectionStrings:ServiceBus").Value)
    : new ServiceBusClient(serviceBusNamespace, new DefaultAzureCredential()));
builder.Services.AddSingleton(services => services.GetRequiredService<ServiceBusClient>().CreateSender(AuditConsumer.QueueName));
builder.Services.AddSingleton(services => services.GetRequiredService<ServiceBusClient>().CreateProcessor(AuditConsumer.QueueName));
builder.Services.AddHostedService<OutboxRelay>();
builder.Services.AddHostedService<AuditConsumer>();

builder.Services.AddHealthChecks();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml")));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();


app.UseSwagger();

app.UseSwaggerUI(options => // UseSwaggerUI is called only in Development.
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
    context.Database.Migrate();
}

app.Run();

public partial class Program { }
