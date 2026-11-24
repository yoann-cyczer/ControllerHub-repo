using ControllerHub.Models;
using HidSharp;

namespace ControllerHub.Services;

/// <summary>
/// Lecture HID directe de la DualShock 4 (USB ou Bluetooth).
/// </summary>
public sealed class DualShock4Reader : PlayStationReaderBase
{
    // Décalage ajouté aux positions USB quand la manette est en Bluetooth (rapport 0x11).
    // Non vérifié sur matériel : à ajuster si les sticks ne répondent pas en Bluetooth.
    private const int BluetoothOffset = 2;

    public DualShock4Reader(HidDevice device) : base(device)
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
        else if (b[0] == 0x11)
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
        // 1 LX, 2 LY, 3 RX, 4 RY, 5 hat/faces, 6 épaules/options/sticks, 7 PS, 8 L2, 9 R2.
        if (length < offset + 10)
        {
            return;
        }

        byte At(int i) => b[offset + i];

        var s = new PadState
        {
            X = (At(5) & 0x10) != 0,   // Carré
            A = (At(5) & 0x20) != 0,   // Croix
            B = (At(5) & 0x40) != 0,   // Rond
            Y = (At(5) & 0x80) != 0,   // Triangle

            LeftShoulder = (At(6) & 0x01) != 0,    // L1
            RightShoulder = (At(6) & 0x02) != 0,   // R1
            Back = (At(6) & 0x10) != 0,            // Share
            Start = (At(6) & 0x20) != 0,           // Options
            LeftThumb = (At(6) & 0x40) != 0,       // L3
            RightThumb = (At(6) & 0x80) != 0,      // R3
            Guide = (At(7) & 0x01) != 0,           // PS

            LeftX = Stick8(At(1)),
            LeftY = -Stick8(At(2)),   // haut = valeur basse sur la DS4, on inverse
            RightX = Stick8(At(3)),
            RightY = -Stick8(At(4)),

            LeftTrigger = At(8) / 255.0,
            RightTrigger = At(9) / 255.0,
        };

        ApplyHat(s, At(5) & 0x0F);
        Latest = s;
    }
}
