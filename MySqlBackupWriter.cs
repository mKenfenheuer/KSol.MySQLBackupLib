using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSol.MySQLBackupLib
{
    public interface IMySqlBackupWriter
    {
        Task StartBackup(string filename);
        Task WriteBackupChunk(byte[] chunk);
        Task CommitBackupChunks();
        Task EndBackup();
        Task CancelBackup();
    }
}
