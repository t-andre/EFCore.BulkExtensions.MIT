﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace EFCore.BulkExtensions
{
    public static class SqlQueryBuilder
    {
        public static string CreateTableCopy(string existingTableName, string newTableName)
        {
            var q = $"SELECT TOP 0 Source.* INTO {newTableName} FROM {existingTableName} ";
            q += $"LEFT JOIN {existingTableName} AS Source ON 1 = 0;"; // removes Identity constrain and makes all columns nullable
            return q;
        }

        public static string SelectFromTable(string tableName, string orderByColumnName)
        {
            return $"Select * FROM {tableName} ORDER BY {orderByColumnName};";
        }

        public static string DropTable(string tableName)
        {
            return $"DROP TABLE {tableName};";
        }

        public static string SelectIsIdentity(string tableName, string idColumnName)
        {
            return $"select columnproperty(object_id('{tableName}'),'{idColumnName}','IsIdentity');";
        }

        public static string MergeTable(TableInfo tableInfo, OperationType operationType)
        {
            string destinationTable = tableInfo.FullTableName;
            string sourceTable = tableInfo.FullTempTableName;
            string primaryKey = tableInfo.PrimaryKey;
            List<string> columnsNames = tableInfo.PropertyColumnNamesDict.Values.ToList();
            List<string> nonIdentityColumnsNames = columnsNames.Where(a => a != primaryKey).ToList();
            List<string> insertColumnsNames = tableInfo.HasIdentity ? nonIdentityColumnsNames : columnsNames;

            var q = $"MERGE {destinationTable} WITH (HOLDLOCK) USING {sourceTable} " +
                    $"ON {destinationTable}.{primaryKey} = {sourceTable}.{primaryKey}";

            if (operationType == OperationType.Insert || operationType == OperationType.InsertOrUpdate)
            {
                q += $" WHEN NOT MATCHED THEN INSERT ({GetCommaSeparatedColumns(insertColumnsNames)})";
                q += $" VALUES ({GetCommaSeparatedColumns(insertColumnsNames)})";
            }
            if (operationType == OperationType.Update || operationType == OperationType.InsertOrUpdate)
            {
                q += $" WHEN MATCHED THEN UPDATE SET {GetCommaSeparatedColumns(nonIdentityColumnsNames, destinationTable, sourceTable)}";
            }
            if (operationType == OperationType.Delete)
            {
                q += " WHEN MATCHED THEN DELETE";
            }

            if (tableInfo.SetOutputIdentity)
            {
                q += $" OUTPUT INSERTED.* INTO dbo.{tableInfo.FullTempOutputTableName}";
            }

            return q + ";";
        }

        public static string GetCommaSeparatedColumns(List<string> columnsNames, string prefixTable = null, string equalsTable = null)
        {
            string commaSeparatedColumns = "";
            foreach (var columnName in columnsNames)
            {
                commaSeparatedColumns += prefixTable != null ? $"{prefixTable}.{columnName}" : columnName;
                commaSeparatedColumns += equalsTable != null ? $" = {equalsTable}.{columnName}" : "";
                commaSeparatedColumns += ",";
            }
            commaSeparatedColumns = commaSeparatedColumns.Remove(commaSeparatedColumns.Length - 1, 1); // removes last excess comma: ","
            return commaSeparatedColumns;
        }
    }
}
