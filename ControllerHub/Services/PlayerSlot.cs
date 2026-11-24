using ControllerHub.Models;

namespace ControllerHub.Services;

/// <summary>
/// Un emplacement joueur (P1 à P4) : une manette physique et la manette Xbox 360 virtuelle associée.
/// </summary>
public sealed class PlayerSlot : IDisposable
{
    public PlayerSlot(int number)
    {
        Number = number;
    }

    public int Number { get; }

    /// <summary>Manette physique occupant ce joueur, ou null si l'emplacement est libre.</summary>
    public ControllerEntry? Entry { get; set; }

    /// <summary>Lecture HID directe propre à ce joueur (DualSense, DS4, Switch Pro).</summary>
    public DualSenseSession Session { get; } = new();

    /// <summary>Manette Xbox 360 virtuelle de ce joueur.</summary>
    public VirtualXbox360 Pad { get; } = new();

    /// <summary>Dernier état lu, utilisé par le testeur.</summary>
    public PadState? Latest { get; set; }

    /// <summary>Message d'état de la lecture (attente, erreur), affiché dans la barre de statut.</summary>
    public string? Reason { get; set; }

    public bool IsOccupied => Entry is not null;

    public string Label => Entry is null
        ? $"P{Number} — libre"
        : $"P{Number} — {ControllerDb.FamilyName(Entry.Family)}"
          + (Pad.IsConnected ? " (Xbox 360 active)" : string.Empty);

    /// <summary>Libère l'emplacement : ferme la lecture et supprime la manette virtuelle.</summary>
    public void Clear()
    {
        Session.Close();
        Pad.Disconnect();
        Entry = null;
        Latest = null;
        Reason = null;
    }

    public void Dispose()
    {
        Pad.Dispose();
        Session.Dispose();
    }
}
