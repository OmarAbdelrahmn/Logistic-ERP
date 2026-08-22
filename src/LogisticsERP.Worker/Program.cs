using LogisticsERP.Application;
using LogisticsERP.Infrastructure;
using LogisticsERP.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<ExportJobProcessor>();
builder.Services.AddHostedService<LogisticsERP.Worker.FleetExpiryNotificationWorker>();

var host = builder.Build();
await host.RunAsync();
