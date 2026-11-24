using System.IO;
using System.Text.Json;
using ControllerHub.Models;

namespace ControllerHub.Services;

/// <summary>Charge les profils de mappage depuis mappings.json (à côté de l'exécutable).</summary>
public static class MappingStore
{
    public const string FileName = "mappings.json";

    public static List<MappingProfile> Load(out string? error)
    {
        error = null;
        var path = Path.Combine(AppContext.BaseDirectory, FileName);
        if (!File.Exists(path))
        {
            error = $"{FileName} introuvable.";
            return new List<MappingProfile>();
        }

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<MappingProfile>>(File.ReadAllText(path), options)
                   ?? new List<MappingProfile>();
        }
        catch (Exception ex)
        {
            error = $"{FileName} invalide : {ex.Message}";
            return new List<MappingProfile>();
        }
    }
}
