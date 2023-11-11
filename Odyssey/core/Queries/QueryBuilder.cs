using Azure;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Odyssey.core.Queries
{
    internal class QueryBuilder
    {
        private readonly Dictionary<ExpressionType, string> _sqlOperationDictionary = new Dictionary<ExpressionType, string>
        {
            { ExpressionType.Equal, "=" },
            { ExpressionType.NotEqual, "<>" },
            { ExpressionType.GreaterThan, ">" },
            { ExpressionType.GreaterThanOrEqual, ">=" },
            { ExpressionType.LessThan, "<" },
            { ExpressionType.LessThanOrEqual, "<=" },
            { ExpressionType.AndAlso, "AND" },
            { ExpressionType.OrElse, "OR" },
            { ExpressionType.AddChecked, "+" },
            { ExpressionType.Subtract, "-" },
            { ExpressionType.SubtractChecked, "-" },
            { ExpressionType.Multiply, "*" },
            { ExpressionType.MultiplyChecked, "*" },
            { ExpressionType.Divide, "/" },
            { ExpressionType.Modulo, "%" },
            { ExpressionType.And, "&" }, // AND bit a bit
            { ExpressionType.Or, "|" }, // OR bit a bit
            { ExpressionType.ExclusiveOr, "^" }, // XOR bit a bit
            { ExpressionType.Negate, "-" },
            { ExpressionType.NegateChecked, "-" },
            { ExpressionType.UnaryPlus, "+" },
            { ExpressionType.Not, "NOT" }
        };

        public string BuildSelectQuery<T>(Expression<Func<T, object>> columnSelector = null,Expression<Func<T, bool>> wherePredicate = null,JoinClause joinClause = null)
        {

            string tableName = typeof(T).Name;
            string columns = columnSelector != null ? GetColumns(columnSelector) : "*";

            var sqlBuilder = new StringBuilder($"SELECT {columns} FROM [{tableName}]");


            if(joinClause != null)
            {
                string joinBuild = BuilJoin(joinClause);

                sqlBuilder.Append($"{joinBuild}");
            }

            if (wherePredicate != null)
            {
                string whereClause = GetWhereClause(wherePredicate);
                sqlBuilder.Append($" WHERE {whereClause}");
            }

            return sqlBuilder.ToString();
        }

        private string BuilJoin(JoinClause join)
        {
           return join.BuildJoinClause();
        }

        public (string SqlQuery, IEnumerable<PropertyInfo> Properties) BuildInsertQuery<T>(T entity)
        {
            var tableName = typeof(T).Name;
            var props = GetPropertiesWithoutKey<T>();

            string columns = string.Join(", ", props.Select(p => p.Name));
            string values = string.Join(", ", props.Select(p => $"@{p.Name}"));
            string sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

            return (sql, props);
        }

        public (string SqlQuery, IEnumerable<PropertyInfo> Properties) BuildUpdateQuery<T>(T entity)
        {
            var tableName = typeof(T).Name;
            var props = GetPropertiesWithoutKey<T>();
            var keyProp = GetKeyColumnName<T>();

            string setClause = string.Join(", ", props.Select(p => $"{p.Name} = @{p.Name}"));

            string sql = $"UPDATE {tableName} SET {setClause} WHERE {keyProp} = @{keyProp}";

            return (sql, props.Concat(new[] { GetKeyProperty<T>() }));
        }

        public (string SqlQuery, string Key) BuildDeleteQuery<T>(int id)
        {
            var keyColumnName = GetKeyColumnName<T>();
            var tableName = typeof(T).Name;
            var sql = $"DELETE FROM {tableName} WHERE {keyColumnName} = @id";
            return (sql, keyColumnName);
        }

        internal string GetWhereClause<T>(Expression<Func<T, bool>> wherePredicate)
        {
            return ProcessExpression(wherePredicate.Body);
        }

        internal string ProcessExpression(Expression expression)
        {

            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso:
                    var andExp = expression as BinaryExpression;
                    var leftAnd = ProcessExpression(andExp.Left);
                    var rightAnd = ProcessExpression(andExp.Right);
                    return $"({leftAnd} AND {rightAnd})";
                case ExpressionType.OrElse:
                    var orExp = expression as BinaryExpression;
                    var leftOr = ProcessExpression(orExp.Left);
                    var rightOr = ProcessExpression(orExp.Right);
                    return $"({leftOr} OR {rightOr})";
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    var binaryExpression = expression as BinaryExpression;
                    return ProcessBinaryExpression(binaryExpression);

                default:
                    throw new NotSupportedException($"Expression type {expression.NodeType} is not supported.");
            }
        }

        internal string ProcessBinaryExpression(BinaryExpression binaryExp)
        {
            string left = null;

            if (binaryExp.Left is MemberExpression leftMember)
            {
                left = ProcessMemberExpression(leftMember);
            }

            var right = ProcessRightSide(binaryExp.Right);
            var operation = GetSqlOperation(binaryExp.NodeType);

            return $"({left} {operation} {right})";
        }

        internal string ProcessMemberExpression(MemberExpression memberExpr)
        {
            if (memberExpr.Expression is ParameterExpression)
            {
                return memberExpr.Member.Name;
            }
            else if (memberExpr.Expression is MemberExpression)
            {
                var innerMember = ProcessMemberExpression((MemberExpression)memberExpr.Expression);
                return $"{innerMember}.{memberExpr.Member.Name}";
            }
            else
            {
                throw new NotSupportedException($"The member expression '{memberExpr}' is not supported.");
            }
        }

        internal object ProcessRightSide(Expression expression)
        {
            switch (expression)
            {
                case MemberExpression member:
                    if (member.Expression is ConstantExpression constantExpression)
                    {
                        // Este es un miembro de una variable capturada en una lambda.
                        var memberInfo = member.Member as FieldInfo;
                        var value = memberInfo.GetValue(constantExpression.Value);
                        if (value is string stringValue)
                        {
                            return $"'{stringValue.Replace("'", "''")}'";
                        }
                        else if (value is bool boolValue)
                        {
                            string sqlValue = boolValue ? "1" : "0";

                            return sqlValue;
                        }

                        return memberInfo.GetValue(constantExpression.Value);
                    }
                    else if (member.Expression is MemberExpression)
                    {
                        // Este es un miembro de una variable capturada en una lambda, pero la captura es más compleja (posiblemente una cadena de propiedades/variables)
                        var objectMember = Expression.Convert(member, typeof(object));
                        var getterLambda = Expression.Lambda<Func<object>>(objectMember);
                        var getter = getterLambda.Compile();
                        return getter();
                    }
                    else
                    {
                        // Es un parámetro de la consulta
                        return "@" + member.Member.Name;
                    }

                case ConstantExpression constant:

                    object valueConstant = constant.Value;

                    if (valueConstant is string stringValueCon)
                    {
                        return $"'{stringValueCon.Replace("'", "''")}'";
                    }
                    else if (valueConstant is bool boolValue)
                    {
                        string sqlValue = boolValue ? "1" : "0";

                        return sqlValue;
                    }

                    return valueConstant;

                default:
                    throw new NotSupportedException("Expression type not supported.");
            }
        }

        internal string GetColumns<T>(Expression<Func<T, object>> columnSelector)
        {
            var memberExpressions = GetMemberExpressions(columnSelector.Body);

            var columns = memberExpressions.Select(member => {
                return $"[{member.Member.Name}]";
            });

            return string.Join(", ", columns);
        }

        internal IEnumerable<MemberExpression> GetMemberExpressions(Expression body)
        {
            if (body is MemberExpression memberExpression)
            {
                return new List<MemberExpression> { memberExpression };
            }
            else if (body is NewExpression newExpression)
            {
                return newExpression.Arguments.Select(arg => arg as MemberExpression);
            }
            else if (body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression)
            {
                return new List<MemberExpression> { unaryExpression.Operand as MemberExpression };
            }
            else
            {
                throw new InvalidOperationException("The column selector expression is not valid.");
            }
        }
        internal string GetSqlOperation(ExpressionType nodeType)
        {
            return _sqlOperationDictionary.TryGetValue(nodeType, out var op) ? op : throw new NotSupportedException($"Operation {nodeType} is not supported.");
        }

        internal PropertyInfo GetKeyProperty<T>()
        {
            var keyProperty = typeof(T)
                .GetProperties()
                .FirstOrDefault(prop => Attribute.IsDefined(prop, typeof(KeyAttribute)));

            if (keyProperty == null)
            {
                throw new Exception($"No KeyAttribute defined for type {typeof(T).Name}");
            }

            return keyProperty;
        }

        internal IEnumerable<PropertyInfo> GetPropertiesWithoutKey<T>()
        {
            return typeof(T).GetProperties().Where(p => p.GetCustomAttribute<KeyAttribute>() == null);
        }


        internal string GetKeyColumnName<T>()
        {
            return GetKeyProperty<T>().Name;
        }
    }

}
