using Analizer.Tables.Enums;

namespace Analizer.Tables;

public class Table
{
    public string Name { get; set; } = "";
    public Connectors Connector { get; set; }
    public bool IsLocal { get; set; } = true;
}