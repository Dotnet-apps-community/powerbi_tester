using Analizer.Tables;
using GoogleBigQuery = Google.Cloud.BigQuery.V2;
using Microsoft.AnalysisServices.Tabular;
using Table = Analizer.Tables.Table;
namespace Analizer
{
    public class Report
    {
        private Database _database;
        private List<BigQueryTable> _tables = [];
        private List<Table> _otherTables = [];

        public Report(Database database)
        {
            string currentDir = Directory.GetCurrentDirectory();
            string ServiceAccountPath = Path.Combine(currentDir, "BigQuery_ServiceAccount.json");
            GoogleBigQuery.BigQueryClient client = GoogleBigQuery.BigQueryClient.Create(ServiceAccountPath);
            _database = database;
            foreach (var table in database.Model.Tables)
            {
                if (Table.IsBigQueryTable(table))
                {
                    _tables.Add(new BigQueryTable(table, client, ""));
                }
                else
                {
                    _otherTables.Add(new Table(table));
                }
            }
        }
    }
}