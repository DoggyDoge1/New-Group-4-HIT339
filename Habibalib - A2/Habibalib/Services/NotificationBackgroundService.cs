using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Habibalib.Data;

namespace Habibalib.Services
{
    // runs quietly in the background the whole time the app is running, and
    // checks for due-soon / overdue loans every couple minutes. this is what
    // makes the reminders "automated" instead of only happening when a staff
    // member happens to open a page
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider services;

        public NotificationBackgroundService(IServiceProvider serviceProvider)
        {
            services = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    NotificationHelper.RunDueDateSweep(db);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // app is shutting down, just stop the loop
                }
            }
        }
    }
}
