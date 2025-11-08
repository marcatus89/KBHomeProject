using System;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace DoAnTotNghiep
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // DB: SQL Server (pooling)
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContextPool<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString), poolSize: 32);

            // Identity (EF stores) + Default UI (Razor pages for Identity)
            services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddDefaultUI();

            // Cookie config for Identity
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                // Adjust SameSite or Cookie settings here if needed (e.g. for cross-site)
            });

            // JWT configuration
            var jwtSection = Configuration.GetSection("Jwt");
            var jwtKey = jwtSection["Key"] ?? throw new Exception("Jwt:Key is missing");
            var key = Encoding.UTF8.GetBytes(jwtKey);

            // IMPORTANT: keep Identity cookie scheme as the default authentication scheme
            // so that the Identity UI and cookie-based login works.
            services.AddAuthentication(options =>
            {
                // Use Identity cookie authentication as the default for Authenticate/Challenge
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            // Add JWT as an *additional* authentication scheme (for API endpoints)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // For development use false; set true in production and use HTTPS + proper cert
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    // If using self-signed certs for SQL server connection, that's unrelated to JWT config.
                };
            });

            // Authorization policy that accepts either Cookie (Identity) or JWT
            services.AddAuthorization(options =>
            {
                options.AddPolicy("JwtOrCookie", policy =>
                {
                    policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                });
            });

            // Add controllers for API
            services.AddControllers();

            // Add Razor Pages and Blazor Server (UI)
            services.AddRazorPages();
            services.AddServerSideBlazor();

            // CORS
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });

            // Register application services (they should create scopes or use IDbContextFactory / context by scope)
            services.AddScoped<ToastService>();
            services.AddScoped<CartService>();
            services.AddScoped<OrderService>();
            services.AddScoped<DashboardService>();
            services.AddScoped<PurchaseOrderService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // Map API controllers
                endpoints.MapControllers();

                // Map Identity UI (Razor Pages), Blazor Hub and fallback to _Host page (Blazor Server)
                endpoints.MapRazorPages();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
