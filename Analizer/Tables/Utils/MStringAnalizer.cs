

using System.Text;
using Microsoft.AnalysisServices.Tabular;

namespace Analizer.Tables.Utils
{
    public static class MStringAnalizer
    {
        public static bool IsNativeQuery(string expression)
        {
            return expression.Contains("Value.NativeQuery", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsJsonQuery(string expression)
        {
            return expression.Contains("Json.Document", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsCsvQuery(string expression)
        {
            return expression.Contains("Csv.Document", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsGoogleSheetQuery(string expression)
        {
            return expression.Contains("GoogleSheets.Contents", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsBigQueryQuery(string expression)
        {
            return
            expression.Contains("GoogleBigQuery.Database", StringComparison.OrdinalIgnoreCase);
        }

        public static string[] GetPowerQuerySteps(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return Array.Empty<string>();

            // Buscar el bloque "let...in"
            var letIndex = expression.IndexOf("let", StringComparison.OrdinalIgnoreCase);
            var inIndex = expression.LastIndexOf("in", StringComparison.OrdinalIgnoreCase);

            if (letIndex == -1 || inIndex == -1 || letIndex >= inIndex)
                return Array.Empty<string>();

            // Extraer la sección entre "let" y "in"
            var stepsSection = expression.Substring(letIndex + 3, inIndex - letIndex - 3).Trim();

            // Separar los pasos usando comas como delimitador, pero considerando paréntesis y comillas
            var steps = new List<string>();
            var currentStep = "";
            var parenthesesCount = 0;
            var inQuotes = false;
            var quoteChar = '\0';

            for (int i = 0; i < stepsSection.Length; i++)
            {
                char c = stepsSection[i];

                // Manejar comillas
                if ((c == '"' || c == '\'') && !inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (c == quoteChar && inQuotes)
                {
                    inQuotes = false;
                    quoteChar = '\0';
                }

                // Contar paréntesis cuando no estamos en comillas
                if (!inQuotes)
                {
                    if (c == '(' || c == '[' || c == '{')
                        parenthesesCount++;
                    else if (c == ')' || c == ']' || c == '}')
                        parenthesesCount--;

                    // Si encontramos una coma al nivel superior, es un separador de pasos
                    if (c == ',' && parenthesesCount == 0)
                    {
                        var step = currentStep.Trim();
                        if (!string.IsNullOrEmpty(step))
                        {
                            // Extraer solo el nombre del paso (parte antes del '=')
                            var equalIndex = step.IndexOf('=');
                            if (equalIndex > 0)
                            {
                                var stepName = step.Substring(0, equalIndex).Trim();
                                steps.Add(stepName);
                            }
                        }
                        currentStep = "";
                        continue;
                    }
                }

                currentStep += c;
            }

            // Agregar el último paso
            var lastStep = currentStep.Trim();
            if (!string.IsNullOrEmpty(lastStep))
            {
                var equalIndex = lastStep.IndexOf('=');
                if (equalIndex > 0)
                {
                    var stepName = lastStep.Substring(0, equalIndex).Trim();
                    steps.Add(stepName);
                }
            }

            return steps.ToArray();
        }

        public static string[] GetPowerQuerysContents(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return Array.Empty<string>();

            // Buscar el bloque "let...in"
            var letIndex = expression.IndexOf("let", StringComparison.OrdinalIgnoreCase);
            var inIndex = expression.LastIndexOf("in", StringComparison.OrdinalIgnoreCase);

            if (letIndex == -1 || inIndex == -1 || letIndex >= inIndex)
                return Array.Empty<string>();

            // Extraer la sección entre "let" y "in"
            var stepsSection = expression.Substring(letIndex + 3, inIndex - letIndex - 3).Trim();

            // Separar los pasos usando comas como delimitador, pero considerando paréntesis y comillas
            var stepContents = new List<string>();
            var currentStep = "";
            var parenthesesCount = 0;
            var inQuotes = false;
            var quoteChar = '\0';

            for (int i = 0; i < stepsSection.Length; i++)
            {
                char c = stepsSection[i];

                // Manejar comillas
                if ((c == '"' || c == '\'') && !inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (c == quoteChar && inQuotes)
                {
                    inQuotes = false;
                    quoteChar = '\0';
                }

                // Contar paréntesis cuando no estamos en comillas
                if (!inQuotes)
                {
                    if (c == '(' || c == '[' || c == '{')
                        parenthesesCount++;
                    else if (c == ')' || c == ']' || c == '}')
                        parenthesesCount--;

                    // Si encontramos una coma al nivel superior, es un separador de pasos
                    if (c == ',' && parenthesesCount == 0)
                    {
                        var step = currentStep.Trim();
                        if (!string.IsNullOrEmpty(step))
                        {
                            // Extraer el contenido completo del paso (parte después del '=')
                            var equalIndex = step.IndexOf('=');
                            if (equalIndex > 0 && equalIndex < step.Length - 1)
                            {
                                var stepContent = step.Substring(equalIndex + 1).Trim();
                                stepContents.Add(stepContent);
                            }
                        }
                        currentStep = "";
                        continue;
                    }
                }

                currentStep += c;
            }

            // Agregar el último paso
            var lastStep = currentStep.Trim();
            if (!string.IsNullOrEmpty(lastStep))
            {
                var equalIndex = lastStep.IndexOf('=');
                if (equalIndex > 0 && equalIndex < lastStep.Length - 1)
                {
                    var stepContent = lastStep.Substring(equalIndex + 1).Trim();
                    stepContents.Add(stepContent);
                }
            }

            return stepContents.ToArray();
        }

        public static string[] GetMExpressionFromTable(Microsoft.AnalysisServices.Tabular.Table table)
        {
            List<Partition> partitions = table.Partitions.ToList();
            List<string> mQueries = new List<string>();

            foreach (var partition in partitions)
            {
                if (partition.Source is MPartitionSource mPartitionSource)
                {
                    mQueries.Add(mPartitionSource.Expression);
                }
            }

            return mQueries.ToArray();
        }

        public static string GetSqlQueryFromStep(string mExpression)
        {
            if (string.IsNullOrEmpty(mExpression))
                return null;

            const string marker = "NativeQuery(";
            int idx = mExpression.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return null; // no es NativeQuery

            // Avanzar hasta la primera comilla doble que empieza el SQL (primer literal string después de los argumentos iniciales)
            int pos = mExpression.IndexOf('"', idx + marker.Length);
            if (pos < 0)
                return null; // no hay cadena

            pos++; // entrar al contenido
            var sb = new StringBuilder();
            while (pos < mExpression.Length)
            {
                char c = mExpression[pos];
                if (c == '"')
                {
                    // ¿Es una comilla escapada (doble "") o el cierre?
                    if (pos + 1 < mExpression.Length && mExpression[pos + 1] == '"')
                    {
                        // escapada -> agregar una comilla y saltar ambas
                        sb.Append('"');
                        pos += 2;
                        continue;
                    }
                    else
                    {
                        // cierre de la cadena
                        break;
                    }
                }
                else
                {
                    sb.Append(c);
                    pos++;
                }
            }

            string raw = sb.ToString();

            // Reemplazar los tokens de M por equivalentes
            string cleaned = raw
                .Replace("#(lf)", "\n")
                .Replace("#(cr)", "\r") // por si acaso
                .Replace("#(tab)", "\t");

            return cleaned;
        }
    }
}