using LogisticsERP.Application;
using LogisticsERP.Infrastructure;
using LogisticsERP.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<ExportJobProcessor>();
builder.Services.AddHostedService<LogisticsERP.Worker.FleetExpiryNotificationWorker>();
builder.Services.AddHostedService<AccidentDeadlineNotificationWorker>();
builder.Services.AddHostedService<LogisticsERP.Worker.EmployeeExpiryNotificationWorker>();
builder.Services.AddHostedService<JahezReminderWorker>();

var host = builder.Build();
await host.RunAsync();
