using System;

namespace LabRabbit
{
    public class TaskDto
    {
        public Guid TaskId { get; set; } = Guid.NewGuid();
        public string ServerIp { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
