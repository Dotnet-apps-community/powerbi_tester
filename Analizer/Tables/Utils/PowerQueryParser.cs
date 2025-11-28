
using System.Text;
using System.Text.RegularExpressions;

public class QueryDefinition
{
    public string Name { get; init; } = "";
    public string Body { get; init; } = "";
}

public static class PowerQueryParser
{
    /// <summary>
    /// Extrae todas las queries definidas a nivel top (ej. "MyQuery = let ... in ...") de un script M.
    /// </summary>
    public static List<QueryDefinition> ExtractQueries(string mScript)
    {
        var results = new List<QueryDefinition>();
        int pos = 0;
        int len = mScript.Length;

        while (pos < len)
        {
            // Avanzar hasta encontrar un posible nombre de query (opcional "shared")
            var match = TopLevelAssignmentMatch(mScript, pos);
            if (match == null)
                break;

            string name = match.Groups["name"].Value;
            int assignPos = match.Groups["equals"].Index; // posición del '='

            // A partir de después del '=', extraer la expresión completa
            int exprStart = assignPos + 1;
            SkipWhitespaceAndComments(mScript, ref exprStart);

            int exprEnd = FindEndOfExpression(mScript, exprStart);
            if (exprEnd < exprStart)
                exprEnd = len;

            string body = mScript.Substring(exprStart, exprEnd - exprStart).TrimEnd();

            results.Add(new QueryDefinition
            {
                Name = name,
                Body = body
            });

            pos = exprEnd;
        }

        return results;
    }

    // Busca la siguiente asignación top-level como "Name =" o "shared Name ="
    private static Match? TopLevelAssignmentMatch(string s, int start)
    {
        // Regex que busca en posición de comienzo de línea (multilínea) opcional "shared", luego identificador y "=", sin consumir mucho
        var pattern = @"(?im)(?:^|\r?\n)\s*(?:shared\s+)?(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?<equals>=)";
        var regex = new Regex(pattern, RegexOptions.Compiled);
        return regex.Match(s, start).Success ? regex.Match(s, start) : null;
    }

    // Avanza pos saltando espacios y comentarios (// y /* */)
    private static void SkipWhitespaceAndComments(string s, ref int pos)
    {
        int len = s.Length;
        while (pos < len)
        {
            if (char.IsWhiteSpace(s[pos]))
            {
                pos++;
                continue;
            }

            if (pos + 1 < len && s[pos] == '/' && s[pos + 1] == '/')
            {
                // comentario de línea
                pos += 2;
                while (pos < len && s[pos] != '\n') pos++;
                continue;
            }

            if (pos + 1 < len && s[pos] == '/' && s[pos + 1] == '*')
            {
                // comentario de bloque
                pos += 2;
                while (pos + 1 < len && !(s[pos] == '*' && s[pos + 1] == '/')) pos++;
                if (pos + 1 < len) pos += 2;
                continue;
            }

            break;
        }
    }

    // Encuentra el final de la expresión que comienza en exprStart.
    // Si empieza con "let", busca el "in" correspondiente (respetando literales y comentarios) y luego consume hasta el siguiente top-level assignment o EOF.
    private static int FindEndOfExpression(string s, int exprStart)
    {
        int len = s.Length;
        int pos = exprStart;

        // Detectar si la expresión empieza con "let" (ignorando mayúsculas/minúsculas)
        if (IsKeywordAt(s, pos, "let"))
        {
            // Necesitamos encontrar el "in" que cierra ese let (balancing de lets internos: cada vez que aparece otro "let" dentro incrementa; cada "in" lo decrementa)
            int balance = 0;
            while (pos < len)
            {
                if (IsStartOfString(s, pos, out int skip))
                {
                    // saltar literal
                    pos = skip;
                    continue;
                }

                if (pos + 1 < len && s[pos] == '/' && s[pos + 1] == '/')
                {
                    // comentario de línea
                    pos += 2;
                    while (pos < len && s[pos] != '\n') pos++;
                    continue;
                }

                if (pos + 1 < len && s[pos] == '/' && s[pos + 1] == '*')
                {
                    // comentario bloque
                    pos += 2;
                    while (pos + 1 < len && !(s[pos] == '*' && s[pos + 1] == '/')) pos++;
                    if (pos + 1 < len) pos += 2;
                    continue;
                }

                if (IsKeywordAt(s, pos, "let"))
                {
                    balance++;
                    pos += 3;
                    continue;
                }

                if (IsKeywordAt(s, pos, "in"))
                {
                    if (balance == 0)
                    {
                        pos += 2;
                        break; // cerrado
                    }
                    else
                    {
                        balance--;
                        pos += 2;
                        continue;
                    }
                }

                pos++;
            }
        }

        // Desde aquí (sea que empezó con let o no) buscamos el comienzo de la siguiente definición top-level para delimitar
        var nextAssign = NextTopLevelAssignmentPosition(s, pos);
        if (nextAssign >= 0)
            return nextAssign;
        else
            return len; // hasta el final
    }

    // Si en la posición hay inicio de string (doble comilla), devuelve la posición después del cierre
    private static bool IsStartOfString(string s, int pos, out int newPos)
    {
        newPos = pos;
        if (s[pos] != '"') return false;
        pos++; // abrir "
        while (pos < s.Length)
        {
            if (s[pos] == '"')
            {
                if (pos + 1 < s.Length && s[pos + 1] == '"')
                {
                    // comilla escapada ""
                    pos += 2;
                    continue;
                }
                else
                {
                    pos++; // cierre
                    break;
                }
            }
            else
            {
                pos++;
            }
        }
        newPos = pos;
        return true;
    }

    // Verifica si en s, en pos, comienza la palabra clave (caso insensible) y es delimitada (no parte de otro identificador)
    private static bool IsKeywordAt(string s, int pos, string keyword)
    {
        if (pos + keyword.Length > s.Length) return false;
        if (!string.Equals(s.Substring(pos, keyword.Length), keyword, StringComparison.OrdinalIgnoreCase))
            return false;

        bool beforeOk = pos == 0 || !IsIdentifierChar(s[pos - 1]);
        bool afterOk = (pos + keyword.Length >= s.Length) || !IsIdentifierChar(s[pos + keyword.Length]);
        return beforeOk && afterOk;
    }

    private static bool IsIdentifierChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    // Busca la siguiente asignación top-level (similar a TopLevelAssignmentMatch) a partir de pos y devuelve su índice (o -1)
    private static int NextTopLevelAssignmentPosition(string s, int pos)
    {
        var pattern = @"(?im)(?:^|\r?\n)\s*(?:shared\s+)?[A-Za-z_][A-Za-z0-9_]*\s*=";
        var regex = new Regex(pattern, RegexOptions.Compiled);
        var m = regex.Match(s, pos);
        return m.Success ? m.Index : -1;
    }
}

public static class BigQuerySqlExtractorSingle

{

    public static string ExtractSqlFromBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("El body no puede ser nulo o vacío.", nameof(body));

        // Normalizar el body removiendo espacios extra y saltos de línea innecesarios
        string normalizedBody = NormalizeBody(body);

        var sqlQueries = new List<string>();
        int pos = 0;

        // Buscar diferentes patrones de consultas SQL
        string[] sqlPatterns = {
            "Value.NativeQuery(",
            "NativeQuery(",
            "Sql.Database(",
            "GoogleBigQuery.Database("
        };

        while (pos < normalizedBody.Length)
        {
            int earliestIdx = -1;
            string matchedPattern = "";

            // Encontrar el patrón más cercano
            foreach (var pattern in sqlPatterns)
            {
                int idx = IndexOfIgnoreCase(normalizedBody, pattern, pos);
                if (idx >= 0 && (earliestIdx < 0 || idx < earliestIdx))
                {
                    earliestIdx = idx;
                    matchedPattern = pattern;
                }
            }

            if (earliestIdx < 0)
                break;

            // Extraer SQL basado en el patrón encontrado
            string extractedSql = ExtractSqlByPattern(normalizedBody, earliestIdx, matchedPattern);
            if (!string.IsNullOrWhiteSpace(extractedSql))
            {
                // Verificar que no sea un duplicado antes de agregarlo
                if (!sqlQueries.Contains(extractedSql))
                {
                    sqlQueries.Add(extractedSql);
                }
            }

            pos = earliestIdx + matchedPattern.Length;
        }

        if (sqlQueries.Count == 0)
            throw new InvalidOperationException("No se encontró ninguna consulta SQL en el body.");

        if (sqlQueries.Count > 1)
        {
            // En caso de múltiples queries, concatenarlas con separador
            return string.Join("\n-- Nueva Query --\n", sqlQueries);
        }

        return sqlQueries[0];
    }

    private static string ExtractSqlByPattern(string body, int patternIndex, string pattern)
    {
        try
        {
            // Buscar el inicio de los parámetros después del patrón
            int openParenIndex = patternIndex + pattern.Length - 1; // -1 porque el patrón ya incluye '('

            // Para diferentes patrones, la posición del SQL puede variar
            int sqlStringIndex = -1;

            if (pattern.Contains("NativeQuery"))
            {
                // Para NativeQuery, el SQL puede estar en diferentes posiciones
                // Intentar encontrar el string más largo que contenga SQL
                sqlStringIndex = FindLargestSqlString(body, openParenIndex);
                if (sqlStringIndex < 0)
                {
                    // Fallback: buscar el segundo parámetro
                    sqlStringIndex = FindNthStringLiteral(body, openParenIndex, 2);
                }
            }
            else if (pattern.Contains("Sql.Database") || pattern.Contains("GoogleBigQuery.Database"))
            {
                // Para estos, buscar después de los parámetros de conexión
                sqlStringIndex = FindSqlInDatabaseCall(body, openParenIndex);
            }

            if (sqlStringIndex < 0)
            {
                // Fallback: buscar la primera string literal que parezca SQL
                sqlStringIndex = FindFirstSqlLikeString(body, openParenIndex);
            }

            if (sqlStringIndex < 0)
                return "";

            string extractedSql = ExtractStringLiteral(body, sqlStringIndex);

            // Limpiar el SQL extraído de cualquier texto adicional
            return CleanExtractedSql(extractedSql);
        }
        catch
        {
            return "";
        }
    }

    private static string CleanExtractedSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return "";

        // Remover caracteres de escape comunes de Power Query
        sql = sql.Trim();

        // Buscar el inicio real del SQL (comenzando con SELECT, WITH, etc.)
        string[] sqlStarters = { "SELECT", "WITH", "INSERT", "UPDATE", "DELETE", "CREATE", "SHOW", "DESCRIBE", "EXPLAIN" };

        foreach (string starter in sqlStarters)
        {
            int starterIndex = sql.IndexOf(starter, StringComparison.OrdinalIgnoreCase);
            if (starterIndex >= 0)
            {
                // Tomar desde el inicio del SQL hasta el final
                sql = sql.Substring(starterIndex);
                break;
            }
        }

        // Limpiar caracteres de escape y formato
        sql = sql.Replace("\\\"", "\"")
                 .Replace("\\'", "'")
                 .Replace("\\n", "\n")
                 .Replace("\\r", "\r")
                 .Replace("\\t", "\t");

        return sql.Trim();
    }

    private static int FindLargestSqlString(string body, int startPos)
    {
        int longestIndex = -1;
        int longestLength = 0;
        int pos = startPos;

        while (pos < body.Length)
        {
            int quoteIndex = body.IndexOf('"', pos);
            if (quoteIndex < 0)
                break;

            string literal = ExtractStringLiteral(body, quoteIndex);

            // Verificar si contiene SQL y es más largo que el anterior
            if (literal.Length > longestLength && ContainsSqlKeywords(literal, new[] { "SELECT", "WITH", "FROM", "INSERT", "UPDATE", "DELETE", "CREATE" }))
            {
                longestLength = literal.Length;
                longestIndex = quoteIndex;
            }

            pos = SkipStringLiteral(body, quoteIndex);
        }

        return longestIndex;
    }

    private static int FindNthStringLiteral(string body, int startPos, int n)
    {
        int count = 0;
        int pos = startPos;

        while (pos < body.Length && count < n)
        {
            int quoteIndex = body.IndexOf('"', pos);
            if (quoteIndex < 0)
                break;

            count++;
            if (count == n)
                return quoteIndex;

            // Saltar esta string literal completa
            pos = SkipStringLiteral(body, quoteIndex);
        }

        return -1;
    }

    private static int FindSqlInDatabaseCall(string body, int openParenIndex)
    {
        // Buscar patrones comunes que indican SQL en llamadas a base de datos
        string[] sqlIndicators = { "SELECT", "WITH", "FROM", "INSERT", "UPDATE", "DELETE", "CREATE" };

        int pos = openParenIndex;
        while (pos < body.Length)
        {
            int quoteIndex = body.IndexOf('"', pos);
            if (quoteIndex < 0)
                break;

            string literal = ExtractStringLiteral(body, quoteIndex);
            if (ContainsSqlKeywords(literal, sqlIndicators))
                return quoteIndex;

            pos = SkipStringLiteral(body, quoteIndex);
        }

        return -1;
    }

    private static int FindFirstSqlLikeString(string body, int startPos)
    {
        string[] sqlKeywords = { "SELECT", "WITH", "FROM", "INSERT", "UPDATE", "DELETE", "CREATE", "SHOW", "DESCRIBE" };

        int pos = startPos;
        while (pos < body.Length)
        {
            int quoteIndex = body.IndexOf('"', pos);
            if (quoteIndex < 0)
                break;

            string literal = ExtractStringLiteral(body, quoteIndex);
            if (literal.Length > 10 && ContainsSqlKeywords(literal, sqlKeywords))
                return quoteIndex;

            pos = SkipStringLiteral(body, quoteIndex);
        }

        return -1;
    }

    private static bool ContainsSqlKeywords(string text, string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string upperText = text.ToUpperInvariant();
        return keywords.Any(keyword => upperText.Contains(keyword));
    }

    private static string ExtractStringLiteral(string body, int quoteStartIndex)
    {
        if (body[quoteStartIndex] != '"')
            return "";

        int current = quoteStartIndex + 1;
        var sb = new StringBuilder();

        while (current < body.Length)
        {
            char c = body[current];
            if (c == '"')
            {
                if (current + 1 < body.Length && body[current + 1] == '"')
                {
                    // Comilla escapada -> agregar una comilla
                    sb.Append('"');
                    current += 2;
                    continue;
                }
                else
                {
                    // Fin de la cadena
                    break;
                }
            }
            else
            {
                sb.Append(c);
                current++;
            }
        }

        string rawSql = sb.ToString();
        return UnescapeMString(rawSql);
    }

    private static int SkipStringLiteral(string body, int quoteStartIndex)
    {
        if (body[quoteStartIndex] != '"')
            return quoteStartIndex + 1;

        int current = quoteStartIndex + 1;
        while (current < body.Length)
        {
            char c = body[current];
            if (c == '"')
            {
                if (current + 1 < body.Length && body[current + 1] == '"')
                {
                    // Comilla escapada
                    current += 2;
                    continue;
                }
                else
                {
                    // Fin de la cadena
                    return current + 1;
                }
            }
            else
            {
                current++;
            }
        }

        return current;
    }

    private static string NormalizeBody(string body)
    {
        // Remover comentarios de línea
        body = Regex.Replace(body, @"//.*$", "", RegexOptions.Multiline);

        // Remover comentarios de bloque
        body = Regex.Replace(body, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // Normalizar espacios en blanco pero preservar la estructura de strings
        return body.Trim();
    }

    private static string UnescapeMString(string raw)
    {
        return raw
            .Replace("#(lf)", "\n")
            .Replace("#(cr)", "\r")
            .Replace("#(tab)", "\t");
    }

    private static int IndexOfIgnoreCase(string source, string value, int startIndex)
    {
        return source.IndexOf(value, startIndex, StringComparison.OrdinalIgnoreCase);
    }
}