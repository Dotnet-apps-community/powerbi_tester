using Analizer.Tables.Enums;
using Microsoft.AnalysisServices.Tabular;
using Analizer.Tables.Utils;
namespace Analizer.Tables;

public class Table
{
    public string Name { get; set; } = "";
    public Connectors Connector { get; set; } = Connectors.Others;
    public bool IsLocal { get; set; } = true;
    public Column[] Columns { get; set; } = [];

    public Microsoft.AnalysisServices.Tabular.Table _table { get; set; }

    public Table(Microsoft.AnalysisServices.Tabular.Table table)
    {
        _table = table;
        GetName();
    }

    private void GetName()
    {
        Name = _table.Name;
    }

    public static bool IsBigQueryTable(Microsoft.AnalysisServices.Tabular.Table table)
    {
        Partition partition = table.Partitions.First();
        PartitionSource partitionSource = partition.Source;

        if (partitionSource is MPartitionSource mPartitionSource)
        {
            switch (mPartitionSource.Expression)
            {
                case string expression when MStringAnalizer.IsBigQueryQuery(expression):
                    return true;
                default:
                    return false;
            }
        }
        return false;

    }
}