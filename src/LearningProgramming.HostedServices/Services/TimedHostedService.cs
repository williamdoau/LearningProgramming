using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LearningProgramming.HostedServices.Services
{
    public class TimedHostedService : BackgroundService
    {
        private readonly ILogger<TimedHostedService> _logger;

        private int _executionCount;

        public TimedHostedService(ILogger<TimedHostedService> logger)
        {
            _logger = logger;
            _executionCount = 0;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{nameof(TimedHostedService)} is starting.");

            stoppingToken.Register(() => _logger.LogInformation($"{nameof(TimedHostedService)} is stopping"));

            while (!stoppingToken.IsCancellationRequested)
            {
                _executionCount += 1;

                _logger.LogInformation($"{nameof(TimedHostedService)} is doing background work at {_executionCount}.");

                await SendMessage(stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        private async Task SendMessage(CancellationToken stoppingToken)
        {
            await Task.CompletedTask;
        }
    }
}
