namespace ControllerHub.Models;

/// <summary>
/// Identifie une manette à partir de son Vendor ID / Product ID USB (VID:PID)
/// et fournit les libellés et conseils d'appairage associés.
/// </summary>
public static class ControllerDb
{
    private const ushort VendorMicrosoft = 0x045E;
    private const ushort VendorSony = 0x054C;
    private const ushort VendorNintendo = 0x057E;

    private static readonly Dictionary<(ushort Vid, ushort Pid), ControllerFamily> Known = new()
    {
        [(VendorSony, 0x05C4)] = ControllerFamily.Ps4,        // DualShock 4 v1
        [(VendorSony, 0x09CC)] = ControllerFamily.Ps4,        // DualShock 4 v2
        [(VendorSony, 0x0CE6)] = ControllerFamily.Ps5,        // DualSense
        [(VendorSony, 0x0DF2)] = ControllerFamily.Ps5Edge,    // DualSense Edge
        [(VendorNintendo, 0x2009)] = ControllerFamily.SwitchPro,
        [(VendorNintendo, 0x2006)] = ControllerFamily.JoyConLeft,
        [(VendorNintendo, 0x2007)] = ControllerFamily.JoyConRight,
        [(VendorNintendo, 0x200E)] = ControllerFamily.JoyConGrip,
    };

    /// <summary>Retourne la famille et un libellé lisible pour un couple VID/PID HID.</summary>
    public static ControllerFamily Identify(ushort vid, ushort pid, out string label)
    {
        if (Known.TryGetValue((vid, pid), out var family))
        {
            label = FamilyName(family);
            return family;
        }

        // Modèle inconnu : on indique au moins le fabricant quand il est connu.
        label = vid switch
        {
            VendorMicrosoft => "Manette Microsoft (modèle inconnu)",
            VendorSony => "Manette Sony (modèle inconnu)",
            VendorNintendo => "Manette Nintendo (modèle inconnu)",
            _ => $"Manette générique HID ({vid:X4}:{pid:X4})"
        };
        return ControllerFamily.GenericHid;
    }

    public static string FamilyName(ControllerFamily family) => family switch
    {
        ControllerFamily.Xbox => "Xbox (Xbox Wireless / XInput)",
        ControllerFamily.Ps4 => "PlayStation 4 — DualShock 4",
        ControllerFamily.Ps5 => "PlayStation 5 — DualSense",
        ControllerFamily.Ps5Edge => "PlayStation 5 — DualSense Edge",
        ControllerFamily.SwitchPro => "Nintendo Switch — Pro Controller",
        ControllerFamily.JoyConLeft => "Joy-Con (gauche)",
        ControllerFamily.JoyConRight => "Joy-Con (droite)",
        ControllerFamily.JoyConGrip => "Joy-Con — poignée de charge",
        _ => "Manette générique (HID / MFi)"
    };

    /// <summary>Procédure d'appairage Bluetooth à suivre pour mettre la manette en mode appairage.</summary>
    public static string PairingHint(ControllerFamily family) => family switch
    {
        ControllerFamily.Xbox =>
            "Bluetooth : appuyez sur le bouton Synchro (petit bouton sur le dessus, près du port USB-C) " +
            "jusqu'à ce que le logo Xbox clignote, puis choisissez « Manette sans fil Xbox » dans Windows. " +
            "USB : un simple branchement suffit.",
        ControllerFamily.Ps4 =>
            "Maintenez les boutons PS + Share jusqu'à ce que la barre lumineuse clignote, " +
            "puis sélectionnez la manette dans les paramètres Bluetooth de Windows.",
        ControllerFamily.Ps5 or ControllerFamily.Ps5Edge =>
            "Maintenez les boutons PS + Créer jusqu'à ce que la LED clignote, " +
            "puis sélectionnez « Wireless Controller » dans les paramètres Bluetooth de Windows.",
        ControllerFamily.SwitchPro =>
            "Appuyez brièvement sur le bouton Synchro (sur le dessus, entre les gâchettes) " +
            "jusqu'à ce que les lumières de joueur clignotent, puis associez-la dans Windows.",
        ControllerFamily.JoyConLeft or ControllerFamily.JoyConRight =>
            "Retirez le Joy-Con de la poignée et maintenez le bouton Synchro situé sur le rail " +
            "jusqu'à ce que la LED clignote. Appairez chaque Joy-Con séparément ; " +
            "ControllerHub les regroupe automatiquement dès que le gauche et le droit sont détectés.",
        ControllerFamily.JoyConGrip =>
            "Consultez le mode d'appairage de la poignée de charge : elle se connecte comme une manette " +
            "Bluetooth, en général avec le bouton Synchro.",
        _ =>
            "Consultez le manuel : la plupart des manettes génériques ou compatibles MFi s'appairent " +
            "en maintenant leur bouton Bluetooth ou Mode jusqu'à ce que la LED clignote."
    };
}
