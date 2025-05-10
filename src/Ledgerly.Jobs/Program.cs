using Ledgerly.Application;
using Ledgerly.Infrastructure;
using Ledgerly.Jobs;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<RecurringSchedulePollingService>();

var host = builder.Build();
host.Run();
