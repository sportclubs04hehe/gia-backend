using Npgsql;
using server.Repository.Implement;
using server.Repository.UnitOfWork;
using server.Service;
using server.Service.Impl;
using System.Data;
using System.Text;

namespace server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            Console.OutputEncoding = Encoding.GetEncoding("UTF-8");

            // Add services to the container.
            builder.Services.AddControllers();

            // Cấu hình CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngularApp",
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:4200")
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
            });

            // Cấu hình AutoMapper đơn giản
            builder.Services.AddAutoMapper(typeof(Program));

            // Cấu hình Database Connection
            builder.Services.AddScoped<IDbConnection>(sp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                return new NpgsqlConnection(connectionString);
            });

            // Đăng ký Repository và Service
            builder.Services.AddScoped<IDm_DonViTinhService, Dm_DonViTinhService>();
            builder.Services.AddScoped<IDm_HangHoaThiTruongService, Dm_HangHoaThiTruongService>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Sử dụng CORS
            app.UseCors("AllowAngularApp");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}