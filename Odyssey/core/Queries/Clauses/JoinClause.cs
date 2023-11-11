using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odyssey.core.Queries
{
    public enum JoinType
    {
        Inner,
        Left,
        Right,
        Full
    }


    public class JoinClause
    {
        public JoinType JoinType { get; private set; }
        public string PrimaryTable { get; private set; }
        public string SecondaryTable { get; private set; }
        public string PrimaryKey { get; private set; }
        public string ForeignKey { get; private set; }


        public JoinClause SetJoinType(JoinType joinType)
        {
            this.JoinType = joinType;
            return this;
        }

        public JoinClause SetPrimaryTable(string primaryTable)
        {
            this.PrimaryTable = primaryTable;
            return this;
        }

        public JoinClause SetSecondaryTable(string secondaryTable)
        {
            this.SecondaryTable = secondaryTable;
            return this;
        }

        public JoinClause SetPrimaryKey(string primaryKey)
        {
            this.PrimaryKey = primaryKey;
            return this;
        }

        public JoinClause SetForeignKey(string foreignKey)
        {
            this.ForeignKey = foreignKey;
            return this;
        }


        internal string BuildJoinClause()
        {
            string joinKeyword;
            switch (JoinType)
            {
                case JoinType.Inner:
                    joinKeyword = "INNER JOIN";
                    break;
                case JoinType.Left:
                    joinKeyword = "LEFT JOIN";
                    break;
                case JoinType.Right:
                    joinKeyword = "RIGHT JOIN";
                    break;
                case JoinType.Full:
                    joinKeyword = "FULL JOIN";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return $"{joinKeyword} {SecondaryTable} ON {PrimaryTable}.{PrimaryKey} = {SecondaryTable}.{ForeignKey}";
        }
    }
}
