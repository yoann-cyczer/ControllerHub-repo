using ControllerHub.Models;
using HidSharp;

namespace ControllerHub.Services;

/// <summary>
/// Lecture HID directe de la DualSense (USB ou Bluetooth).
/// Donne la croix directionnelle, les sticks et les gâchettes en valeurs exactes.
/// </summary>
public sealed class DualSenseReader : PlayStationReaderBase
{
    // Décalage ajouté aux positions USB quand la manette est en Bluetooth (rapport 0x31).
    // Non vérifié sur matériel : à ajuster si les sticks ne répondent pas en Bluetooth.
    private const int BluetoothOffset = 1;

    public DualSenseReader(HidDevice device) : base(device)
    {
    }

    protected override void Parse(byte[] b, int length)
    {
        int offset;
        if (b[0] == 0x01)
        {
            Transport = "USB";
            offset = 0;
        }
        else if (b[0] == 0x31)
        {
            Transport = "Bluetooth";
            // Rapport Bluetooth complet de 78 octets, protégé par un CRC32.
            if (length < 78 || !HasValidCrc32(b, length))
            {
                CrcRejected++;
                return;
            }
            offset = BluetoothOffset;
        }
        else
        {
            return;
        }

        // Positions USB (0 = identifiant du rapport) :
        // 1 LX, 2 LY, 3 RX, 4 RY, 5 L2, 6 R2, 8 hat/faces, 9 épaules/options/sticks, 10 PS.
        if (length < offset + 11)
        {
            return;
        }

        byte At(int i) => b[offset + i];

        var s = new PadState
        {
            X = (At(8) & 0x10) != 0,   // Carré
            A = (At(8) & 0x20) != 0,   // Croix
            B = (At(8) & 0x40) != 0,   // Rond
            Y = (At(8) & 0x80) != 0,   // Triangle

            LeftShoulder = (At(9) & 0x01) != 0,    // L1
            RightShoulder = (At(9) & 0x02) != 0,   // R1
            Back = (At(9) & 0x10) != 0,            // Create
            Start = (At(9) & 0x20) != 0,           // Options
            LeftThumb = (At(9) & 0x40) != 0,       // L3
            RightThumb = (At(9) & 0x80) != 0,      // R3
            Guide = (At(10) & 0x01) != 0,          // PS

            LeftX = Stick8(At(1)),
            LeftY = -Stick8(At(2)),   // haut = valeur basse sur la DualSense, on inverse
            RightX = Stick8(At(3)),
            RightY = -Stick8(At(4)),

            LeftTrigger = At(5) / 255.0,
            RightTrigger = At(6) / 255.0,
        };

        ApplyHat(s, At(8) & 0x0F);
        Latest = s;
    }
}
