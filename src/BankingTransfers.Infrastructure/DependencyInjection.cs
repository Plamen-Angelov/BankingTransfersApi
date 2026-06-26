using BankingTransfers.Application.Interfaces;
using BankingTransfers.Infrastructure.BackgroundServices;
using BankingTransfers.Infrastructure.Data;
using BankingTransfers.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankingTransfers.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();

        services.AddHostedService<BackgroundTransferProcessor>();

        return services;
    }
}
