using Microsoft.Extensions.Configuration;
using System.Threading;

namespace MinecraftTunnel.Service
{
    public class SemaphoreService
    {
        public Semaphore Semaphore;                                    // 信号量 控制最大连接数

        public SemaphoreService(IConfiguration Configuration)
        {
            _ = ushort.TryParse(Configuration["MaxConnections"], out ushort MaxConnections);
            Semaphore = new Semaphore(MaxConnections, MaxConnections);
        }
    }
}
