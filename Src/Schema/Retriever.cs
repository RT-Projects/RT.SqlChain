using System;

namespace RT.SqlChain.Schema
{
    /// <remarks>
    /// TODO: this may be better refactored to be an abstract base class - will be easier to
    /// see when SQL Server retriever is implemented.
    /// </remarks>
    public interface IRetriever
    {
        SchemaInfo RetrieveSchema();
    }
}
