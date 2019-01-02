# AspNetCore.FileLog
Log information to file for .net core
https://www.nuget.org/packages/AspNetCore.FileLog/

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddFileLog(".Logs");
        //services.AddFileLog("C:/wwwroot/.Logs");
    }
    public void Configure(IApplicationBuilder app)
    {
        app.UseFileLog("/_Logs_","/_Settings_");
    }
