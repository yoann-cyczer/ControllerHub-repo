using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ControllerHub.Models;
using ControllerHub.Services;

namespace ControllerHub;

public partial class MainWindow : Window
{
    private readonly ControllerService _service = new();
    private readonly DispatcherTimer _scanTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _readTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };

    private readonly VirtualXbox360 _virtualPad = new();
    private readonly List<MappingProfile> _profiles = LoadProfiles();

    private ControllerEntry? _selected;
    private List<string> _knownIds = new();

    public MainWindow()
    {
        InitializeComponent();

        _scanTimer.Tick += (_, _) => Rescan();
        _readTimer.Tick += (_, _) => UpdateTester();

        Loaded += (_, _) =>
        {
            Rescan();
            _scanTimer.Start();
            _readTimer.Start();
        };
        Closed += (_, _) =>
        {
            _scanTimer.Stop();
            _readTimer.Stop();
            _virtualPad.Dispose();
        };
    }

    private static List<MappingProfile> LoadProfiles()
    {
        var profiles = MappingStore.Load(out var error);
        if (error is not null)
        {
            System.Diagnostics.Debug.WriteLine(error);
        }
        return profiles;
    }

    /// <summary>Recherche les manettes et ne reconstruit la liste que si elle a changé.</summary>
    private void Rescan()
    {
        var entries = _service.Scan();
        var ids = entries.Select(e => e.Id).ToList();

        if (ids.SequenceEqual(_knownIds))
        {
            return;
        }

        _knownIds = ids;
        var previousId = _selected?.Id;

        ControllerList.ItemsSource = entries;

        // Conserve la sélection si la manette est toujours là.
        var match = entries.FirstOrDefault(e => e.Id == previousId) ?? entries.FirstOrDefault();
        _selected = match;
        ControllerList.SelectedItem = match;

        StatusText.Text = entries.Count switch
        {
            0 => "Aucune manette détectée",
            1 => "1 manette détectée",
            _ => $"{entries.Count} manettes détectées"
        };
    }

    private void ControllerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selected = ControllerList.SelectedItem as ControllerEntry;

        if (_selected is null)
        {
            TitleText.Text = "Aucune manette sélectionnée";
            DetailText.Text = "Branchez une manette en USB ou en Bluetooth.";
            PairingText.Text = string.Empty;
            TesterText.Text = string.Empty;
            return;
        }

        TitleText.Text = ControllerDb.FamilyName(_selected.Family);
        var vidPid = _selected.Vid == 0 && _selected.Pid == 0
            ? "—"
            : $"{_selected.Vid:X4}:{_selected.Pid:X4}";
        DetailText.Text = $"{_selected.Name}\nConnexion : {_selected.Connection}    VID:PID {vidPid}";
        PairingText.Text = ControllerDb.PairingHint(_selected.Family);
    }

    private void UpdateTester()
    {
        if (_selected is null)
        {
            return;
        }

        TesterText.Text = _service.ReadSnapshot(_selected);

        if (_virtualPad.IsConnected)
        {
            var state = PadMapper.Read(_selected, _profiles, out var reason);
            if (state is not null)
            {
                _virtualPad.Send(state);
            }
            else if (reason is not null)
            {
                StatusText.Text = reason;
            }
        }
    }

    /// <summary>Active ou supprime la manette virtuelle Xbox 360 avec un seul bouton.</summary>
    private void VirtualPadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_virtualPad.IsConnected)
        {
            _virtualPad.Disconnect();
            VirtualPadButton.Content = "Activer la conversion Xbox 360";
            StatusText.Text = "Conversion Xbox 360 supprimée";
            return;
        }

        try
        {
            _virtualPad.Connect();
            VirtualPadButton.Content = "Supprimer la conversion Xbox 360";
            StatusText.Text = "Conversion Xbox 360 active";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Échec (ViGEmBus installé ?) : {ex.Message}";
        }
    }

    private async void Vibrate_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null)
        {
            StatusText.Text = "Sélectionnez une manette";
            return;
        }

        if (_selected.Gamepad is null)
        {
            StatusText.Text = "Vibration disponible uniquement pour les manettes XInput";
            return;
        }

        VibrateButton.IsEnabled = false;
        await _service.VibrateAsync(_selected);
        VibrateButton.IsEnabled = true;
    }

    private void OpenBluetooth_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:bluetooth") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Impossible d'ouvrir les paramètres : {ex.Message}";
        }
    }
}
