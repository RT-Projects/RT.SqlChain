using System;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using RT.SqlChain.Schema;
using RT.Util.Xml;

namespace RT.SqlChain
{
    /// <summary>
    /// Describes an SqlChain database connection.
    /// </summary>
    public abstract class ConnectionInfo
    {
        public string ConnectionString { get; protected set; }
        protected abstract string ProviderNamespace { get; }

        [XmlIgnore]
        private Type _providerType, _adoConnectionType, _queryLanguageType;

        /// <summary>
        /// Creates a schema mutator appropriate for the database engine being used.
        /// </summary>
        /// <param name="conn">An open ADO.NET connection to be used by the mutator for
        /// retrieving schema information and applying modifications.</param>
        public abstract SchemaMutator CreateSchemaMutator(DbConnection conn);

        /// <summary>
        /// Used by the SqlChain DB connection constructor to instantiate an IQToolkit connection.
        /// </summary>
        public virtual DbEntityProvider CreateEntityProvider(Type mappingType)
        {
            if (_providerType == null)
                _providerType = TryFindDescendantOfType(typeof(DbEntityProvider), ProviderNamespace);
            if (_providerType == null)
                throw new InvalidOperationException(string.Format("Could not find an appropriate \"{0}\" in the namespace \"{1}\"", typeof(DbEntityProvider), ProviderNamespace));

            if (_adoConnectionType == null)
                _adoConnectionType = GetAdoConnectionType(_providerType);
            if (_adoConnectionType == null)
                throw new InvalidOperationException(string.Format("Could not deduce ADO connection type for \"{0}\"", _providerType));

            if (_queryLanguageType == null)
                _queryLanguageType = TryFindDescendantOfType(typeof(QueryLanguage), _providerType.Namespace);
            if (_queryLanguageType == null)
                throw new InvalidOperationException(string.Format("Could not find a \"{0}\" for \"{1}\"", typeof(QueryLanguage), _providerType));

            var connection = (DbConnection) Activator.CreateInstance(_adoConnectionType);
            connection.ConnectionString = ConnectionString;
            var language = (QueryLanguage) Activator.CreateInstance(_queryLanguageType);
            var provider = (DbEntityProvider) Activator.CreateInstance(_providerType,
                new object[] { connection, new AttributeMapping(language, mappingType), QueryPolicy.Default });

            return provider;
        }

        private static Type GetAdoConnectionType(Type providerType)
        {
            foreach (var con in providerType.GetConstructors())
                foreach (var arg in con.GetParameters())
                    if (arg.ParameterType.IsSubclassOf(typeof(DbConnection)))
                        return arg.ParameterType;
            return null;
        }

        private Type TryFindDescendantOfType(Type type, string @namespace)
        {
            Type result;

            // Look in the executing or entry assembly, for cases where it's been merged
            var assyExecuting = Assembly.GetExecutingAssembly();
            result = TryFindDescendantOfType(assyExecuting, type, @namespace);
            if (result != null) return result;

            var assyEntry = Assembly.GetEntryAssembly();
            if (assyExecuting != assyEntry)
            {
                result = TryFindDescendantOfType(assyEntry, type, @namespace);
                if (result != null) return result;
            }

            // Look in all other app domain assemblies, but use assembly name as an optimization
            foreach (var assy in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains(@namespace)))
            {
                result = TryFindDescendantOfType(assy, type, @namespace);
                if (result != null) return result;
            }

            // Try to load from an external file of the namespace name.
            try
            {
                result = TryFindDescendantOfType(Assembly.LoadFrom(@namespace + ".dll"), type, @namespace);
                if (result != null) return result;
            }
            catch { }

            return null;
        }

        private Type TryFindDescendantOfType(Assembly assembly, Type type, string @namespace)
        {
            var types = assembly.GetTypes().Where(t => t.IsSubclassOf(type) && t.Namespace == @namespace).ToArray();
            if (types.Length == 1)
                return types[0];
            else if (types.Length == 0)
                return null;
            else
                throw new InvalidOperationException(string.Format("Multiple descendants of \"{0}\" found in namespace \"{1}\", assembly \"{2}\".", type, @namespace, assembly));
        }
    }

    /// <summary>Describes an SqlChain connection to an SQLite database.</summary>
    public class SqliteConnectionInfo : ConnectionInfo
    {
        protected override string ProviderNamespace
        {
            get { return "IQToolkit.Data.SQLite"; }
        }

        /// <summary>
        /// Describes a connection to an SQLite database described by <paramref name="connectionString"/>.
        /// </summary>
        public SqliteConnectionInfo(string connectionString)
        {
            ConnectionString = connectionString;
        }

        /// <summary>
        /// Creates a schema mutator appropriate for the database engine being used.
        /// </summary>
        public override SchemaMutator CreateSchemaMutator(DbConnection conn)
        {
            return new SqliteSchemaMutator(conn);
        }

        /// <summary>
        /// Describes a connection to an SQLite database in file <paramref name="fileName"/>, and specifies
        /// whether the connection should fail if the file is missing.
        /// </summary>
        public SqliteConnectionInfo(string fileName, bool failIfMissing)
        {
            if (fileName.Contains("\"") && fileName.Contains("'"))
                throw new ArgumentException("File name contains both single and double quotes; this is not supported by the query string mechanism.", "fileName");
            else if (fileName.Contains("\""))
                fileName = "'" + fileName + "'";
            else
                fileName = "\"" + fileName + "\"";
            ConnectionString = string.Format(@"Data Source={0};Version=3;FailIfMissing={1}", fileName, failIfMissing);
        }
    }
    
    /// <summary>Describes an SqlChain connection to a Microsoft SQL Server database.</summary>
    public class SqlServerConnectionInfo : ConnectionInfo
    {
        protected override string ProviderNamespace
        {
            get { return "IQToolkit.Data.SqlClient"; }
        }

        /// <summary>
        /// Creates a schema mutator appropriate for the database engine being used.
        /// </summary>
        public override SchemaMutator CreateSchemaMutator(DbConnection conn)
        {
            throw new NotImplementedException();
        }

        /// <summary>Describes a connection to a Microsoft SQL Server database described by <paramref name="connectionString"/>.</summary>
        public SqlServerConnectionInfo(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}
