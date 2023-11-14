using Odyssey.core.Queries;
using Odyssey.src.core.Connection;
using Odyssey.src.core.Mappers;
using System;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Odyssey.src.core.Queries
{
    public class QueryExecutor
    {
        private readonly IDatabaseConnection _dbConnection;

        private string _BaseQuery;
        private QueryBuilder queryBuilder { get; } = new QueryBuilder();
       
        public QueryExecutor(IDatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public IEnumerable<T> Query<T>(Expression<Func<T, object>> column = null, Expression<Func<T, bool>> where = null)
        {
            _BaseQuery = queryBuilder.BuildSelectQuery(column, where);

            IEnumerable<T> listQuery = Execute<T>();
            return listQuery;
        }

        public void Insert<T>(T entity)
        {
            var (queryInsert, props) = queryBuilder.BuildInsertQuery(entity);
            ExecuteNonQuery(queryInsert, props, entity);
        }

        public void Update<T>(T entity)
        {
            var (queryUpdate, props) = queryBuilder.BuildUpdateQuery(entity);
            ExecuteNonQuery(queryUpdate, props, entity);
        }

        public void Delete<T>(int id) where T : class
        {
            var (queryDelete, keyColumnName) = queryBuilder.BuildDeleteQuery<T>(id);
            var properties = typeof(T).GetProperties().Where(p => p.Name == keyColumnName);
            ExecuteNonQuery(queryDelete, properties, id);
        }

        private IEnumerable<T> Execute<T>()
        {

            if (string.IsNullOrEmpty(_BaseQuery))
            {
                throw new InvalidOperationException("The query has not been initialized. Call Select first.");
            }

            return _dbConnection.ExecuteOnConnection(connection =>
            {
                var entities = new List<T>();

                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = _BaseQuery;

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            T entity = EntityMapper.Map<T>(reader);
                            entities.Add(entity);
                        }
                    }
                }

                return entities;
            });
        }
        private void ExecuteNonQuery(string sql, IEnumerable<PropertyInfo>? properties, object? entity)
        {
            _dbConnection.ExecuteOnConnection(connection =>
            {
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    AddParametersToCommand(command, properties, entity);
                    command.ExecuteNonQuery();
                }
                return 0;
            });
        }
        private void AddParametersToCommand(IDbCommand command, IEnumerable<PropertyInfo>? properties, object? entity)
        {
            if (properties == null || entity == null)
            {
                throw new ArgumentNullException("Los parámetros 'properties' y 'entity' no pueden ser nulos.");
            }

            if (properties.Count() == 1)
            {
                var prop = properties.First();
                if (prop != null)
                {
                    queryBuilder.AddDeleteParameter(command, prop, entity);
                }
            }
            else
            {
                foreach (var prop in properties)
                {
                    if (prop != null)
                    {
                        queryBuilder.AddUpdateOrInsertParameter(command, prop, entity);
                    }
                }
            }
        }

       


    }
}
