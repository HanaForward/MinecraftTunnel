using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftTunnel.Protocol
{
    public class ZipProtocol
    {

        public Block block;

        public int PacketLength;        // 数据包中剩余的字节数，包括数据长度字段
        public int DataLength;          // 如果长度为0则标识当前数据包未压缩
        public int PacketID;            // zlib压缩的数据包Id
        public byte[] Data;             // zlib压缩的荷载数据

        public ZipProtocol() { }

        public void Analyze(Block block)
        {
            this.block = block;
            this.PacketLength = block.readVarInt();
            this.DataLength = block.readVarInt();
            this.PacketID = block.readVarInt();

            this.Data = block.Remaini();
        }

    }
}
