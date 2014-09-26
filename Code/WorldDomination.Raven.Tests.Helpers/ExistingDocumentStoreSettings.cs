using System;

namespace WorldDomination.Raven.Tests.Helpers
{
    /// <summary>
    /// Extra settings required when trying to debug against a real database.
    /// </summary>
    /// <remarks>This is a vary rare case of debugging. Generally, you do not create an instance of this class and just use the embedded DocumentStore for in memory tests. Sometimes, you might want to see what data has actually been stored because there's something going wrong and you can't seem to programmatically debug the issue. Therefore, you can use a normal DocumentStore instance.</remarks>
    public class ExistingDocumentStoreSettings
    {
        public const string DefaultDocumentStoreUrl = "http://localhost:8080";
        public const string DefaultDatabaseName = "UnitTests";

        /// <summary>
        /// A new instance of an ExistingDocumentStoreSettings, which is used to connect to a real database.
        /// </summary>
        /// <param name="documentStoreUrl">string: document store Url. Eg. http://localhost:8080</param>
        /// <param name="defaultDatabase">string: database tenant name.</param>
        /// <remarks>If a DocumentStore Url is not provided, then we create/connect to the url: http://localhost:8080.<br/>
        ///          If a Default Database is not provided, then the we create/connect to a database tenant called 'UnitTests'.</remarks>
        public ExistingDocumentStoreSettings(string documentStoreUrl = DefaultDocumentStoreUrl,
            string defaultDatabase = DefaultDatabaseName)
        {
            if (string.IsNullOrWhiteSpace(documentStoreUrl))
            {
                documentStoreUrl = DefaultDocumentStoreUrl;
            }

            if (string.IsNullOrWhiteSpace(defaultDatabase))
            {
                defaultDatabase = DefaultDatabaseName;
            }

            DocumentStoreUrl = documentStoreUrl;
            DefaultDatabase = defaultDatabase;
        }

        /// <summary>
        /// Url of another document store to use in the test scenario.
        /// </summary>
        /// <remarks>This is a vary rare case of debugging. Generally, you do not set the value of this property and just use the embedded DocumentStore for in memory tests. Sometimes, you might want to see what data has actually been stored because there's something going wrong and you can't seem to programmatically debug the issue. Therefore, you can use a normal DocumentStore instance.</remarks>
        public string DocumentStoreUrl { get; private set; }

        /// <summary>
        /// Database tenant name to connect to.
        /// </summary>
        public string DefaultDatabase { get; private set; }
    }
}