using Analizer.Tables.Enums;

namespace Analizer.Tables.Utils
{

    public static class ColumnTypeConverter
    {
        public static ColumnTypes ConvertToColumnType(Microsoft.AnalysisServices.Tabular.DataType dataType)
        {
            return dataType switch
            {
                Microsoft.AnalysisServices.Tabular.DataType.Boolean => ColumnTypes.Boolean,
                Microsoft.AnalysisServices.Tabular.DataType.DateTime => ColumnTypes.DateTime,
                Microsoft.AnalysisServices.Tabular.DataType.Decimal => ColumnTypes.Number,
                Microsoft.AnalysisServices.Tabular.DataType.Int64 => ColumnTypes.Number,
                Microsoft.AnalysisServices.Tabular.DataType.String => ColumnTypes.Text,
                Microsoft.AnalysisServices.Tabular.DataType.Double => ColumnTypes.Number,
                _ => ColumnTypes.Others
            };
        }
    }
}