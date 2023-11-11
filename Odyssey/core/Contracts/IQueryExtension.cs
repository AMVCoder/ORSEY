using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Odyssey.Core.core.Contracts
{
    public interface IQueryExtension<T>
    {
        IEnumerable<T?> ExecuteQuery(string sql, List<IDbDataParameter> parameters = null);
        T? ExecuteSingle(string sql);
        void ExecuteNonQuery(string sql, IEnumerable<PropertyInfo>? properties = null, object? entity = null);
    }
}
