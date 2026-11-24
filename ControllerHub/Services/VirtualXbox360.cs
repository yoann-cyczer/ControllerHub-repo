using ControllerHub.Models;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace ControllerHub.Services;

/// <summary>
/// Manette virtuelle Xbox 360 créée via le pilote ViGEmBus.
/// Le jeu voit cette manette comme une vraie Xbox 360.
/// </summary>
public sealed class VirtualXbox360 : IDisposable
{
    private ViGEmClient? _client;
    private IXbox360Controller? _pad;

    public bool IsConnected => _pad is not null;

    /// <summary>Crée la manette virtuelle. Lève une exception si ViGEmBus n'est pas installé.</summary>
    public void Connect()
    {
        if (_pad is not null)
        {
            return;
        }

        _client = new ViGEmClient();
        _pad = _client.CreateXbox360Controller();
        _pad.Connect();
    }

    public void Disconnect()
    {
        _pad?.Disconnect();
        _pad = null;
        _client?.Dispose();
        _client = null;
    }

    /// <summary>Envoie un état complet à la manette virtuelle.</summary>
    public void Send(PadState s)
    {
        if (_pad is null)
        {
            return;
        }

        _pad.SetButtonState(Xbox360Button.A, s.A);
        _pad.SetButtonState(Xbox360Button.B, s.B);
        _pad.SetButtonState(Xbox360Button.X, s.X);
        _pad.SetButtonState(Xbox360Button.Y, s.Y);
        _pad.SetButtonState(Xbox360Button.LeftShoulder, s.LeftShoulder);
        _pad.SetButtonState(Xbox360Button.RightShoulder, s.RightShoulder);
        _pad.SetButtonState(Xbox360Button.Back, s.Back);
        _pad.SetButtonState(Xbox360Button.Start, s.Start);
        _pad.SetButtonState(Xbox360Button.Guide, s.Guide);
        _pad.SetButtonState(Xbox360Button.LeftThumb, s.LeftThumb);
        _pad.SetButtonState(Xbox360Button.RightThumb, s.RightThumb);
        _pad.SetButtonState(Xbox360Button.Up, s.DpadUp);
        _pad.SetButtonState(Xbox360Button.Down, s.DpadDown);
        _pad.SetButtonState(Xbox360Button.Left, s.DpadLeft);
        _pad.SetButtonState(Xbox360Button.Right, s.DpadRight);

        _pad.SetAxisValue(Xbox360Axis.LeftThumbX, ToAxis(s.LeftX));
        _pad.SetAxisValue(Xbox360Axis.LeftThumbY, ToAxis(s.LeftY));
        _pad.SetAxisValue(Xbox360Axis.RightThumbX, ToAxis(s.RightX));
        _pad.SetAxisValue(Xbox360Axis.RightThumbY, ToAxis(s.RightY));

        _pad.SetSliderValue(Xbox360Slider.LeftTrigger, ToTrigger(s.LeftTrigger));
        _pad.SetSliderValue(Xbox360Slider.RightTrigger, ToTrigger(s.RightTrigger));

        _pad.SubmitReport();
    }

    private static short ToAxis(double v) =>
        (short)Math.Clamp(Math.Round(v * 32767.0), -32768.0, 32767.0);

    private static byte ToTrigger(double v) =>
        (byte)Math.Clamp(Math.Round(v * 255.0), 0.0, 255.0);

    public void Dispose() => Disconnect();
}
