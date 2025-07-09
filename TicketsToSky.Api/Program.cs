using Microsoft.EntityFrameworkCore;
using TicketsToSky.Api.Data;
using TicketsToSky.Api.Repositories;
using TicketsToSky.Api.Services;
using TicketsToSky.Api.Mappings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<SubscriptionsDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<ISubscriptionsService, SubscriptionsService>();
builder.Services.AddAutoMapper(typeof(SubscriptionMappingProfile));
builder.Services.AddScoped<IRabbitMQService, RabbitMQService>();

var app = builder.Build();

// Автоматическое применение миграций БД
using var scope = app.Services.CreateScope();
SubscriptionsDbContext dbContext = scope.ServiceProvider.GetRequiredService<SubscriptionsDbContext>();
await dbContext.Database.MigrateAsync();

app.MapControllers();
await app.RunAsync();
