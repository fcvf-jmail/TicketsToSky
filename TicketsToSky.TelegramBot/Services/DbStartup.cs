using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketsToSky.TelegramBot.Data;

namespace TicketsToSky.TelegramBot.Services
{
    public static class DbStartup
    {
        public static void AddUserStateDb(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<UserStateDbContext>(options =>options.UseSqlite(config.GetConnectionString("UserStateDb")));
        }
    }
}
