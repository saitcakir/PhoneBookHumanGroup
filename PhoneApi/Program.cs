using AutoMapper.Extensions.ExpressionMapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PhoneBookHumanGroupBL.ImplementationsOfManagers;
using PhoneBookHumanGroupBL.InterfacesOfManagers;
using PhoneBookHumanGroupDL.ContextInfo;
using PhoneBookHumanGroupDL.ImplementationofRepos;
using PhoneBookHumanGroupDL.InterfacesofRepos;
using PhoneBookHumanGroupEL.Mappings;

namespace PhoneApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<PhoneBookHumanGroupContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("MyLocal"));
            });
            builder.Services.AddAutoMapper(x =>
            {
                x.AddExpressionMapping();
                x.AddProfile<Maps>();

            });
            builder.Services.AddControllers();
            builder.Services.AddScoped<IMemberPhoneRepo, MemberPhoneRepo>();
            builder.Services.AddScoped<IMemberPhoneManager, MemberPhoneManager>();

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

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}