using ScrapingBackgroundService_IZYTimeControl;
using Microsoft.Extensions.Hosting.WindowsServices;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ScrapingBackgroundService_IZYTimeControl";
});


var host = builder.Build();
host.Run();