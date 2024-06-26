using System;
using System.Collections.Generic;
using System.Linq;
using EFCore.BulkExtensions.SqlAdapters.SQLite;
using Microsoft.Data.SqlClient;

namespace EFCore.BulkExtensions.Tests;

public class SqlQueryBuilderSqliteTests
{
    [Fact]
    public void MergeTableInsertOrUpdateWithoutOnConflictWithIdentityUpdateWhereSqlTest()
    {
        TableInfo tableInfo = GetTestTableInfo(bulkCopyOptions: SqlBulkCopyOptions.KeepIdentity);
        tableInfo.IdentityColumnName = "ItemId";
        string actual = SqlQueryBuilderSqlite.InsertIntoTable(tableInfo, OperationType.InsertOrUpdate);

        string expected = @"INSERT INTO [Item] ([ItemId], [Name]) " +
                          @"VALUES (@ItemId, @Name) " +
                          @"ON CONFLICT([ItemId]) DO UPDATE SET [ItemId] = @ItemId, [Name] = @Name " +
                          @"WHERE [ItemId] = @ItemId;";

        Assert.Equal(expected, actual);
    }
    
    [Fact]
    public void MergeTableInsertOrUpdateWithoutOnConflictWithoutIdentityUpdateWhereSqlTest()
    {
        TableInfo tableInfo = GetTestTableInfo();
        tableInfo.IdentityColumnName = "ItemId";
        string actual = SqlQueryBuilderSqlite.InsertIntoTable(tableInfo, OperationType.InsertOrUpdate);

        string expected = @"INSERT INTO [Item] ([Name]) " +
                          @"VALUES (@Name) " +
                          @"ON CONFLICT([ItemId]) DO UPDATE SET [Name] = @Name " +
                          @"WHERE [ItemId] = @ItemId;";

        Assert.Equal(expected, actual);
    }
    
    [Fact]
    public void MergeTableInsertOrUpdateWithOnConflictUpdateWhereSqlTest()
    {
        TableInfo tableInfo = GetTestTableInfo((existing, inserted) => $"{inserted}.ItemTimestamp > {existing}.ItemTimestamp", SqlBulkCopyOptions.KeepIdentity);
        tableInfo.IdentityColumnName = "ItemId";
        string actual = SqlQueryBuilderSqlite.InsertIntoTable(tableInfo, OperationType.InsertOrUpdate);

        string expected = @"INSERT INTO [Item] ([ItemId], [Name]) " +
                          @"VALUES (@ItemId, @Name) " +
                          @"ON CONFLICT([ItemId]) DO UPDATE SET [ItemId] = @ItemId, [Name] = @Name " +
                          @"WHERE [ItemId] = @ItemId AND excluded.ItemTimestamp > [Item].ItemTimestamp;";

        Assert.Equal(expected, actual);
    }
    
    private TableInfo GetTestTableInfo(
        Func<string, string, string>? onConflictUpdateWhereSql = null
        , SqlBulkCopyOptions? bulkCopyOptions = null)
    {
        var tableInfo = new TableInfo()
        {
            EscL = "[",
            EscR = "]",
            Schema = "dbo",
            TempSchema = "dbo",
            TableName = nameof(Item),
            TempTableName = nameof(Item) + "Temp1234",
            TempTableSufix = "Temp1234",
            PrimaryKeysPropertyColumnNameDict = new Dictionary<string, string> { { nameof(Item.ItemId), nameof(Item.ItemId) } },
            BulkConfig = new BulkConfig()
            {
                OnConflictUpdateWhereSql = onConflictUpdateWhereSql,
                SqlBulkCopyOptions = bulkCopyOptions ?? SqlBulkCopyOptions.Default
            }
        };
        const string nameText = nameof(Item.Name);

        tableInfo.PropertyColumnNamesDict.Add(tableInfo.PrimaryKeysPropertyColumnNameDict.Keys.First(), tableInfo.PrimaryKeysPropertyColumnNameDict.Values.First());
        tableInfo.PropertyColumnNamesDict.Add(nameText, nameText);
        //compare on all columns (default)
        tableInfo.PropertyColumnNamesCompareDict = tableInfo.PropertyColumnNamesDict;
        //update all columns (default)
        tableInfo.PropertyColumnNamesUpdateDict = tableInfo.PropertyColumnNamesDict;
        return tableInfo;
    }
}
