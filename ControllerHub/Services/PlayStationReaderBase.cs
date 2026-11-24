using ControllerHub.Models;
using HidSharp;

namespace ControllerHub.Services;

/// <summary>Base commune aux manettes PlayStation (libellés et croix directionnelle sur hat).</summary>
public abstract class PlayStationReaderBase : HidPadReader
{
    protected PlayStationReaderBase(HidDevice device) : base(device)
    {
    }

    /// <summary>Hat PlayStation : 0 = haut, sens horaire, 8 ou plus = neutre.</summary>
    protected static void ApplyHat(PadState s, int hat)
    {
        s.DpadUp = hat is 0 or 1 or 7;
        s.DpadRight = hat is 1 or 2 or 3;
        s.DpadDown = hat is 3 or 4 or 5;
        s.DpadLeft = hat is 5 or 6 or 7;
    }

    protected override string Label(string key) => key switch
    {
        "A" => "Croix",
        "B" => "Rond",
        "X" => "Carré",
        "Y" => "Triangle",
        "LB" => "L1",
        "RB" => "R1",
        "LT" => "L2",
        "RT" => "R2",
        "Back" => "Create",
        "Start" => "Options",
        "Guide" => "PS",
        "LS" => "L3",
        "RS" => "R3",
        _ => key
    };
}
