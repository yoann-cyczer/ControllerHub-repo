namespace ControllerHub.Models;

/// <summary>État normalisé d'une manette, indépendant de sa marque.</summary>
public sealed class PadState
{
    public bool A { get; set; }
    public bool B { get; set; }
    public bool X { get; set; }
    public bool Y { get; set; }
    public bool LeftShoulder { get; set; }
    public bool RightShoulder { get; set; }
    public bool Back { get; set; }
    public bool Start { get; set; }
    public bool Guide { get; set; }
    public bool LeftThumb { get; set; }
    public bool RightThumb { get; set; }
    public bool DpadUp { get; set; }
    public bool DpadDown { get; set; }
    public bool DpadLeft { get; set; }
    public bool DpadRight { get; set; }

    /// <summary>Sticks : de -1 à 1 (haut = positif pour Y).</summary>
    public double LeftX { get; set; }
    public double LeftY { get; set; }
    public double RightX { get; set; }
    public double RightY { get; set; }

    /// <summary>Gâchettes : de 0 à 1.</summary>
    public double LeftTrigger { get; set; }
    public double RightTrigger { get; set; }
}
