using LoginService.Data;
using LoginService.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.KeyVault;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LoginService
{
    public class Startup
    {
        private readonly TelemetryClient _telemetryClient;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            string appInsightsKey = Configuration["ApplicationInsights:InstrumentationKey"];
            _telemetryClient = new TelemetryClient(new TelemetryConfiguration(appInsightsKey));
            _telemetryClient.TrackEvent("Login service started.");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string connectionString = Configuration.GetConnectionString("DefaultConnection");

            if (connectionString == "DataSource=app.db")
            {
                services.AddDbContext<ApplicationDbContext>((options) => options.UseSqlite(connectionString));
            }
            else
            {
                if (connectionString.Contains("<password>"))
                {
                    string sqlPassword = GetSecret("sql-password");
                    connectionString = connectionString.Replace("<password>", sqlPassword);
                }
                services.AddDbContext<ApplicationDbContext>((options) => options.UseSqlServer(connectionString));
            }

            _telemetryClient.TrackTrace($"connection string: '{connectionString}'"); // Need to debug secrets in K8s. TODO: remove before releasing to production.

            if (string.IsNullOrEmpty(Configuration["JwtKey"]))
            {
                try
                {
                    Configuration["JwtKey"] = GetSecret("token-sign-key");
                }
                catch(Exception x) // until secrets work end-to-end have plan B
                {
                    _telemetryClient.TrackException(x);

                    Configuration["JwtKey"] = Guid.NewGuid().ToString();
                }
            }

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", builder =>                
                    builder
                        .WithOrigins(Configuration["CorsOrigins"])
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                );
            });

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = false;

                // User settings
                options.User.RequireUniqueEmail = true;
            });

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); 

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

                })
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = Configuration["JwtIssuer"],
                        ValidAudience = Configuration["JwtAudience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JwtKey"])),
                        ClockSkew = TimeSpan.FromMinutes(5)
                    };
                });

            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var dbInitializer = new DbInitializer(serviceScope.ServiceProvider);
                dbInitializer.ApplyMigrationsAsync().Wait();
                dbInitializer.SeedDataAsync().Wait();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            app.UseMvc();
        }

        private string GetSecret(string secretName)
        {
            using (var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetTokenAsync)))
            {
                var sec = kv.GetSecretAsync($"{Configuration["SecretsVaultUrl"]}/secrets/{secretName}").Result;

                return sec.Value;
            }
        }

        private async Task<string> GetTokenAsync(string authority, string resource, string scope)
        {
            /* 
             * Azure Principle password must be stored in the configured AadPasswordFilePath. 
             * In production the file will be written by deployment (Hexadite) into 
             * /secrets/secrets/mt-aad-password
             * In dev, create the file with the password and point AadPasswordFilePath to it.
             */
            string aadPassword = await File.ReadAllTextAsync(Configuration["AadPasswordFilePath"]);

            var authContext = new AuthenticationContext(authority);
            var clientCred = new ClientCredential(Configuration["AadAppId"], aadPassword);
            var result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return result.AccessToken;
        }
    }
}
