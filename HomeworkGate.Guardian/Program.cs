using HomeworkGate.Guardian;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "HomeworkGateGuardian";
});
builder.Services.AddHostedService<GuardianService>();

var host = builder.Build();
host.Run();
