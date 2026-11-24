using ControllerHub.Models;
using Windows.Gaming.Input;

namespace ControllerHub.Services;

/// <summary>Convertit la lecture d'une manette en PadState, avec le mappage si nécessaire.</summary>
public static class PadMapper
{
    public static PadState? Read(ControllerEntry entry, IReadOnlyList<MappingProfile> profiles, out string? reason)
    {
        reason = null;

        try
        {
            if (entry.Gamepad is { } gamepad)
            {
                return FromGamepad(gamepad.GetCurrentReading());
            }

            if (entry.Raw is { } raw)
            {
                var id = $"{entry.Vid:X4}:{entry.Pid:X4}";
                var profile = profiles.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

                if (profile is null || profile.Buttons.Count == 0)
                {
                    reason = $"Mappage à compléter pour {id} dans mappings.json.";
                    return null;
                }

                return FromRaw(raw, profile);
            }

            reason = "Manette non prise en charge.";
            return null;
        }
        catch (Exception)
        {
            reason = "Lecture impossible : la manette a peut-être été déconnectée.";
            return null;
        }
    }

    private static PadState FromGamepad(GamepadReading r)
    {
        var b = r.Buttons;
        return new PadState
        {
            A = b.HasFlag(GamepadButtons.A),
            B = b.HasFlag(GamepadButtons.B),
            X = b.HasFlag(GamepadButtons.X),
            Y = b.HasFlag(GamepadButtons.Y),
            LeftShoulder = b.HasFlag(GamepadButtons.LeftShoulder),
            RightShoulder = b.HasFlag(GamepadButtons.RightShoulder),
            Back = b.HasFlag(GamepadButtons.View),
            Start = b.HasFlag(GamepadButtons.Menu),
            LeftThumb = b.HasFlag(GamepadButtons.LeftThumbstick),
            RightThumb = b.HasFlag(GamepadButtons.RightThumbstick),
            DpadUp = b.HasFlag(GamepadButtons.DPadUp),
            DpadDown = b.HasFlag(GamepadButtons.DPadDown),
            DpadLeft = b.HasFlag(GamepadButtons.DPadLeft),
            DpadRight = b.HasFlag(GamepadButtons.DPadRight),
            LeftX = r.LeftThumbstickX,
            LeftY = r.LeftThumbstickY,
            RightX = r.RightThumbstickX,
            RightY = r.RightThumbstickY,
            LeftTrigger = r.LeftTrigger,
            RightTrigger = r.RightTrigger,
        };
    }

    private static PadState FromRaw(RawGameController raw, MappingProfile p)
    {
        var buttons = new bool[raw.ButtonCount];
        var switches = new GameControllerSwitchPosition[raw.SwitchCount];
        var axes = new double[raw.AxisCount];
        raw.GetCurrentReading(buttons, switches, axes);

        bool Btn(string key) =>
            p.Buttons.TryGetValue(key, out var i) && i >= 0 && i < buttons.Length && buttons[i];

        double Axis(string key)
        {
            if (!p.Axes.TryGetValue(key, out var i) || i < 0 || i >= axes.Length)
            {
                return 0;
            }

            var v = axes[i];
            return p.InvertedAxes.Contains(key) ? -v : v;
        }

        double Trigger(string key)
        {
            // Gâchette remontée comme bouton numérique (pas d'axe défini) : 1 si pressée, sinon 0.
            if (!p.Axes.ContainsKey(key))
            {
                return Btn(key) ? 1.0 : 0.0;
            }

            var v = Axis(key);
            if (p.SignedTriggers)
            {
                v = (v + 1) / 2;
            }
            return Math.Clamp(v, 0, 1);
        }

        return new PadState
        {
            A = Btn("A"),
            B = Btn("B"),
            X = Btn("X"),
            Y = Btn("Y"),
            LeftShoulder = Btn("LB"),
            RightShoulder = Btn("RB"),
            Back = Btn("Back"),
            Start = Btn("Start"),
            Guide = Btn("Guide"),
            LeftThumb = Btn("LS"),
            RightThumb = Btn("RS"),
            DpadUp = Btn("Up"),
            DpadDown = Btn("Down"),
            DpadLeft = Btn("Left"),
            DpadRight = Btn("Right"),
            LeftX = Math.Clamp(Axis("LX"), -1, 1),
            LeftY = Math.Clamp(Axis("LY"), -1, 1),
            RightX = Math.Clamp(Axis("RX"), -1, 1),
            RightY = Math.Clamp(Axis("RY"), -1, 1),
            LeftTrigger = Trigger("LT"),
            RightTrigger = Trigger("RT"),
        };
    }
}
