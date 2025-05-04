using FluentCMS.DataAccess.Abstractions;

namespace FluentCMS.DataAccess.EntityFramework.Tests.Infrastructure
{
    public class TestApplicationExecutionContext : IApplicationExecutionContext
    {
        public string Username { get; set; }
        public bool IsAuthenticated { get; set; } = true;
        public string Language { get; set; } = "en-US";
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public string TraceId { get; set; } = Guid.NewGuid().ToString();
        public string UniqueId { get; set; } = Guid.NewGuid().ToString();
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public string UserIp { get; set; } = "127.0.0.1";

        public TestApplicationExecutionContext(string username = "test-user")
        {
            Username = username;
        }
    }
}
