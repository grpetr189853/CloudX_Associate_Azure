using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.eShopWeb.PublicApi;

public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.Run(async (context) =>
        {
            TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
            TelemetryClient telemetryClient = new TelemetryClient(configuration);
            try
            {
                throw new Exception("Cannot move further");
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
                throw new Exception();
            }
        });
    }
}
