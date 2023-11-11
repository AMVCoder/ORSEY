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

        public IEnumerable<T> Query<T>(Expression<Func<T, object>> column = null, Expression<Func<T, bool>> where = null,JoinClause join = null)
        {
            _BaseQuery = queryBuilder.BuildSelectQuery(column, where,join);

            IEnumerable<T> listQuery = Execute<T>();
            return listQuery;
        }

        public IEnumerable<T> Query<T>(Expression<Func<T, object>> column = null, Expression<Func<T, bool>> where = null, List<JoinClause> join = null)
        {
            _BaseQuery = queryBuilder.BuildSelectQuery(column, where, join);

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

        public void Delete<T>(int id)
        {
            var (queryDelete, keyColumnName) = queryBuilder.BuildDeleteQuery<T>(id);
            ExecuteNonQuery(queryDelete, new[] { typeof(T).GetProperty(keyColumnName) }, id);
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
            if (properties != null && entity != null)
            {
                foreach (var prop in properties)
                {
                    if (prop != null)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = $"@{prop.Name}";
                        parameter.Value = prop.GetValue(entity) ?? DBNull.Value;
                        command.Parameters.Add(parameter);
                    }
                }
            }
        }
    }
}
