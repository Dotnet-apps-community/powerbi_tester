using Analizer.Tables.Enums;

namespace Analizer.Tables;

public class BigQueryTable:Table
{
    new public Connectors Connector = Connectors.BigQuery;
    public SqlConnection? SqlConnection { get; set; }
}

public class SqlConnection
{
    public string Query { get; set; } = "";
    public bool IsAllColumnsSelected { get; set; } = false;
    public bool HasStartRangeParameter { get; set; }
    public bool HasEndRangeParameter { get; set; }
    public int RowsQuantityExtracted { get; set; }
    public string[] ColumnsNameJoined { get; set; } = [];
    public int QueryWeightMb { get; set; }
}