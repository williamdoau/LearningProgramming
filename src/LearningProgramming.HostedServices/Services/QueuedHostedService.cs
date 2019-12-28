using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LearningProgramming.HostedServices.Services
{
    public class QueuedHostedService : BackgroundService
    {
        private readonly MonitorLoop _monitorLoop;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ILogger<QueuedHostedService> _logger;

        public QueuedHostedService(MonitorLoop monitorLoop, IBackgroundTaskQueue taskQueue, ILogger<QueuedHostedService> logger)
        {
            _monitorLoop = monitorLoop;
            _taskQueue = taskQueue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"{nameof(QueuedHostedService)} is starting.{Environment.NewLine}{Environment.NewLine}" +
                $"Tap W to add a work item to the background queue.{Environment.NewLine}");

            stoppingToken.Register(() => _logger.LogInformation($"{nameof(QueuedHostedService)} is stopping"));

            _monitorLoop.StartMonitorLoop();

            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                try
                {
                    await workItem(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error occurred executing {nameof(workItem)}.");
                }
            }
        }
    }

    public interface IBackgroundTaskQueue
    {
        Task QueueAsync(Func<CancellationToken, Task> workItem);
        Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
    }

    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private ConcurrentQueue<Func<CancellationToken, Task>> _workItems;
        private SemaphoreSlim _signal;

        public BackgroundTaskQueue()
        {
            _workItems = new ConcurrentQueue<Func<CancellationToken, Task>>();
            _signal = new SemaphoreSlim(0);
        }

        public async Task QueueAsync(Func<CancellationToken, Task> workItem)
        {
            if (workItem == null)
                throw new ArgumentNullException(nameof(workItem));

            _workItems.Enqueue(workItem);
            _signal.Release();

            await Task.CompletedTask;
        }

        public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);

            return workItem;
        }
    }

    public class MonitorLoop
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ILogger _logger;
        private readonly CancellationToken _cancellationToken;

        public MonitorLoop(IBackgroundTaskQueue taskQueue, ILogger<MonitorLoop> logger, IHostApplicationLifetime applicationLifetime)
        {
            _taskQueue = taskQueue;
            _logger = logger;
            _cancellationToken = applicationLifetime.ApplicationStopping;
        }

        public void StartMonitorLoop()
        {
            _logger.LogInformation($"{nameof(MonitorLoop)} is starting");

            Task.Run(async () => await Monitor());
        }

        public async Task Monitor()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                await _taskQueue.QueueAsync(async cancellationToken =>
                {
                    // Simulate a 1 second task to complete for each enqueued work item
                    var delayLoop = 0;
                    var guid = Guid.NewGuid().ToString();

                    _logger.LogInformation($"WorkItem {guid} is starting.");

                    while (!cancellationToken.IsCancellationRequested && delayLoop < 3)
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                        }

                        delayLoop++;

                        _logger.LogInformation($"WorkItem {guid} is running. {delayLoop}/3");
                    }

                    if (delayLoop == 3)
                    {
                        _logger.LogInformation($"WorkItem {guid} is complete.");
                    }
                    else
                    {
                        _logger.LogInformation($"WorkItem {guid} was cancelled.");
                    }
                });

                await Task.Delay(TimeSpan.FromSeconds(5), _cancellationToken);
            }
        }
    }
}
