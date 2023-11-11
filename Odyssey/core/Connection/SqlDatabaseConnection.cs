using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Odyssey.src.core.Connection
{
    public class SqlDatabaseConnection : IDatabaseConnection
    {
        private readonly string _connectionString;
        private IDbConnection _connection;

        public SqlDatabaseConnection(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _connection = new SqlConnection(_connectionString);

            // Establecer cultura invariante para el hilo actual
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        }

        public void Open()
        {
            if (_connection.State == ConnectionState.Closed)
            {
                try
                {
                    _connection.Open();
                }
                catch (Exception ex)
                {
                    // Aquí puedes agregar un logger para registrar el error
                    throw new Exception("Error al abrir la conexión a la base de datos.", ex);
                }
            }
        }

        public void Close()
        {
            if (_connection.State != ConnectionState.Closed)
            {
                try
                {
                    _connection.Close();
                }
                catch (Exception ex)
                {
                    // Aquí puedes agregar un logger para registrar el error
                    throw new Exception("Error al cerrar la conexión a la base de datos.", ex);
                }
            }
        }

        public bool IsOpen => _connection.State == ConnectionState.Open;

        public void Dispose()
        {
            Close();
            _connection.Dispose();
        }

        public T ExecuteOnConnection<T>(Func<IDbConnection, T> func)
        {
            this.Open();
            T result = func(_connection);
            this.Close();
            return result;
        }

        // Métodos y propiedades de IDbConnection que deberías implementar
        // Puedes redirigirlos simplemente a _connection

        public IDbTransaction BeginTransaction()
        {
            return _connection.BeginTransaction();
        }
    }
}

