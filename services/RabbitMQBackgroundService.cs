namespace Comms.Services
{
    public class RabbitMQBackgroundService : BackgroundService
    {
        private readonly IRabbitMQService _rabbitMQService;
        private readonly ILogger<RabbitMQBackgroundService> _logger;

        public RabbitMQBackgroundService(IRabbitMQService rabbitMQService, ILogger<RabbitMQBackgroundService> logger)
        {
            _rabbitMQService = rabbitMQService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RabbitMQ Background Service starting...");

            // Start listening to RabbitMQ queues
            _rabbitMQService.StartListening();

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RabbitMQ Background Service stopping...");
            
            _rabbitMQService.StopListening();
            
            await base.StopAsync(stoppingToken);
        }
    }
} 