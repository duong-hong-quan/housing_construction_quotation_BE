﻿using Hangfire;
using HCQS.BackEnd.Service.Implementations;

namespace HCQS.BackEnd.API.Installers
{
    public class HangfireInstaller : IInstaller
    {
        public void InstallService(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHangfire(x => x.UseSqlServerStorage(configuration["ConnectionStrings:Host"]));
            services.AddHangfireServer();
            services.AddScoped<WorkerService>();
        }
    }
}