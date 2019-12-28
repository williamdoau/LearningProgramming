using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LearningProgramming.HostedServices.Services
{
    public class ScopedHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScopedHostedService> _logger;

        public ScopedHostedService(IServiceProvider serviceProvider, ILogger<ScopedHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{nameof(ScopedHostedService)} is starting.");

            stoppingToken.Register(() => _logger.LogInformation($"{nameof(ScopedHostedService)} is stopping"));

            using (var scope = _serviceProvider.CreateScope())
            {
                var sendMessageService = scope.ServiceProvider.GetRequiredService<ISendMessageService>();

                await sendMessageService.SendMessage(stoppingToken);
            }
        }
    }

    public interface ISendMessageService
    {
        Task SendMessage(CancellationToken stoppingToken);
    }

    public class SendMessageService : ISendMessageService
    {
        private readonly ILogger _logger;
        private int _executionCount;

        public SendMessageService(ILogger<SendMessageService> logger)
        {
            _logger = logger;
            _executionCount = 0;
        }

        public async Task SendMessage(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _executionCount++;

                _logger.LogInformation($"{nameof(SendMessageService)} is sending message at {_executionCount}.");

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
