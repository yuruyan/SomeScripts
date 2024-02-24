using Karambolo.Extensions.Logging.File;
using Microsoft.Extensions.Logging;

namespace SharedHelper;

public static class SharedLogging {
    public static readonly ILogger FileLogger;
    public static readonly ILogger Logger;

    static SharedLogging() {
        var factory = LoggerFactory.Create(builder => builder.AddConsole());
        var fileFactory = LoggerFactory.Create(
            builder => builder.AddConsole().AddFile(context => {
                context.RootPath = Path.GetDirectoryName(Environment.ProcessPath);
                context.Files = [new LogFileOptions() {
                    Path = "app.log",
                    MinLevel = new Dictionary<string, LogLevel> {
                        {"Default", LogLevel.Warning}
                    }
                }];
            })
        );
        Logger = factory.CreateLogger("Program");
        FileLogger = fileFactory.CreateLogger("Program");
    }
}
