using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LabRabbit
{
    class Program
    {
        static async Task Main(string[] args)
        {
            const string QueueName = "vuln-scan-queue";
            string HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq";

            Console.WriteLine($"[Consumer] Подключение к RabbitMQ ({HostName})...");
            var factory = new ConnectionFactory { HostName = HostName };

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            // Объявляем ту же очередь
            await channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            // prefetchCount: 1 — брокер не даст консьюмеру новое сообщение,
            // пока он не подтвердит (ACK) текущее. Это обеспечивает
            // последовательную обработку по одному сообщению.
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(channel);

            // Обработчик входящих сообщений
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var task = JsonSerializer.Deserialize<TaskDto>(message);

                if (task == null) return;

                Console.WriteLine($"[Consumer] Получена заявка на сканирование CVE сервера: {task.ServerIp} (ID: {task.TaskId}). Начинаю обработку (3 сек)...");

                // Искусственная задержка (имитация дорогой операции: обращение к БД CVE)
                await Task.Delay(3000);

                // Создание объектов предметной области (Вариант 9: Управление уязвимостями)
                var detected = new VulnerabilityDetectedEvent("CVE-2024-38077", DateTime.UtcNow, "CRITICAL");
                var patched = new VulnerabilityPatchedEvent("CVE-2023-36884", DateTime.UtcNow, "KB5040442");

                Console.WriteLine($"[Consumer] Результаты анализа {task.ServerIp}:");
                Console.WriteLine($"           - {detected.GetDescription()}");
                Console.WriteLine($"           - {patched.GetDescription()}");
                Console.WriteLine($"[Consumer] Сервер {task.ServerIp} успешно проверен. Отправка ACK (DeliveryTag: {ea.DeliveryTag}).");

                // Ручное подтверждение
                // Если консьюмер упадет ДО этой строки, RabbitMQ вернет сообщение в очередь.
                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            // Запускаем прослушивание.
            // autoAck: false — КРИТИЧЕСКИ ВАЖНО. Отключает автоматическое подтверждение.
            await channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer
            );

            Console.WriteLine($"[Consumer] Ожидание заявок в очереди '{QueueName}'. Нажмите Ctrl+C для выхода.");

            // Блокируем главный поток, чтобы приложение не завершилось
            await Task.Delay(Timeout.Infinite);
        }
    }
}
