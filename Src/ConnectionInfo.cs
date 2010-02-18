using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using RT.SqlChain.Schema;
using RT.Util;
using RT.Util.Xml;
using RT.Util.ExtensionMethods;

namespace RT.SqlChain
{
    /// <summary>
    /// Describes an SqlChain database connection.
    /// </summary>
    public abstract class ConnectionInfo
    {
        protected abstract string ProviderNamespace { get; }

        /// <summary>
        /// Gets/sets a <see cref="TextWriter"/> object used for logging debug information, including
        /// all SQL queries, associated with this connection. Defaults to null, which disables logging.
        /// </summary>
        public TextWriter Log { get; set; }

        /// <summary>
        /// Creates a schema retriever appropriate for the database engine being used.
        /// </summary>
        /// <param name="conn">An open ADO.NET connection to be used for retrieving schema information.</param>
        public abstract SchemaRetriever CreateSchemaRetriever(DbConnection conn);

        /// <summary>
        /// Creates a schema mutator appropriate for the database engine being used.
        /// </summary>
        /// <param name="conn">An open ADO.NET connection to be used by the mutator for
        /// retrieving schema information and applying modifications.</param>
        /// <param name="readOnly">If true, the mutator will not make any actual changes, which
        /// is useful if one is only interested in the resulting SQL.</param>
        public abstract SchemaMutator CreateSchemaMutator(DbConnection conn, bool readOnly);

        /// <summary>
        /// Creates a <see cref="DbConnection"/> as described by this class. The returned connection
        /// needs to be opened before use. This method will likely fail if the database does not exist.
        /// </summary>
        public abstract DbConnection CreateConnection();

        /// <summary>
        /// Creates a <see cref="DbConnection"/> as described by this class, for the purpose of creating
        /// a schema from scratch. May perform additional operations to make it possible to open the
        /// returned connection. This method will not fail just because the database does not exist.
        /// </summary>
        public abstract DbConnection CreateConnectionForSchemaCreation();

        /// <summary>
        /// Completely erases the schema represented by this class. This method succeeds if and only if
        /// the schema no longer exists on return. It will likely fail if there exist connections to the schema.
        /// </summary>
        public abstract void DeleteSchema();

        /// <summary>
        /// Returns true if the schema represented by this class exists, false otherwise. May throw an
        /// exception if the existence of a schema cannot be determined.
        /// </summary>
        public abstract bool SchemaExists();

        /// <summary>
        /// Instantiates an IQToolkit "entity provider" for a new connection described by this class, using
        /// the specified mapping type to map tables/rows onto types/instances.
        /// </summary>
        public virtual DbEntityProvider CreateEntityProvider(DbConnection connection, Type mappingType)
        {
            var language = (QueryLanguage) Activator.CreateInstance(QueryLanguageType);
            var provider = (DbEntityProvider) Activator.CreateInstance(ProviderType,
                new object[] { connection, new AttributeMapping(language, mappingType), QueryPolicy.Default });

            return provider;
        }

        #region ProviderType, QueryLanguageType and AdoConnectionType

        [XmlIgnore]
        private Type _providerType, _adoConnectionType, _queryLanguageType;

        protected Type ProviderType
        {
            get
            {
                if (_providerType == null)
                {
                    _providerType = tryFindDescendantOfType(typeof(DbEntityProvider), ProviderNamespace);
                    if (_providerType == null)
                        throw new InvalidOperationException(string.Format("Could not find an appropriate \"{0}\" in the namespace \"{1}\"", typeof(DbEntityProvider), ProviderNamespace));
                }
                return _providerType;
            }
        }

        protected Type QueryLanguageType
        {
            get
            {
                if (_queryLanguageType == null)
                {
                    _queryLanguageType = tryFindDescendantOfType(typeof(QueryLanguage), _providerType.Namespace);
                    if (_queryLanguageType == null)
                        throw new InvalidOperationException(string.Format("Could not find a \"{0}\" for \"{1}\"", typeof(QueryLanguage), _providerType));
                }
                return _queryLanguageType;
            }
        }

        protected Type AdoConnectionType
        {
            get
            {
                if (_adoConnectionType == null)
                {
                    foreach (var con in ProviderType.GetConstructors())
                        foreach (var arg in con.GetParameters())
                            if (arg.ParameterType.IsSubclassOf(typeof(DbConnection)))
                            {
                                _adoConnectionType = arg.ParameterType;
                                return _adoConnectionType;
                            }
                    throw new InvalidOperationException(string.Format("Could not deduce ADO connection type for \"{0}\"", _providerType));
                }
                return _adoConnectionType;
            }
        }

        private static bool _iqtoolkitLoaded = false;

        private static Type tryFindDescendantOfType(Type type, string @namespace)
        {
            Type result;

            // Look in the executing or entry assembly, for cases where it's been merged
            var assyExecuting = Assembly.GetExecutingAssembly();
            result = tryFindDescendantOfType(assyExecuting, type, @namespace);
            if (result != null) return result;

            var assyEntry = Assembly.GetEntryAssembly();
            if (assyExecuting != assyEntry)
            {
                result = tryFindDescendantOfType(assyEntry, type, @namespace);
                if (result != null) return result;
            }

            // Look in all other app domain assemblies, but use assembly name as an optimization
            if (!_iqtoolkitLoaded)
            {
                // Even if the IQToolkit.Data.*.dll (iqtoolkit providers) assemblies are referenced, they may not necessarily
                // be automatically loaded into the current appdomain - for example, they won't if none of their types are
                // used directly and the program starts with the current directory set somewhere away from exe path.
                foreach (var filename in Directory.GetFiles(PathUtil.AppPath, "IQToolkit.Data.*.dll"))
                    Assembly.LoadFrom(filename);
                _iqtoolkitLoaded = true;
            }
            foreach (var assy in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains(@namespace)))
            {
                result = tryFindDescendantOfType(assy, type, @namespace);
                if (result != null) return result;
            }

            // Try to load from an external file of the namespace name.
            try
            {
                result = tryFindDescendantOfType(Assembly.LoadFrom(@namespace + ".dll"), type, @namespace);
                if (result != null) return result;
            }
            catch { }

            return null;
        }

        private static Type tryFindDescendantOfType(Assembly assembly, Type type, string @namespace)
        {
            var types = assembly.GetTypes().Where(t => t.IsSubclassOf(type) && t.Namespace == @namespace).ToArray();
            if (types.Length == 1)
                return types[0];
            else if (types.Length == 0)
                return null;
            else
                throw new InvalidOperationException(string.Format("Multiple descendants of \"{0}\" found in namespace \"{1}\", assembly \"{2}\".", type, @namespace, assembly));
        }

        #endregion
    }

    /// <summary>Describes an SqlChain connection to an SQLite database.</summary>
    public class SqliteConnectionInfo : ConnectionInfo
    {
        public string FileName { get; private set; }

        protected override string ProviderNamespace { get { return "IQToolkit.Data.SQLite"; } }

        /// <summary>
        /// Describes a connection to an SQLite database in file <paramref name="fileName"/>.
        /// </summary>
        public SqliteConnectionInfo(string fileName)
        {
            FileName = fileName;
        }

        // For XmlClassify
        protected SqliteConnectionInfo() { }

        public override SchemaRetriever CreateSchemaRetriever(DbConnection conn)
        {
            return new SqliteSchemaRetriever(conn);
        }

        public override SchemaMutator CreateSchemaMutator(DbConnection conn, bool readOnly)
        {
            return new SqliteSchemaMutator(conn, readOnly) { Log = Log };
        }

        public override DbConnection CreateConnection()
        {
            var conn = (DbConnection) Activator.CreateInstance(AdoConnectionType);
            conn.ConnectionString = new DbConnectionStringBuilder()
                {
                    {"Data Source", FileName},
                    {"Version", "3"},
                    {"FailIfMissing", "True"},
                }.ConnectionString;
            return conn;
        }

        public override DbConnection CreateConnectionForSchemaCreation()
        {
            var conn = (DbConnection) Activator.CreateInstance(AdoConnectionType);
            conn.ConnectionString = new DbConnectionStringBuilder()
                {
                    {"Data Source", FileName},
                    {"Version", "3"},
                    {"FailIfMissing", "False"},
                }.ConnectionString;
            return conn;
        }

        public override void DeleteSchema()
        {
            try
            {
                File.SetAttributes(FileName, FileAttributes.Normal);
                File.Delete(FileName);
            }
            catch (FileNotFoundException) { }

            if (Log != null)
            {
                Log.WriteLine("Schema deleted.");
                Log.WriteLine();
            }
        }

        public override bool SchemaExists()
        {
            return File.Exists(FileName);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SqliteConnectionInfo))
                return false;
            return ((SqliteConnectionInfo) obj).FileName.Equals(FileName, StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return FileName.ToLowerInvariant().GetHashCode();
        }
    }

    /// <summary>Describes an SqlChain connection to a Microsoft SQL Server database.</summary>
    public class SqlServerConnectionInfo : ConnectionInfo
    {
        public string Server { get; private set; }
        public string Database { get; private set; }

        protected override string ProviderNamespace { get { return "IQToolkit.Data.SqlClient"; } }

        public SqlServerConnectionInfo(string server, string database)
        {
            Server = server;
            Database = database;
        }

        // For XmlClassify
        protected SqlServerConnectionInfo() { }

        public override SchemaRetriever CreateSchemaRetriever(DbConnection conn)
        {
            return new SqlServerSchemaRetriever(conn);
        }

        public override SchemaMutator CreateSchemaMutator(DbConnection conn, bool readOnly)
        {
            return new SqlServerSchemaMutator(conn, readOnly) { Log = Log };
        }

        public override DbConnection CreateConnection()
        {
            var conn = (DbConnection) Activator.CreateInstance(AdoConnectionType);
            conn.ConnectionString = new DbConnectionStringBuilder()
                {
                    {"Server", Server},
                    {"Database", Database},
                    {"Trusted_Connection", "True"},
                }.ConnectionString;
            return conn;
        }

        public override DbConnection CreateConnectionForSchemaCreation()
        {
            // First try to create the database, in case it doesn't exist yet
            try
            {
                using (var master = (DbConnection) Activator.CreateInstance(AdoConnectionType))
                {
                    master.ConnectionString = new DbConnectionStringBuilder()
                        {
                            {"Server", Server},
                            {"Database", "master"},
                            {"Trusted_Connection", "True"},
                        }.ConnectionString;
                    master.Open();
                    using (var cmd = master.CreateCommand())
                    {
                        cmd.CommandText = "CREATE DATABASE " + Database;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (DbException)
            {
            }

            var conn = (DbConnection) Activator.CreateInstance(AdoConnectionType);
            conn.ConnectionString = new DbConnectionStringBuilder()
                {
                    {"Server", Server},
                    {"Database", Database},
                    {"Trusted_Connection", "True"},
                }.ConnectionString;
            return conn;
        }

        public override void DeleteSchema()
        {
            using (var master = (DbConnection) Activator.CreateInstance(AdoConnectionType))
            {
                master.ConnectionString = new DbConnectionStringBuilder()
                        {
                            {"Server", Server},
                            {"Database", "master"},
                            {"Trusted_Connection", "True"},
                        }.ConnectionString;
                master.Open();
                if (schemaExists(master))
                    using (var cmd = master.CreateCommand())
                    {
                        cmd.CommandText = "DROP DATABASE " + Database;
                        if (Log != null)
                        {
                            Log.WriteLine(cmd.CommandText);
                            Log.WriteLine();
                        }
                        cmd.ExecuteNonQuery();
                    }
            }

            if (Log != null)
            {
                Log.WriteLine("Schema deleted.");
                Log.WriteLine();
            }
        }

        public override bool SchemaExists()
        {
            using (var master = (DbConnection) Activator.CreateInstance(AdoConnectionType))
            {
                master.ConnectionString = new DbConnectionStringBuilder()
                        {
                            {"Server", Server},
                            {"Database", "master"},
                            {"Trusted_Connection", "True"},
                        }.ConnectionString;
                master.Open();
                return schemaExists(master);
            }
        }

        private bool schemaExists(DbConnection master)
        {
            using (var cmd = master.CreateCommand())
            {
                cmd.CommandText = "SELECT Count(*) FROM sys.databases WHERE [name]='{0}'".Fmt(Database);
                if (Log != null)
                {
                    Log.WriteLine(cmd.CommandText);
                    Log.WriteLine();
                }
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }

        public override bool Equals(object obj)
        {
            var objSql = obj as SqlServerConnectionInfo;
            if (objSql == null)
                return false;
            return objSql.Server.EqualsNoCase(Server) && objSql.Database.EqualsNoCase(Database);
        }

        public override int GetHashCode()
        {
            return Server.ToLowerInvariant().GetHashCode() + Database.ToLowerInvariant().GetHashCode() * 17;
        }
    }
}
