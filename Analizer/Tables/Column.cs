using Analizer.Tables.Enums;

namespace Analizer.Tables;

public class Column
{
    public string Name { get; set; } = "";
    public ColumnTypes Type { get; set; }
    public bool IsCalculated { get; set; }
}