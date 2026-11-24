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

    private readonly List<MappingProfile> _profiles = LoadProfiles();
    private readonly PlayerManager _players;

    // Lecture HID réservée au testeur, quand la manette sélectionnée n'est pas un joueur.
    private readonly DualSenseSession _testerSession = new();

    private ControllerEntry? _selected;
    private List<string> _knownIds = new();

    public MainWindow()
    {
        InitializeComponent();

        _players = new PlayerManager(_profiles);

        _scanTimer.Tick += (_, _) => Rescan();
        _readTimer.Tick += (_, _) =>
        {
            _players.Tick();
            UpdateTester();
        };

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
            _players.Dispose();
            _testerSession.Dispose();
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

        // Libère les joueurs dont la manette a été débranchée.
        if (_players.Prune(ids))
        {
            RefreshPlayers();
        }

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

    /// <summary>Affiche la lecture de la manette sélectionnée. Les états envoyés aux joueurs sont gérés par PlayerManager.</summary>
    private void UpdateTester()
    {
        if (_selected is null)
        {
            return;
        }

        var slot = _players.FindSlot(_selected.Id);
        string? reason;

        if (slot is not null)
        {
            reason = slot.Reason;
        }
        else
        {
            PlayerManager.ReadState(_selected, _testerSession, _profiles, out reason);
        }

        var session = slot?.Session ?? _testerSession;
        bool isHidDirect = DualSenseSession.Supports(_selected.Family);
        TesterText.Text = isHidDirect && session.Current is { } reader
            ? reader.Describe()
            : _service.ReadSnapshot(_selected);

        if (reason is not null)
        {
            StatusText.Text = reason;
        }
    }

    /// <summary>Recharge la liste des joueurs affichée.</summary>
    private void RefreshPlayers()
    {
        PlayerList.ItemsSource = null;
        PlayerList.ItemsSource = _players.Slots.Where(s => s.IsOccupied).ToList();
    }

    /// <summary>Ajoute la manette sélectionnée au premier joueur libre.</summary>
    private void AddPlayer_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null)
        {
            StatusText.Text = "Sélectionnez une manette";
            return;
        }

        var slot = _players.Assign(_selected, out var error);
        if (slot is null)
        {
            StatusText.Text = error;
            return;
        }

        // La manette est désormais lue par son joueur : on libère la lecture du testeur.
        _testerSession.Close();
        RefreshPlayers();
        StatusText.Text = error ?? $"{_selected.Title} ajouté comme P{slot.Number}";
    }

    /// <summary>Retire le joueur sélectionné dans la liste des joueurs.</summary>
    private void RemovePlayer_Click(object sender, RoutedEventArgs e)
    {
        if (PlayerList.SelectedItem is not PlayerSlot slot)
        {
            StatusText.Text = "Sélectionnez un joueur à retirer";
            return;
        }

        _players.Unassign(slot);
        RefreshPlayers();
        StatusText.Text = $"P{slot.Number} retiré";
    }

    /// <summary>Active ou supprime la conversion Xbox 360 pour tous les joueurs, avec un seul bouton.</summary>
    private void VirtualPadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_players.ConversionEnabled)
        {
            _players.SetConversion(false);
            VirtualPadButton.Content = "Activer la conversion Xbox 360";
            StatusText.Text = "Conversion Xbox 360 supprimée";
            RefreshPlayers();
            return;
        }

        var error = _players.SetConversion(true);
        VirtualPadButton.Content = "Supprimer la conversion Xbox 360";
        StatusText.Text = error is null
            ? "Conversion Xbox 360 active"
            : $"Échec (ViGEmBus installé ?) : {error}";
        RefreshPlayers();
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
