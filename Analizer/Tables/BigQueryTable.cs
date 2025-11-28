using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Analizer.Tables.Enums;
using Analizer.Tables.Utils;
using Analizer.Bigquery;
using Google.Cloud.BigQuery.V2;

namespace Analizer.Tables;

public class BigQueryTable : Table
{
    public string ServiceAccountPath { get; set; } = "BigQuery_ServiceAccount.json";
    public Connectors Connector = Connectors.BigQuery;
    public SqlConnection? SqlConnection { get; set; }

    public string ProjectId { get; set; } = "";

    public List<QueryDefinition> Queries { get; set; } = [];

    public BigQueryClient _client;

    public BigQueryTable(Microsoft.AnalysisServices.Tabular.Table table, BigQueryClient client, string projectId) : base(table)
    {
        SqlConnection = new SqlConnection(this);
        Queries = PowerQueryParser.ExtractQueries(MStringAnalizer.GetMExpressionFromTable(table)[0]);
        _client = client;
        ProjectId = projectId;
        
    }
}

public class SqlConnection
{
    private BigQueryTable _bigQueryTable;

    public SqlConnection(BigQueryTable table)
    {
        _bigQueryTable = table;
        _bigQueryAnalizer = new BigQueryAnalizer(_bigQueryTable._client, _bigQueryTable.ProjectId);
        SetQuery();
        SetIsAllColumnsSelected();
        SetHasStartRangeParameter();
        SetHasRangeEndParameter();
        setQueryData();
    }
    public string Query { get; set; } = "";
    public bool IsAllColumnsSelected { get; set; } = false;
    public bool HasRangeStartParameter { get; set; }
    public bool HasRangeEndParameter { get; set; }
    public ulong RowsQuantityExtracted { get; set; }
    public string[] ColumnsNameJoined { get; set; } = [];
    public int QueryWeightMb { get; set; }

    private BigQueryAnalizer _bigQueryAnalizer;


    private void SetQuery()
    {
        string mQuery = MStringAnalizer.GetMExpressionFromTable(_bigQueryTable._table)[0];
        if (MStringAnalizer.IsNativeQuery(mQuery))
        {
            QueryDefinition[] queries = PowerQueryParser.ExtractQueries(mQuery).ToArray();

            if (queries.Length > 0)
            {
                Query = BigQuerySqlExtractorSingle.ExtractSqlFromBody(queries[0].Body);
            }
        }
    }

    private void SetIsAllColumnsSelected()
    {
        if (Query.Contains("SELECT *")) IsAllColumnsSelected = true;
        else IsAllColumnsSelected = false;
    }

    private void SetHasStartRangeParameter()
    {
        HasRangeStartParameter = Query.Contains("@RangeStart");
    }

    private void SetHasRangeEndParameter()
    {
        HasRangeEndParameter = Query.Contains("@RangeEnd");
    }

    private void setQueryData()
    {
        if (Query != "")
        {
            Results results = _bigQueryAnalizer.GetResults(Query);
            RowsQuantityExtracted = results.TotalRows;
        }
    }
}