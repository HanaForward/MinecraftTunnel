using MySql.Data.MySqlClient;

namespace MinecraftTunnel
{
    public class DatabaseManager
    {
        public DatabaseManager()
        {


        }
        private MySqlConnection CreateConnection()
        {
            MySqlConnection result = null;
            result = new MySqlConnection(Program.ConnectionString);
            return result;
        }
    }
}
