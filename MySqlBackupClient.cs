using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace KSol.MySQLBackupLib
{
    public class MySqlBackupClient
    {
        public string Host { get; set; } = "localhost";
        public string Username { get; set; }
        public string Password { get; set; }
        public string ConnectionString => $"Server={Host};Uid={Username};Pwd={Password};";
        public string MySqlDumpBinPath { get; set; } = "mysqldump";

        public MySqlBackupClient(string host, string username, string password)
        {
            Host = host;
            Username = username;
            Password = password;
        }

        public MySqlBackupClient(string host, string username, string password, string mySqlDumpBinPath) : this(host, username, password)
        {
            MySqlDumpBinPath = mySqlDumpBinPath;
        }

        public async Task<string[]> GetSchemas()
        {
            MySqlConnection con = new MySqlConnection(ConnectionString);
            await con.OpenAsync();
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = "show databases;";
            var reader = await cmd.ExecuteReaderAsync();
            var schemas = new List<string>();
            while (reader.Read())
            {
                schemas.Add(reader.GetString(0));
            }
            await reader.CloseAsync();
            await con.CloseAsync();
            return schemas.ToArray();
        }

        public async Task BackupDatabase(string db, IMySqlBackupWriter writer)
        {
            var fileEnding = ".sql";
            var filename = $"{db}{fileEnding}";
            string commandArgs = "";

            if (Host != "localhost")
            {
                commandArgs += " -h" + Host;
            }

            commandArgs += " -u" + Username;

            if (Password != "")
            {
                commandArgs += " -p" + Password;
            }

            commandArgs += " --databases " + db;

            try
            {
                await writer.StartBackup($"{db}{fileEnding}");

                Process dump = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = MySqlDumpBinPath,
                        Arguments = commandArgs,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                dump.Start();

                ulong chunkCount = 0;

                char[] buffer = new char[4 * 1024 * 1024];
                while (!dump.StandardOutput.EndOfStream)
                {
                    var len = dump.StandardOutput.Read(buffer, 0, buffer.Length);
                    if (len > 0)
                    {
                        var bytes = dump.StandardOutput.CurrentEncoding.GetBytes(buffer, 0, len);
                        await writer.WriteBackupChunk( bytes);
                        chunkCount++;
                        if(chunkCount % 100 == 0)
                        {
                            await writer.CommitBackupChunks();
                        }
                    }
                }

                await writer.CommitBackupChunks();
                await writer.EndBackup();
            }
            catch (Exception ex)
            {
                await writer.CommitBackupChunks();
                await writer.CancelBackup();
                throw ex;
            }
        }
    }
}