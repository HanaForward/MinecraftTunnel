using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftTunnel.Protocol
{
    public interface IProtocol
    {
        void Analyze(Block block);
    }
}
