
using Microsoft.AspNetCore.Builder;
using PaymentApi.Data;
using Stripe;

namespace PaymentApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("PaymentLab",
                    policy =>
                    {
                        policy
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowAnyOrigin();
                    });
            });


            builder.Services.AddSwaggerGen();
            builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

            StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe")["SecretKey"];
            

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("PaymentLab");

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
