using System;

namespace MinecraftTunnel.Model
{
    public class UserModel
    {
        public int Id { get; set; }
        public string PlayerId { get; set; }
        public DateTime Create_at { get; set; }
        public DateTime End_at { get; set; }
    }
}
