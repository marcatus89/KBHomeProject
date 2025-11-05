using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DoAnTotNghiep.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // Tạo một scope để lấy các dịch vụ
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    
                    // Tự động áp dụng tất cả các migration chưa được thực thi.
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied successfully.");

                    // Thêm dữ liệu mẫu
                    SeedData.Initialize(context, logger);
                    await SeedIdentity.SeedRolesAndAdminAsync(services);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred during database initialization or seeding.");
                    throw; 
                }
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}