using System.Text;
using ControllerHub.Models;
using HidSharp;

namespace ControllerHub.Services;

/// <summary>
/// Base de lecture HID directe : thread de lecture, dernier état et texte du testeur.
/// Chaque manette décode son propre format de rapport dans <see cref="Parse"/>.
/// </summary>
public abstract class HidPadReader : IDisposable
{
    private HidStream? _stream;
    private Thread? _thread;
    private volatile bool _running;

    protected HidDevice Device { get; }

    /// <summary>Dernier état décodé, ou null tant qu'aucun rapport n'a été reçu.</summary>
    public PadState? Latest { get; protected set; }

    public string Transport { get; protected set; } = "inconnu";

    public string? Error { get; private set; }

    /// <summary>Premiers octets du dernier rapport reçu, pour diagnostiquer le décodage.</summary>
    public string LastReport { get; private set; } = "";

    protected HidPadReader(HidDevice device)
    {
        Device = device;
    }

    /// <summary>Décode un rapport reçu. Les rapports inconnus doivent être ignorés.</summary>
    protected abstract void Parse(byte[] report, int length);

    /// <summary>Commande envoyée une fois le périphérique ouvert, si la manette en a besoin.</summary>
    protected virtual void Initialize(HidStream stream)
    {
    }

    /// <summary>Libellé affiché dans le testeur pour une clé logique (A, B, LB, Haut…).</summary>
    protected virtual string Label(string key) => key;

    public void Start()
    {
        _stream = Device.Open();
        _stream.ReadTimeout = 500;
        Initialize(_stream);

        _running = true;
        _thread = new Thread(ReadLoop) { IsBackground = true, Name = GetType().Name };
        _thread.Start();
    }

    private void ReadLoop()
    {
        var buffer = new byte[Math.Max(64, Device.GetMaxInputReportLength())];

        while (_running)
        {
            try
            {
                int n = _stream!.Read(buffer, 0, buffer.Length);
                if (n > 0)
                {
                    LastReport = string.Join(" ", buffer.Take(Math.Min(n, 24)).Select(x => x.ToString("X2")));
                    Parse(buffer, n);
                }
            }
            catch (TimeoutException)
            {
                // Aucun rapport pendant 500 ms : on réessaie.
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                _running = false;
            }
        }
    }

    /// <summary>Convertit une position 0..255 (centre 128) en -1..1.</summary>
    protected static double Stick8(byte v) => Math.Clamp((v - 128) / 127.0, -1.0, 1.0);

    /// <summary>Nombre de rapports Bluetooth rejetés car leur CRC32 est invalide.</summary>
    public int CrcRejected { get; protected set; }

    /// <summary>
    /// Vérifie le CRC32 d'un rapport Bluetooth PlayStation (78 octets).
    /// Calcul comme le pilote Linux : graine 0xA1 suivie des 74 premiers octets,
    /// CRC inversé, stocké en little-endian dans les 4 derniers octets.
    /// </summary>
    protected static bool HasValidCrc32(byte[] b, int length)
    {
        const byte seed = 0xA1;
        int dataLength = length - 4;
        if (dataLength <= 0)
        {
            return false;
        }

        uint crc = 0xFFFFFFFF;
        crc = Crc32Step(crc, seed);
        for (int i = 0; i < dataLength; i++)
        {
            crc = Crc32Step(crc, b[i]);
        }
        crc = ~crc;

        return crc == System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(dataLength));
    }

    private static uint Crc32Step(uint crc, byte value)
    {
        crc ^= value;
        for (int k = 0; k < 8; k++)
        {
            crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }
        return crc;
    }

    /// <summary>Texte affiché dans le testeur.</summary>
    public string Describe()
    {
        var s = Latest;
        if (s is null)
        {
            return "En attente d'un rapport de la manette...";
        }

        var pressed = new List<string>();
        void Add(bool on, string key)
        {
            if (on)
            {
                pressed.Add(Label(key));
            }
        }

        Add(s.A, "A");
        Add(s.B, "B");
        Add(s.X, "X");
        Add(s.Y, "Y");
        Add(s.LeftShoulder, "LB");
        Add(s.RightShoulder, "RB");
        Add(s.LeftTrigger > 0.5, "LT");
        Add(s.RightTrigger > 0.5, "RT");
        Add(s.Back, "Back");
        Add(s.Start, "Start");
        Add(s.Guide, "Guide");
        Add(s.LeftThumb, "LS");
        Add(s.RightThumb, "RS");
        Add(s.DpadUp, "Haut");
        Add(s.DpadDown, "Bas");
        Add(s.DpadLeft, "Gauche");
        Add(s.DpadRight, "Droite");

        var sb = new StringBuilder();
        sb.AppendLine($"Transport : {Transport}");
        sb.AppendLine($"Rapport brut : {LastReport}");
        if (CrcRejected > 0)
        {
            sb.AppendLine($"Rapports Bluetooth rejetés (CRC invalide) : {CrcRejected}");
        }
        sb.AppendLine("Boutons : " + (pressed.Count > 0 ? string.Join(" + ", pressed) : "(aucun)"));
        sb.AppendLine($"Stick gauche : X {s.LeftX,6:+0.00;-0.00;0.00}   Y {s.LeftY,6:+0.00;-0.00;0.00}");
        sb.AppendLine($"Stick droit  : X {s.RightX,6:+0.00;-0.00;0.00}   Y {s.RightY,6:+0.00;-0.00;0.00}");
        sb.AppendLine($"Gâchettes    : L2 {s.LeftTrigger,4:0.00}   R2 {s.RightTrigger,4:0.00}");
        if (Error is not null)
        {
            sb.AppendLine("Erreur : " + Error);
        }
        return sb.ToString();
    }

    public void Dispose()
    {
        _running = false;
        _stream?.Dispose();
        _stream = null;
        _thread?.Join(1000);
    }
}
