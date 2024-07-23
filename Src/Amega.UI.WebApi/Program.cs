using Amega.UI.WebApi.Hubs;
using Amega.UI.WebApi.Infrastructure.WebSocketService;
using Amega.UI.WebApi.Model.AmegaSettings;
using Amega.UI.WebApi.Services.InstrumentProvider;

namespace Amega.UI.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var services = builder.Services;

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Trace);

            var amegaSettings = builder.Configuration.GetSection("AmegaSettings");
            var settings = amegaSettings.Get<AmegaSettings>();
            services.Configure<AmegaSettings>(amegaSettings);

            services.AddDistributedMemoryCache();
            services.AddControllers();

            services.AddSignalR(options =>
            {
                //TODO: Set the maximum message size and default StreamBufferCapacity
                //      according to the realtime usage.
            });

            foreach (var instrumentProviderSettings in settings.InstrumentProviderSettingsList)
            {
                services.AddWebSocketServices(instrumentProviderSettings);
            }
            services.AddTiingoInstrumentProvider();

            //NOTE: IInstrumentProvider object is Eagerly loaded,
            //      to start registering and listenning to the websocket.
            services.AddHostedService<SingletonEagerLoader>();
            services.AddSingleton<PriceUpdateService>();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.MapControllers();
            app.MapHub<TickerHub>("/priceHub");

            app.Run();
        }
    }
}