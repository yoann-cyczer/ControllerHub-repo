using ControllerHub.Models;
using Windows.Gaming.Input;

namespace ControllerHub.Models;

/// <summary>Une manette détectée, quelle que soit l'API utilisée pour la lire.</summary>
public sealed class ControllerEntry
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ControllerFamily Family { get; init; }
    public required string Connection { get; init; }

    public ushort Vid { get; init; }
    public ushort Pid { get; init; }

    /// <summary>Renseigné pour les manettes XInput (Xbox) : lecture standardisée.</summary>
    public Gamepad? Gamepad { get; init; }

    /// <summary>Renseigné pour les manettes HID génériques : lecture brute (index de boutons).</summary>
    public RawGameController? Raw { get; init; }

    /// <summary>Vrai lorsque les deux Joy-Con (gauche et droit) sont détectés ensemble.</summary>
    public bool InJoyConPair { get; set; }

    public string Title => InJoyConPair ? $"{Name} (paire)" : Name;
}
