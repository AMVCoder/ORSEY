using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odyssey.src.core.Mappers
{
    public static class EntityMapper
    {
        public static T Map<T>(IDataRecord record)
        {
            T entity = (T)Activator.CreateInstance(typeof(T), nonPublic: true); // nonPublic: true permite crear instancias con constructores no públicos

            foreach (var prop in typeof(T).GetProperties())
            {
                try
                {
                    int ordinal = record.GetOrdinal(prop.Name);
                    if (!record.IsDBNull(ordinal))
                    {
                        var value = Convert.ChangeType(record[ordinal], prop.PropertyType);
                        prop.SetValue(entity, value);
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    // Column does not exist in the result set, you can handle it or ignore it
                }
            }

            return entity;
        }
    }
}
