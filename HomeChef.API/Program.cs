using HomeChef.Application.Auth.Interfaces;
using HomeChef.Application.Auth.Services;
using HomeChef.Data;
using HomeChef.Data.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace HomeChef.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();



            //---------------------------------------------------------------
            //=========================================================
            // 1. Database - Entity Framework Core
            //=========================================================
            var defaultConnection = builder.Configuration.GetConnectionString("Default");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(defaultConnection));

            //=========================================================
            // 2. Identity - User Management
            //    Registers Identity services with ApplicationUser and IdentityRole,
            //    stored in ApplicationDbContext via Entity Framework.
            //    AddDefaultTokenProviders() enables token generation for:
            //    - Email confirmation
            //    - Password reset
            //    - Two-factor authentication
            //=========================================================
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Identity configuration - Password rules, lockout, and email confirmation
            builder.Services.Configure<IdentityOptions>(options =>
            {
                // Password requirements
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;

                // Lockout settings - 3 failed attempts → 20-minute lockout
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(20);

                // Require email/phone number confirmation before sign-in
                //options.SignIn.RequireConfirmedEmail = true;
                options.SignIn.RequireConfirmedPhoneNumber = true;

                // Token provider used for email confirmation tokens
                options.Tokens.EmailConfirmationTokenProvider = "Default";
            });

            //=========================================================
            // 3. JWT Authentication
            //    Validates tokens using Issuer, Audience, and SecretKey
            //    from appsettings.json
            //=========================================================
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
                };

                // ===== ADDED: Validate security stamp on each request =====
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userManager = context.HttpContext.RequestServices
                            .GetRequiredService<UserManager<ApplicationUser>>();

                        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                        if (string.IsNullOrEmpty(userId))
                        {
                            context.Fail("Invalid token: No user identifier found");
                            return;
                        }

                        // Get the security stamp from the token
                        var tokenSecurityStamp = context.Principal?.FindFirstValue("AspNet.Identity.SecurityStamp");

                        if (string.IsNullOrEmpty(tokenSecurityStamp))
                        {
                            context.Fail("Invalid token: No security stamp found");
                            return;
                        }

                        // Get the current security stamp from the database
                        var user = await userManager.FindByIdAsync(userId);

                        if (user == null)
                        {
                            context.Fail("User not found");
                            return;
                        }

                        // Compare stamps - if they don't match, token is invalid
                        if (tokenSecurityStamp != user.SecurityStamp)
                        {
                            context.Fail("Token has been revoked");
                            return;
                        }
                    }
                };

            });

            builder.Services.AddAuthorization();

            //=========================================================
            // 4. Rate Limiting
            //    - PublicPerIp: 4 requests/minute per IP (unauthenticated)
            //    - PerUser: 4 requests/minute per user (authenticated)
            //    429 Too Many Requests returned when limit exceeded
            //=========================================================
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Public endpoints - throttled by IP address
                options.AddPolicy("PublicPerIp", httpContext =>
                {
                    var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ip,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 4,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0 // No queueing - excess requests immediately rejected
                        });
                });

                // Authenticated endpoints - throttled by user identity
                options.AddPolicy("PerUser", httpContext =>
                {
                    var userId =
                        httpContext.User?.Identity?.IsAuthenticated == true
                            ? httpContext.User.Identity.Name!
                            : "anonymous";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: userId,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 4,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });
            });
            //---------------------------------------------------------------


            // Register Services
            builder.Services.AddScoped<IAuthService, AuthService>();




            builder.Services.AddSwaggerGen(); 

            var app = builder.Build();


            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();

            }


            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
