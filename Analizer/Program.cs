using Microsoft.AnalysisServices.Tabular;
namespace Analizer;

class Program
{
    static void Main(string[] args)
    {
        string currentDir = Directory.GetCurrentDirectory();
        string? projectDir = FindProjectDirectory(currentDir);

        try
        {
            Console.WriteLine("Deserializando base de datos desde la carpeta data del proyecto...");

            // Obtener la ruta del directorio base del proyecto (donde está la carpeta data)
            // Desde el ejecutable navegamos hacia arriba hasta encontrar la carpeta del proyecto


            if (projectDir == null)
            {
                Console.WriteLine("Error: No se pudo encontrar el directorio del proyecto.");
                return;
            }

            string dataPath = Path.Combine(projectDir, "data");
            if (!Directory.Exists(dataPath))
            {
                Console.WriteLine($"Error: La carpeta data no exists en {dataPath}");
                return;
            }

            Console.WriteLine($"Cargando desde: {dataPath}");
            Database database = TmdlSerializer.DeserializeDatabaseFromFolder(dataPath);

            Console.WriteLine($"Base de datos cargada exitosamente: {database.Name}");
            Console.WriteLine($"Número de tablas: {database.Model.Tables.Count}");

            Report report = new Report(database);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al deserializar la base de datos: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    /// Busca el directorio del proyecto navegando hacia arriba desde el directorio actual
    /// hasta encontrar una carpeta que contenga la carpeta "data"
    /// </summary>
    /// <param name="startDirectory">Directorio desde donde empezar la búsqueda</param>
    /// <returns>La ruta del directorio del proyecto, o null si no se encuentra</returns>
    static string? FindProjectDirectory(string startDirectory)
    {
        DirectoryInfo? currentDir = new DirectoryInfo(startDirectory);

        while (currentDir != null)
        {
            // Buscar la carpeta "data" en el directorio actual
            string dataPath = Path.Combine(currentDir.FullName, "data");
            if (Directory.Exists(dataPath))
            {
                return currentDir.FullName;
            }

            // Subir un nivel en la jerarquía de directorios
            currentDir = currentDir.Parent;
        }

        return null;
    }
}