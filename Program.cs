using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Syncfusion.Licensing;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();

// Înregistrare licență Syncfusion din environment variable
var licenseKey = Environment.GetEnvironmentVariable("SyncfusionLicenseKey");
if (!string.IsNullOrEmpty(licenseKey) && licenseKey != "YOUR_SYNCFUSION_LICENSE_KEY_HERE")
{
    SyncfusionLicenseProvider.RegisterLicense(licenseKey);
}

host.Run();
