namespace ControllerHub.Models;

/// <summary>
/// Correspondance entre les index bruts HID d'un modèle et les boutons logiques.
/// Chargée depuis mappings.json à côté de l'exécutable.
/// </summary>
public sealed class MappingProfile
{
    /// <summary>VID:PID en hexadécimal, par exemple "054C:0CE6".</summary>
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    /// <summary>
    /// Clés : A, B, X, Y, LB, RB, Back, Start, Guide, LS, RS, Up, Down, Left, Right.
    /// Valeur : index du bouton brut (B0, B1, …) affiché par le testeur.
    /// </summary>
    public Dictionary<string, int> Buttons { get; set; } = new();

    /// <summary>
    /// Clés : LX, LY, RX, RY, LT, RT. Valeur : index de l'axe brut affiché par le testeur.
    /// </summary>
    public Dictionary<string, int> Axes { get; set; } = new();

    /// <summary>Axes à inverser, par exemple ["LY", "RY"] si haut donne une valeur négative.</summary>
    public List<string> InvertedAxes { get; set; } = new();

    /// <summary>Vrai si les gâchettes vont de -1 à 1 au lieu de 0 à 1.</summary>
    public bool SignedTriggers { get; set; }
}
