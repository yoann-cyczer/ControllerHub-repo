using ControllerHub.Models;

namespace ControllerHub.Services;

/// <summary>
/// Gère jusqu'à quatre joueurs. Chaque joueur a sa propre manette physique
/// et sa propre manette Xbox 360 virtuelle, ce qui permet de jouer à plusieurs en même temps.
/// </summary>
public sealed class PlayerManager : IDisposable
{
    public const int MaxPlayers = 4;

    private readonly List<MappingProfile> _profiles;
    private readonly PlayerSlot[] _slots;

    public PlayerManager(List<MappingProfile> profiles)
    {
        _profiles = profiles;
        _slots = Enumerable.Range(1, MaxPlayers).Select(n => new PlayerSlot(n)).ToArray();
    }

    public IReadOnlyList<PlayerSlot> Slots => _slots;

    /// <summary>Vrai quand la conversion Xbox 360 est demandée pour tous les joueurs.</summary>
    public bool ConversionEnabled { get; private set; }

    public PlayerSlot? FindSlot(string entryId) => _slots.FirstOrDefault(s => s.Entry?.Id == entryId);

    /// <summary>Place la manette dans le premier joueur libre. Connecte son pad si la conversion est active.</summary>
    public PlayerSlot? Assign(ControllerEntry entry, out string? error)
    {
        error = null;

        if (FindSlot(entry.Id) is not null)
        {
            error = "Cette manette est déjà attribuée à un joueur.";
            return null;
        }

        var slot = _slots.FirstOrDefault(s => !s.IsOccupied);
        if (slot is null)
        {
            error = $"Les {MaxPlayers} joueurs sont déjà attribués.";
            return null;
        }

        slot.Entry = entry;

        if (ConversionEnabled)
        {
            TryConnect(slot, out error);
        }

        return slot;
    }

    /// <summary>Retire un joueur et supprime sa manette virtuelle.</summary>
    public void Unassign(PlayerSlot slot) => slot.Clear();

    /// <summary>Libère les joueurs dont la manette n'est plus branchée. Renvoie vrai si quelque chose a changé.</summary>
    public bool Prune(IEnumerable<string> presentIds)
    {
        var ids = presentIds.ToHashSet();
        bool changed = false;

        foreach (var slot in _slots)
        {
            if (slot.Entry is { } entry && !ids.Contains(entry.Id))
            {
                slot.Clear();
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>
    /// Active ou supprime la conversion Xbox 360 pour tous les joueurs occupés.
    /// Renvoie le premier message d'erreur rencontré, ou null si tout s'est bien passé.
    /// </summary>
    public string? SetConversion(bool enabled)
    {
        string? firstError = null;

        if (enabled)
        {
            ConversionEnabled = true;
            foreach (var slot in _slots.Where(s => s.IsOccupied))
            {
                if (!TryConnect(slot, out var error))
                {
                    firstError ??= error;
                }
            }
            return firstError;
        }

        ConversionEnabled = false;
        foreach (var slot in _slots)
        {
            slot.Pad.Disconnect();
        }
        return null;
    }

    private static bool TryConnect(PlayerSlot slot, out string? error)
    {
        error = null;
        try
        {
            slot.Pad.Connect();
            return true;
        }
        catch (Exception ex)
        {
            error = $"Joueur {slot.Number} : {ex.Message}";
            return false;
        }
    }

    /// <summary>Lit l'état de chaque joueur occupé et l'envoie à sa manette virtuelle.</summary>
    public void Tick()
    {
        foreach (var slot in _slots)
        {
            if (slot.Entry is not { } entry)
            {
                continue;
            }

            var state = ReadState(entry, slot.Session, _profiles, out var reason);
            slot.Latest = state;
            slot.Reason = reason;

            if (state is not null && slot.Pad.IsConnected)
            {
                slot.Pad.Send(state);
            }
        }
    }

    /// <summary>
    /// État normalisé d'une manette. Les DualSense, DS4 et Switch Pro passent par la lecture HID directe
    /// (session fournie), les autres par XInput ou le mappage RawGameController.
    /// </summary>
    public static PadState? ReadState(ControllerEntry entry, DualSenseSession session, List<MappingProfile> profiles, out string? reason)
    {
        reason = null;

        if (DualSenseSession.Supports(entry.Family))
        {
            var reader = session.GetOrOpen(entry, out var error);
            if (reader is null)
            {
                reason = error;
                return null;
            }

            if (reader.Latest is null)
            {
                reason = "En attente d'un rapport de la manette...";
            }
            return reader.Latest;
        }

        return PadMapper.Read(entry, profiles, out reason);
    }

    public void Dispose()
    {
        foreach (var slot in _slots)
        {
            slot.Dispose();
        }
    }
}
