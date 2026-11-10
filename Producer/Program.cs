using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace LabRabbit
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Если передан аргумент "sync", работаем БЕЗ очереди
            bool isSyncMode = args.Length > 0 && args[0].ToLower() == "sync";

            if (isSyncMode)
            {
                Console.WriteLine("=== РЕЖИМ БЕЗ ОЧЕРЕДИ (СИНХРОННЫЙ) ===");
                var sw = Stopwatch.StartNew();

                // Имитация дорогой операции прямо в клиенте: запрос к базе CVE
                Console.WriteLine("[Синхронно] Выполняется прямой запрос к базе данных CVE для сервера 192.168.1.10...");
                await Task.Delay(3000);
                sw.Stop();

                Console.WriteLine($"[Синхронно] Задача выполнена. Клиент ждал: {sw.ElapsedMilliseconds} мс.");
                Console.WriteLine("Клиент был полностью заблокирован на все время выполнения дорогой операции.");
                return; // Завершаем работу
            }

            // ИНАЧЕ - РАБОТАЕМ ЧЕРЕЗ ОЧЕРЕДЬ (АСИНХРОННО)
            Console.WriteLine("=== РЕЖИМ С ОЧЕРЕДЬЮ (АСИНХРОННЫЙ) ===");
            const string QueueName = "vuln-scan-queue";
            string HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq";

            var factory = new ConnectionFactory { HostName = HostName };
            Console.WriteLine($"[Producer] Подключение к RabbitMQ ({HostName})...");

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            string[] serversToScan = new[]
            {
                "192.168.1.10",
                "192.168.1.15",
                "192.168.1.20",
                "10.10.0.5",
                "10.10.0.12"
            };

            Console.WriteLine($"[Producer] Отправка {serversToScan.Length} заявок на сканирование CVE в очередь '{QueueName}'...");
            var swTotal = Stopwatch.StartNew();

            for (int i = 0; i < serversToScan.Length; i++)
            {
                var swTask = Stopwatch.StartNew();
                var task = new TaskDto
                {
                    ServerIp = serversToScan[i],
                    Payload = $"{{\"server_ip\": \"{serversToScan[i]}\", \"action\": \"cve_scan\"}}"
                };

                var message = JsonSerializer.Serialize(task);
                var body = Encoding.UTF8.GetBytes(message);

                var properties = new BasicProperties
                {
                    DeliveryMode = DeliveryModes.Persistent
                };

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: QueueName,
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                );

                swTask.Stop();
                Console.WriteLine($"[Асинхронно] Заявка #{i + 1} (Сервер: {task.ServerIp}, TaskId: {task.TaskId}) отправлена в RabbitMQ за {swTask.ElapsedMilliseconds} мс.");
            }

            swTotal.Stop();
            Console.WriteLine($"[Асинхронно] Все {serversToScan.Length} заявок отправлены в RabbitMQ. Суммарное время клиента: {swTotal.ElapsedMilliseconds} мс.");
            Console.WriteLine("Клиент свободен и может выполнять другую работу, пока Consumer трудится.");
        }
    }
}
