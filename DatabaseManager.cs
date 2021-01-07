using MinecraftTunnel.Model;
using MySql.Data.MySqlClient;
using System;

namespace MinecraftTunnel
{
    public class DatabaseManager
    {
        public DatabaseManager()
        {

        }
        public UserModel FindPlayer(string PlayerName)
        {
            UserModel userModel = null;
            using (MySqlConnection connection = CreateConnection())
            {
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "select * from MinecraftPlayer  where PlayerName = '" + PlayerName + "' ORDER BY MinecraftPlayer.End_at DESC LIMIT 1";
                    connection.Open();
                    MySqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        int.TryParse(reader["Id"].ToString(), out int Id);
                        DateTime.TryParse(reader["Create_at"].ToString(), out DateTime Create_at);
                        DateTime.TryParse(reader["End_at"].ToString(), out DateTime End_at);
                        userModel = new UserModel(Id, reader["PlayerName"].ToString(), Create_at, End_at);
                    }
                    command.Clone();
                }
                connection.Close();
            }
            return userModel;
        }
        private MySqlConnection CreateConnection()
        {
            MySqlConnection result = null;
            result = new MySqlConnection(Program.ConnectionString);
            return result;
        }
    }
}
