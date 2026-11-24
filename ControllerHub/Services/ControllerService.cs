using System.Text;
using ControllerHub.Models;
using Windows.Gaming.Input;

namespace ControllerHub.Services;

/// <summary>
/// Énumère les manettes connectées (XInput via Gamepad, HID via RawGameController)
/// et fournit leur état en direct.
/// </summary>
public sealed class ControllerService
{
    private const ushort VendorMicrosoft = 0x045E;

    public List<ControllerEntry> Scan()
    {
        var result = new List<ControllerEntry>();

        // 1) Manettes XInput (Xbox et manettes qui se présentent comme telles).
        var pads = Gamepad.Gamepads;
        for (int i = 0; i < pads.Count; i++)
        {
            result.Add(new ControllerEntry
            {
                Id = $"xinput-{i}",
                Name = "Manette Xbox (XInput)",
                Family = ControllerFamily.Xbox,
                Connection = "XInput",
                Gamepad = pads[i],
            });
        }

        // 2) Manettes HID (PlayStation, Switch, Joy-Con, génériques).
        foreach (var raw in RawGameController.RawGameControllers)
        {
            // Les manettes Microsoft sont déjà couvertes par XInput : on évite les doublons.
            if (raw.HardwareVendorId == VendorMicrosoft)
            {
                continue;
            }

            var family = ControllerDb.Identify(raw.HardwareVendorId, raw.HardwareProductId, out var label);
            result.Add(new ControllerEntry
            {
                Id = "hid-" + raw.NonRoamableId,
                Name = label,
                Family = family,
                Connection = "HID (USB ou Bluetooth)",
                Vid = raw.HardwareVendorId,
                Pid = raw.HardwareProductId,
                Raw = raw,
            });
        }

        // 3) Regroupement des Joy-Con : gauche + droit détectés => affichés comme une paire.
        bool hasLeft = result.Any(e => e.Family == ControllerFamily.JoyConLeft);
        bool hasRight = result.Any(e => e.Family == ControllerFamily.JoyConRight);
        if (hasLeft && hasRight)
        {
            foreach (var entry in result.Where(e => e.Family is ControllerFamily.JoyConLeft or ControllerFamily.JoyConRight))
            {
                entry.InJoyConPair = true;
            }
        }

        return result;
    }

    /// <summary>Retourne un texte décrivant l'état actuel de la manette.</summary>
    public string ReadSnapshot(ControllerEntry entry)
    {
        try
        {
            if (entry.Gamepad is { } gamepad)
            {
                var r = gamepad.GetCurrentReading();
                var sb = new StringBuilder();
                sb.AppendLine($"Boutons : {FormatButtons(r.Buttons)}");
                sb.AppendLine($"Stick gauche : X {r.LeftThumbstickX,6:+0.00;-0.00;0.00}   Y {r.LeftThumbstickY,6:+0.00;-0.00;0.00}");
                sb.AppendLine($"Stick droit  : X {r.RightThumbstickX,6:+0.00;-0.00;0.00}   Y {r.RightThumbstickY,6:+0.00;-0.00;0.00}");
                sb.AppendLine($"Gâchettes    : gauche {r.LeftTrigger,4:0.00}   droite {r.RightTrigger,4:0.00}");
                return sb.ToString();
            }

            if (entry.Raw is { } raw)
            {
                var buttons = new bool[raw.ButtonCount];
                var switches = new GameControllerSwitchPosition[raw.SwitchCount];
                var axes = new double[raw.AxisCount];
                raw.GetCurrentReading(buttons, switches, axes);

                var pressed = Enumerable.Range(0, buttons.Length)
                    .Where(i => buttons[i])
                    .Select(i => $"B{i}")
                    .ToList();

                var sb = new StringBuilder();
                sb.AppendLine("Boutons : " + (pressed.Count > 0 ? string.Join(" ", pressed) : "(aucun)"));
                for (int i = 0; i < axes.Length; i++)
                {
                    sb.AppendLine($"Axe {i} : {axes[i],7:+0.000;-0.000;0.000}");
                }
                sb.AppendLine("Note : la numérotation des boutons HID dépend du modèle ; " +
                              "appuyez sur chaque bouton pour l'identifier.");
                return sb.ToString();
            }

            return "Pas de données disponibles.";
        }
        catch (Exception)
        {
            return "Lecture impossible : la manette a peut-être été déconnectée.";
        }
    }

    /// <summary>Fait vibrer une manette XInput pendant une courte durée (test).</summary>
    public async Task VibrateAsync(ControllerEntry entry, int milliseconds = 600)
    {
        if (entry.Gamepad is not { } gamepad)
        {
            return;
        }

        gamepad.Vibration = new GamepadVibration { LeftMotor = 1.0, RightMotor = 1.0 };
        await Task.Delay(milliseconds);
        gamepad.Vibration = new GamepadVibration();
    }

    private static string FormatButtons(GamepadButtons buttons) =>
        buttons == GamepadButtons.None
            ? "(aucun)"
            : buttons.ToString().Replace(", ", " + ");
}
