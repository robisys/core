﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Bit.Core.Models.Table;
using System.Data;
using Dapper;
using Core.Models.Data;
using Bit.Core.Utilities;
using Newtonsoft.Json;

namespace Bit.Core.Repositories.SqlServer
{
    public class CipherRepository : Repository<Cipher, Guid>, ICipherRepository
    {
        public CipherRepository(GlobalSettings globalSettings)
            : this(globalSettings.SqlServer.ConnectionString)
        { }

        public CipherRepository(string connectionString)
            : base(connectionString)
        { }

        public async Task<CipherDetails> GetByIdAsync(Guid id, Guid userId)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<CipherDetails>(
                    $"[{Schema}].[CipherDetails_ReadByIdUserId]",
                    new { Id = id, UserId = userId },
                    commandType: CommandType.StoredProcedure);

                return results.FirstOrDefault();
            }
        }

        public async Task<bool> GetCanEditByIdAsync(Guid userId, Guid cipherId)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var result = await connection.QueryFirstOrDefaultAsync<bool>(
                    $"[{Schema}].[Cipher_ReadCanEditByIdUserId]",
                    new { UserId = userId, Id = cipherId },
                    commandType: CommandType.StoredProcedure);

                return result;
            }
        }

        public async Task<ICollection<CipherDetails>> GetManyByUserIdAsync(Guid userId)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<CipherDetails>(
                    $"[{Schema}].[CipherDetails_ReadByUserId]",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure);

                // Return distinct Id results. If at least one of the grouped results allows edit, that we return it.
                return results
                    .GroupBy(c => c.Id)
                    .Select(g => g.OrderByDescending(og => og.Edit).First())
                    .ToList();
            }
        }

        public async Task<ICollection<CipherDetails>> GetManyByUserIdHasCollectionsAsync(Guid userId)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<CipherDetails>(
                    $"[{Schema}].[CipherDetails_ReadByUserIdHasCollection]",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure);

                // Return distinct Id results. If at least one of the grouped results allows edit, that we return it.
                return results
                    .GroupBy(c => c.Id)
                    .Select(g => g.OrderByDescending(og => og.Edit).First())
                    .ToList();
            }
        }

        public async Task<ICollection<Cipher>> GetManyByOrganizationIdAsync(Guid organizationId)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<Cipher>(
                    $"[{Schema}].[Cipher_ReadByOrganizationId]",
                    new { OrganizationId = organizationId },
                    commandType: CommandType.StoredProcedure);

                return results.ToList();
            }
        }

        public async Task<ICollection<CipherDetails>> GetManyByTypeAndUserIdAsync(Enums.CipherType type, Guid userId)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<CipherDetails>(
                    $"[{Schema}].[CipherDetails_ReadByTypeUserId]",
                    new
                    {
                        Type = type,
                        UserId = userId
                    },
                    commandType: CommandType.StoredProcedure);

                // Return distinct Id results. If at least one of the grouped results allows edit, that we return it.
                return results
                    .GroupBy(c => c.Id)
                    .Select(g => g.OrderByDescending(og => og.Edit).First())
                    .ToList();
            }
        }

        public async Task CreateAsync(CipherDetails cipher)
        {
            cipher.SetNewId();
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.ExecuteAsync(
                    $"[{Schema}].[CipherDetails_Create]",
                    cipher,
                    commandType: CommandType.StoredProcedure);
            }
        }

        public async Task ReplaceAsync(CipherDetails obj)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.ExecuteAsync(
                    $"[{Schema}].[CipherDetails_Update]",
                    obj,
                    commandType: CommandType.StoredProcedure);
            }
        }

        public async Task UpsertAsync(CipherDetails cipher)
        {
            if(cipher.Id.Equals(default(Guid)))
            {
                await CreateAsync(cipher);
            }
            else
            {
                await ReplaceAsync(cipher);
            }
        }

        public async Task ReplaceAsync(Cipher obj, IEnumerable<Guid> collectionIds)
        {
            var objWithCollections = JsonConvert.DeserializeObject<CipherWithCollections>(JsonConvert.SerializeObject(obj));
            objWithCollections.CollectionIds = collectionIds.ToGuidIdArrayTVP();

            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.ExecuteAsync(
                    $"[{Schema}].[Cipher_UpdateWithCollections]",
                    objWithCollections,
                    commandType: CommandType.StoredProcedure);
            }
        }

        public async Task UpdatePartialAsync(Guid id, Guid userId, Guid? folderId, bool favorite)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.ExecuteAsync(
                    $"[{Schema}].[Cipher_UpdatePartial]",
                    new { Id = id, UserId = userId, FolderId = folderId, Favorite = favorite },
                    commandType: CommandType.StoredProcedure);
            }
        }

        public Task UpdateUserEmailPasswordAndCiphersAsync(User user, IEnumerable<Cipher> ciphers, IEnumerable<Folder> folders)
        {
            using(var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using(var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Update user.

                        using(var cmd = new SqlCommand("[dbo].[User_UpdateEmailPassword]", connection, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = user.Id;
                            cmd.Parameters.Add("@Email", SqlDbType.NVarChar).Value = user.Email;
                            cmd.Parameters.Add("@EmailVerified", SqlDbType.NVarChar).Value = user.EmailVerified;
                            cmd.Parameters.Add("@MasterPassword", SqlDbType.NVarChar).Value = user.MasterPassword;
                            cmd.Parameters.Add("@SecurityStamp", SqlDbType.NVarChar).Value = user.SecurityStamp;
                            if(string.IsNullOrWhiteSpace(user.PrivateKey))
                            {
                                cmd.Parameters.Add("@PrivateKey", SqlDbType.VarChar).Value = DBNull.Value;
                            }
                            else
                            {
                                cmd.Parameters.Add("@PrivateKey", SqlDbType.VarChar).Value = user.PrivateKey;
                            }
                            cmd.Parameters.Add("@RevisionDate", SqlDbType.DateTime2).Value = user.RevisionDate;
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Create temp tables to bulk copy into.

                        var sqlCreateTemp = @"
                            SELECT TOP 0 *
                            INTO #TempCipher
                            FROM [dbo].[Cipher]

                            SELECT TOP 0 *
                            INTO #TempFolder
                            FROM [dbo].[Folder]";

                        using(var cmd = new SqlCommand(sqlCreateTemp, connection, transaction))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // 3. Bulk copy into temp tables.

                        if(ciphers.Any())
                        {
                            using(var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity, transaction))
                            {
                                bulkCopy.DestinationTableName = "#TempCipher";
                                var dataTable = BuildCiphersTable(ciphers);
                                bulkCopy.WriteToServer(dataTable);
                            }
                        }

                        if(folders.Any())
                        {
                            using(var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity, transaction))
                            {
                                bulkCopy.DestinationTableName = "#TempFolder";
                                var dataTable = BuildFoldersTable(folders);
                                bulkCopy.WriteToServer(dataTable);
                            }
                        }

                        // 4. Insert into real tables from temp tables and clean up.

                        var sql = string.Empty;

                        if(ciphers.Any())
                        {
                            sql += @"
                                UPDATE
                                    [dbo].[Cipher]
                                SET
                                    [Data] = TC.[Data],
                                    [RevisionDate] = TC.[RevisionDate]
                                FROM
                                    [dbo].[Cipher] C
                                INNER JOIN
                                    #TempCipher TC ON C.Id = TC.Id
                                WHERE
                                    C.[UserId] = @UserId";
                        }

                        if(folders.Any())
                        {
                            sql += @"
                                UPDATE
                                    [dbo].[Folder]
                                SET
                                    [Name] = TF.[Name],
                                    [RevisionDate] = TF.[RevisionDate]
                                FROM
                                    [dbo].[Folder] F
                                INNER JOIN
                                    #TempFolder TF ON F.Id = TF.Id
                                WHERE
                                    F.[UserId] = @UserId";
                        }

                        sql += @"
                            DROP TABLE #TempCipher
                            DROP TABLE #TempFolder";

                        using(var cmd = new SqlCommand(sql, connection, transaction))
                        {
                            cmd.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = user.Id;
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }

            return Task.FromResult(0);
        }

        public async Task CreateAsync(IEnumerable<Cipher> ciphers, IEnumerable<Folder> folders)
        {
            if(!ciphers.Any())
            {
                return;
            }

            using(var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using(var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        if(folders.Any())
                        {
                            using(var bulkCopy = new SqlBulkCopy(connection,
                                SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.FireTriggers, transaction))
                            {
                                bulkCopy.DestinationTableName = "[dbo].[Folder]";
                                var dataTable = BuildFoldersTable(folders);
                                bulkCopy.WriteToServer(dataTable);
                            }
                        }

                        using(var bulkCopy = new SqlBulkCopy(connection,
                            SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.FireTriggers, transaction))
                        {
                            bulkCopy.DestinationTableName = "[dbo].[Cipher]";
                            var dataTable = BuildCiphersTable(ciphers);
                            bulkCopy.WriteToServer(dataTable);
                        }

                        await connection.ExecuteAsync(
                                $"[{Schema}].[User_BumpAccountRevisionDate]",
                                new { Id = ciphers.First().UserId },
                                commandType: CommandType.StoredProcedure, transaction: transaction);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private DataTable BuildCiphersTable(IEnumerable<Cipher> ciphers)
        {
            var c = ciphers.FirstOrDefault();
            if(c == null)
            {
                throw new ApplicationException("Must have some ciphers to bulk import.");
            }

            var ciphersTable = new DataTable("CipherDataTable");

            var idColumn = new DataColumn(nameof(c.Id), c.Id.GetType());
            ciphersTable.Columns.Add(idColumn);
            var userIdColumn = new DataColumn(nameof(c.UserId), typeof(Guid));
            ciphersTable.Columns.Add(userIdColumn);
            var organizationId = new DataColumn(nameof(c.OrganizationId), typeof(Guid));
            ciphersTable.Columns.Add(organizationId);
            var typeColumn = new DataColumn(nameof(c.Type), typeof(short));
            ciphersTable.Columns.Add(typeColumn);
            var dataColumn = new DataColumn(nameof(c.Data), typeof(string));
            ciphersTable.Columns.Add(dataColumn);
            var favoritesColumn = new DataColumn(nameof(c.Favorites), typeof(string));
            ciphersTable.Columns.Add(favoritesColumn);
            var foldersColumn = new DataColumn(nameof(c.Folders), typeof(string));
            ciphersTable.Columns.Add(foldersColumn);
            var creationDateColumn = new DataColumn(nameof(c.CreationDate), c.CreationDate.GetType());
            ciphersTable.Columns.Add(creationDateColumn);
            var revisionDateColumn = new DataColumn(nameof(c.RevisionDate), c.RevisionDate.GetType());
            ciphersTable.Columns.Add(revisionDateColumn);

            var keys = new DataColumn[1];
            keys[0] = idColumn;
            ciphersTable.PrimaryKey = keys;

            foreach(var cipher in ciphers)
            {
                var row = ciphersTable.NewRow();

                row[idColumn] = cipher.Id;
                row[userIdColumn] = cipher.UserId.HasValue ? (object)cipher.UserId.Value : DBNull.Value;
                row[organizationId] = cipher.OrganizationId.HasValue ? (object)cipher.OrganizationId.Value : DBNull.Value;
                row[typeColumn] = (short)cipher.Type;
                row[dataColumn] = cipher.Data;
                row[favoritesColumn] = cipher.Favorites;
                row[foldersColumn] = cipher.Folders;
                row[creationDateColumn] = cipher.CreationDate;
                row[revisionDateColumn] = cipher.RevisionDate;

                ciphersTable.Rows.Add(row);
            }

            return ciphersTable;
        }

        private DataTable BuildFoldersTable(IEnumerable<Folder> folders)
        {
            var f = folders.FirstOrDefault();
            if(f == null)
            {
                throw new ApplicationException("Must have some folders to bulk import.");
            }

            var foldersTable = new DataTable("FolderDataTable");

            var idColumn = new DataColumn(nameof(f.Id), f.Id.GetType());
            foldersTable.Columns.Add(idColumn);
            var userIdColumn = new DataColumn(nameof(f.UserId), f.UserId.GetType());
            foldersTable.Columns.Add(userIdColumn);
            var nameColumn = new DataColumn(nameof(f.Name), typeof(string));
            foldersTable.Columns.Add(nameColumn);
            var creationDateColumn = new DataColumn(nameof(f.CreationDate), f.CreationDate.GetType());
            foldersTable.Columns.Add(creationDateColumn);
            var revisionDateColumn = new DataColumn(nameof(f.RevisionDate), f.RevisionDate.GetType());
            foldersTable.Columns.Add(revisionDateColumn);

            var keys = new DataColumn[1];
            keys[0] = idColumn;
            foldersTable.PrimaryKey = keys;

            foreach(var folder in folders)
            {
                var row = foldersTable.NewRow();

                row[idColumn] = folder.Id;
                row[userIdColumn] = folder.UserId;
                row[nameColumn] = folder.Name;
                row[creationDateColumn] = folder.CreationDate;
                row[revisionDateColumn] = folder.RevisionDate;

                foldersTable.Rows.Add(row);
            }

            return foldersTable;
        }

        public class CipherWithCollections : Cipher
        {
            public DataTable CollectionIds { get; set; }
        }
    }
}
