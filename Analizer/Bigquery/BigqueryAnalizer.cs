using Google.Apis.Auth.OAuth2;
using Google.Cloud.BigQuery.V2;

namespace Analizer.Bigquery
{
    public struct Results
    {
        public ulong TotalRows { get; set; }
    }
    public class BigQueryAnalizer
    {
        public BigQueryClient bigQueryClient { get; set; }
        public string ProjectId { get; set; } = "";

        private BigQueryClient? _client = null;


        public BigQueryAnalizer(BigQueryClient bigQueryClient, string projectId)
        {
            this.bigQueryClient = bigQueryClient;
            ProjectId = projectId;
        }


        public Results GetResults(string query)
        {
            if (bigQueryClient == null)
            {
                throw new InvalidOperationException("BigQuery client is not initialized.");
            }
            else
            {
                BigQueryResults results = bigQueryClient.ExecuteQuery(query, null, new QueryOptions { });
                if (results == null)
                {
                    throw new InvalidOperationException("Query execution returned null results.");
                }
                else
                {
                    if (results.TotalRows.HasValue)
                    {
                        return new Results
                        {
                            TotalRows = results.TotalRows.Value
                        };
                    }
                    else
                    {
                        throw new InvalidOperationException("TotalRows is null.");
                    }
                }
            }
        }



    }
}
