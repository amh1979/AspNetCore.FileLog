# AspNetCore.FileLog
Log information to file
support type: markdown,txt,database,system events
https://www.nuget.org/packages/AspNetCore.FileLog/

详细用法(v2.2.0.3): https://github.com/AspNetCoreFoundations/AspNetCore.FileLog

//now version v2.2.0.4

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddFileLog(logger =>
        {
            //default is UseText;
            logger.UseText();

            logger.UseMarkdown();
            logger.UseLogAdapter<DatabaseLogAdapter>();

            //default
            logger.SettingsPath = "/_Settings_";

            //default
            logger.LogRequestPath = "/_Logs_";

            //default
            logger.LogDirectory = ".Logs";
        });
    }
    public void Configure(IApplicationBuilder app)
    {
       // 
    }
