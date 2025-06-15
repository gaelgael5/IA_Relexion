
using NLog;
using NLog.Extensions.Logging;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace Bb.Logging.NLog
{


    public class NLogInitializer 
    {     
   
        public static void Execute()
        {

            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {

                var config = Loggers.InitializeLogger();
                if (config != null)
                {
                    builder.ClearProviders();
                    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
                    builder.AddNLog(config);
                }

            });

            StaticContainer.Set(loggerFactory);

            Trace.Listeners.Add(new NLogTraceListener());

        }

    }

}
