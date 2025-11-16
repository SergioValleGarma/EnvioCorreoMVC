namespace RabbitMQApiService.Models
{
    public class RabbitMQOptions
    {
        public string HostName { get; set; } = "localhost";
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string QueueName { get; set; } = "email_sent_queue";
        public int Port { get; set; } = 5672;
    }
}