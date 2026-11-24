using ControllerHub.Models;
using HidSharp;

namespace ControllerHub.Services;

/// <summary>
/// Garde ouverte une seule lecture HID directe, celle de la manette sélectionnée.
/// Couvre la DualSense, la DualShock 4 et la Switch Pro.
/// Évite de rouvrir le périphérique HID à chaque rafraîchissement.
/// </summary>
public sealed class DualSenseSession : IDisposable
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    private HidPadReader? _reader;
    private string? _entryId;
    private string? _failedId;
    private DateTime _failedAt = DateTime.MinValue;

    public HidPadReader? Current => _reader;

    /// <summary>Vrai si la manette est lue directement en HID (et non via RawGameController).</summary>
    public static bool Supports(ControllerFamily family) =>
        family is ControllerFamily.Ps5 or ControllerFamily.Ps5Edge or ControllerFamily.Ps4 or ControllerFamily.SwitchPro;

    /// <summary>Retourne la lecture de la manette, en l'ouvrant si nécessaire.</summary>
    public HidPadReader? GetOrOpen(ControllerEntry entry, out string? error)
    {
        error = null;

        if (_reader is not null && _entryId == entry.Id && _reader.Error is null)
        {
            return _reader;
        }

        // Après un échec, on attend un peu avant de réessayer pour ne pas saturer le système.
        if (_failedId == entry.Id && DateTime.UtcNow - _failedAt < RetryDelay)
        {
            error = "Ouverture HID en attente de nouvelle tentative.";
            return null;
        }

        Close();

        // Plusieurs interfaces HID peuvent exister : on prend celle qui a le plus grand rapport
        // (le rapport complet de la manette).
        var device = DeviceList.Local
            .GetHidDevices(entry.Vid, entry.Pid)
            .OrderByDescending(d => d.GetMaxInputReportLength())
            .FirstOrDefault();

        if (device is null)
        {
            error = "Périphérique HID introuvable (débranché ?).";
            Remember(entry.Id);
            return null;
        }

        try
        {
            var reader = Create(entry.Family, device);
            reader.Start();
            _reader = reader;
            _entryId = entry.Id;
            _failedId = null;
            return reader;
        }
        catch (Exception ex)
        {
            error = $"Ouverture HID impossible : {ex.Message}";
            Remember(entry.Id);
            return null;
        }
    }

    private static HidPadReader Create(ControllerFamily family, HidDevice device) => family switch
    {
        ControllerFamily.Ps4 => new DualShock4Reader(device),
        ControllerFamily.SwitchPro => new SwitchProReader(device),
        _ => new DualSenseReader(device),
    };

    private void Remember(string id)
    {
        _failedId = id;
        _failedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        _reader?.Dispose();
        _reader = null;
        _entryId = null;
    }

    public void Dispose() => Close();
}
