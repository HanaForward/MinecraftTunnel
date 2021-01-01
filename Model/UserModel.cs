using System;

namespace MinecraftTunnel.Model
{
    public class UserModel
    {
        public int Id { get; set; }
        public string PlayerName { get; set; }
        public DateTime Create_at { get; set; }
        public DateTime End_at { get; set; }

        public UserModel(int Id, string PlayerName, DateTime Create_at, DateTime End_at)
        {
            this.Id = Id;
            this.PlayerName = PlayerName;
            this.Create_at = Create_at;
            this.End_at = End_at;
        }
    }
}
