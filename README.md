# AspNetCore.FileLog
https://www.nuget.org/packages/AspNetCore.FileLog/

ÏêÏ¸ÓÃ·¨: https://github.com/AspNetCoreFoundations/AspNetCore.FileLog

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
