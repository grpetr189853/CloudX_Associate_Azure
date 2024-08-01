using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using System;

namespace Microsoft.eShopWeb.PublicApi;

public class Global
{
    protected void Application_Start() 
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
    }
}
