using ControllerHub.Models;
using HidSharp;

namespace ControllerHub.Services;

/// <summary>
/// Lecture HID directe de la Switch Pro Controller.
/// Passe la manette en mode « rapport complet » (0x30) au démarrage pour recevoir les sticks.
/// Les boutons sont placés comme sur une manette Xbox : A est en bas, B à droite, etc.
/// </summary>
public sealed class SwitchProReader : HidPadReader
{
    public SwitchProReader(HidDevice device) : base(device)
    {
    }

    protected override void Initialize(HidStream stream)
    {
        // Rapport de sortie 0x01 : sous-commande 0x03 « mode d'entrée », argument 0x30 (rapport complet).
        int length = Math.Max(49, Device.GetMaxOutputReportLength());
        var report = new byte[length];
        report[0] = 0x01;     // identifiant du rapport de sortie
        report[1] = 0x00;     // compteur de paquets
        report[2] = 0x00;     // vibration neutre, moteur gauche
        report[3] = 0x01;
        report[4] = 0x40;
        report[5] = 0x40;
        report[6] = 0x00;     // vibration neutre, moteur droit
        report[7] = 0x01;
        report[8] = 0x40;
        report[9] = 0x40;
        report[10] = 0x03;    // sous-commande : mode d'entrée
        report[11] = 0x30;    // rapport complet

        stream.Write(report, 0, report.Length);
    }

    protected override void Parse(byte[] b, int length)
    {
        if (b[0] == 0x30 && length >= 12)
        {
            Transport = "HID (rapport complet)";

            // Sticks sur 12 bits (centre 2048), packés sur les octets 6 à 11.
            double Raw12(int v) => Math.Clamp((v - 2048) / 1800.0, -1.0, 1.0);

            Decode(
                face: b[3],
                misc: b[4],
                dir: b[5],
                lx: Raw12(b[6] | ((b[7] & 0x0F) << 8)),
                ly: Raw12((b[7] >> 4) | (b[8] << 4)),
                rx: Raw12(b[9] | ((b[10] & 0x0F) << 8)),
                ry: Raw12((b[10] >> 4) | (b[11] << 4)));
        }
        else if (b[0] == 0x3F && length >= 4)
        {
            // Rapport simple : boutons et croix uniquement, sans sticks.
            Transport = "HID (rapport simple)";

            int hat = b[3] & 0x0F;
            int dir = 0;
            if (hat is 0 or 1 or 7) dir |= 0x02;   // haut
            if (hat is 1 or 2 or 3) dir |= 0x04;   // droite
            if (hat is 3 or 4 or 5) dir |= 0x01;   // bas
            if (hat is 5 or 6 or 7) dir |= 0x08;   // gauche

            Decode(face: b[1], misc: b[2], dir: dir, lx: 0, ly: 0, rx: 0, ry: 0);
        }
    }

    /// <summary>
    /// face : Y=0x01, X=0x02, B=0x04, A=0x08, R=0x40, ZR=0x80 (disposition Nintendo).
    /// misc : Moins=0x01, Plus=0x02, RS=0x04, LS=0x08, Home=0x10.
    /// dir  : bas=0x01, haut=0x02, droite=0x04, gauche=0x08, L=0x40, ZL=0x80.
    /// </summary>
    private void Decode(int face, int misc, int dir, double lx, double ly, double rx, double ry)
    {
        Latest = new PadState
        {
            // Placement Xbox : le bouton du bas (Nintendo B) devient A, celui de droite (Nintendo A) devient B.
            A = (face & 0x04) != 0,
            B = (face & 0x08) != 0,
            X = (face & 0x01) != 0,   // Nintendo Y (gauche)
            Y = (face & 0x02) != 0,   // Nintendo X (haut)

            RightShoulder = (face & 0x40) != 0,   // R
            RightTrigger = (face & 0x80) != 0 ? 1.0 : 0.0,   // ZR (bouton)
            LeftShoulder = (dir & 0x40) != 0,     // L
            LeftTrigger = (dir & 0x80) != 0 ? 1.0 : 0.0,     // ZL (bouton)

            Back = (misc & 0x01) != 0,            // Moins
            Start = (misc & 0x02) != 0,           // Plus
            RightThumb = (misc & 0x04) != 0,      // RS
            LeftThumb = (misc & 0x08) != 0,       // LS
            Guide = (misc & 0x10) != 0,           // Home

            DpadDown = (dir & 0x01) != 0,
            DpadUp = (dir & 0x02) != 0,
            DpadRight = (dir & 0x04) != 0,
            DpadLeft = (dir & 0x08) != 0,

            LeftX = lx,
            LeftY = ly,
            RightX = rx,
            RightY = ry,
        };
    }

    protected override string Label(string key) => key switch
    {
        "A" => "B (bas)",
        "B" => "A (droite)",
        "X" => "Y (gauche)",
        "Y" => "X (haut)",
        "LB" => "L",
        "RB" => "R",
        "LT" => "ZL",
        "RT" => "ZR",
        "Back" => "Moins",
        "Start" => "Plus",
        "Guide" => "Home",
        "LS" => "Stick gauche",
        "RS" => "Stick droit",
        _ => key
    };
}
