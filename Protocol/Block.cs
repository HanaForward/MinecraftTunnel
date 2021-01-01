using System;
using System.IO;
using System.Text;

namespace MinecraftTunnel.Protocol
{
    public class Block : IDisposable
    {
        public MemoryStream stream;
        public byte[] buffer;
        public int step;


        public Block()
        {
            stream = new MemoryStream();
        }
        public Block(int Position)
        {
            stream = new MemoryStream();
            stream.Position = Position;
        }
        public Block(byte[] buffer)
        {
            this.buffer = buffer;
            step = 0;
        }
        public Block(byte[] buffer,int step)
        {
            this.buffer = buffer;
            this.step = step;
        }
        public byte readByte()
        {
            if (step >= buffer.Length)
                return 0;
            return buffer[step++];
        }
        public int readVarInt()
        {
            int numRead = 0;
            int result = 0;
            byte read;
            do
            {
                read = readByte();
                int value = read & 0b01111111;
                result |= (value << (7 * numRead));

                numRead++;
                if (numRead > 5)
                {
                    throw new Exception("VarInt is too big");
                }
            } while ((read & 0b10000000) != 0);

            return result;
        }
        public long readVarLong()
        {
            int numRead = 0;
            long result = 0;
            byte read;
            do
            {
                read = readByte();
                long value = (read & 0b01111111);
                result |= (value << (7 * numRead));

                numRead++;
                if (numRead > 10)
                {
                    throw new Exception("VarLong is too big");
                }
            } while ((read & 0b10000000) != 0);

            return result;
        }
        public ushort readShort()
        {
            byte b = readByte();
            return (ushort)((b << 8) + (readByte() & 0xFF));
        }
        public long readLong()
        {
            byte[] temp = new byte[8];
            Array.Copy(buffer, step, temp, 0, 8);
            step += 8;
            if (BitConverter.IsLittleEndian)
                Array.Reverse(temp);
            return BitConverter.ToInt64(temp, 0);
        }

        public byte[] GetBytes()
        {
            return stream.ToArray();
        }
        public string readString(int Length)
        {
            if (step - Length >= buffer.Length)
                return null;
            byte[] arrayOfByte = new byte[Length];
            Array.Copy(buffer, step, arrayOfByte, 0, Length);
            step = step + Length;
            return Encoding.UTF8.GetString(arrayOfByte);
        }
        public void WriteInt(int value)
        {
            do
            {
                byte b = (byte)(value & 0x7F);
                value >>= 7;
                if (value != 0)
                    b = (byte)(b | 0x80);
                stream.WriteByte(b);
            } while (value != 0);
        }
        public void WriteUShort(ushort value)
        {
            stream.WriteByte((byte)(value >> 8 & 0xFF));
            stream.WriteByte((byte)(value & 0xFF));
        }
        public void WriteShort(short value)
        {
            stream.WriteByte((byte)(value >> 8 & 0xFF));
            stream.WriteByte((byte)(value & 0xFF));
        }
        public void WriteString(string data, bool longString)
        {
            byte[] arrayOfByte = Encoding.UTF8.GetBytes(data);
            int Length = arrayOfByte.Length;
            if (longString)
                WriteInt(Length);
            stream.Write(arrayOfByte, 0, arrayOfByte.Length);
        }
        public void WriteLong(long value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            stream.Write(bytes);
        }



        [Obsolete]
        public void SetSize(int size)
        {
            stream.Position = 0;
            WriteInt(size);
        }
        public void Dispose()
        {
            if (stream != null)
                stream.Dispose();
            buffer = null;
        }

    }
}
