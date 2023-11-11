using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odyssey.src.core.Connection
{
    public interface IDatabaseConnection : IDisposable
    {
        void Open();
        void Close();
        bool IsOpen { get; }
        T ExecuteOnConnection<T>(Func<IDbConnection, T> func);
    }
}
