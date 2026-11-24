# ControllerHub

Application Windows native (WPF, .NET 8) pour détecter, identifier, tester et utiliser les manettes de jeu, y compris dans des jeux qui ne reconnaissent que les manettes Xbox 360.

## Fonctionnalités

- **Détection** des manettes branchées en USB ou en Bluetooth, mises à jour toutes les secondes.
- **Identification** par VID:PID : PlayStation 4 (DualShock 4), PlayStation 5 (DualSense, DualSense Edge), Nintendo Switch Pro Controller, Joy-Con (gauche, droite, poignée de charge), Xbox, manettes HID génériques et compatibles MFi.
- **Joy-Con en paire** : lorsque le Joy-Con gauche et le droit sont détectés, ils sont signalés comme paire. Le regroupement est visuel uniquement.
- **Testeur en direct** : boutons pressés, axes des sticks et valeurs des gâchettes.
- **Vibration de test** pour les manettes XInput (Xbox).
- **Appairage** : procédure adaptée à chaque modèle, et bouton qui ouvre les paramètres Bluetooth de Windows.
- **Conversion en manette virtuelle Xbox 360** via le pilote ViGEmBus, pour qu'un jeu qui ne reconnaît que les Xbox 360 utilise votre manette. Un bouton active la conversion, le même bouton la supprime.

## État du projet

- Le build de la v1 a été validé sur Windows (0 erreur, 0 avertissement).
- Les versions suivantes (conversion Xbox 360, mappage DualSense partiel) n'ont **pas encore été compilées ni testées** sur Windows.
- Le mappage est renseigné uniquement pour les boutons de la DualSense. Les sticks, la croix directionnelle, Create, Options et PS restent à compléter.

## Prérequis

- Windows 10 (1809 ou plus récent) ou Windows 11.
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), ou Visual Studio 2022 avec la charge de travail « Développement de bureau .NET ».
- Pour la conversion Xbox 360 : le pilote [ViGEmBus](https://github.com/nefarius/ViGEmBus), installé puis redémarrage du PC.

## Utilisation

### Lancer depuis Visual Studio ou la ligne de commande

```cmd
dotnet run --project ControllerHub\ControllerHub\ControllerHub.csproj -c Release
```

### Créer un exécutable autonome

Double-cliquez sur `publier.bat`, à la racine du dépôt. Il publie l'application dans `ControllerHub-exe\`, avec un fichier `ControllerHub.exe` et une copie de `mappings.json`.

## Architecture

```
ControllerHub/                       racine du dépôt
├── publier.bat                      publication en .exe autonome
├── docs/MAPPING.md                  guide de remplissage de mappings.json
└── ControllerHub/                   projet .NET
    ├── ControllerHub.csproj
    ├── mappings.json                mappage des boutons bruts, par VID:PID
    ├── App.xaml(.cs)
    ├── MainWindow.xaml(.cs)         interface et boucles de rafraîchissement
    ├── Models/
    │   ├── ControllerFamily.cs      énumération des familles
    │   ├── ControllerDb.cs          VID:PID → famille, libellés, appairage
    │   ├── ControllerEntry.cs       une manette détectée
    │   ├── MappingProfile.cs        un profil de mappage
    │   └── PadState.cs              état normalisé d'une manette
    └── Services/
        ├── ControllerService.cs     énumération, lecture du testeur, vibration
        ├── MappingStore.cs          chargement de mappings.json
        ├── PadMapper.cs             lecture → PadState, avec mappage pour le HID
        └── VirtualXbox360.cs        manette virtuelle Xbox 360 (ViGEmBus)
```

### Flux de données

1. **Énumération** (`ControllerService.Scan`, toutes les secondes) :
   - les manettes XInput sont lues via `Gamepad.Gamepads` ;
   - les manettes HID sont lues via `RawGameController.RawGameControllers`, sauf celles de Microsoft (déjà couvertes par XInput) ;
   - chaque manette HID est identifiée par son VID:PID (`ControllerDb.Identify`).
2. **Liste** : `MainWindow` ne reconstruit la liste que si les identifiants changent, et conserve la sélection.
3. **Testeur** (`ControllerService.ReadSnapshot`, toutes les 33 ms) : affiche l'état brut ou standardisé de la manette sélectionnée.
4. **Conversion** (si la manette virtuelle est active) :
   - `PadMapper.Read` produit un `PadState` ;
   - pour une manette XInput, les valeurs sont déjà standardisées ;
   - pour une manette HID, le profil de `mappings.json` (identifié par VID:PID) traduit les index bruts en boutons logiques ;
   - `VirtualXbox360.Send` transmet l'état à la manette virtuelle.

### Mappage HID

Les manettes HID n'exposent pas de noms de boutons. Le profil de chaque modèle indique l'index brut de chaque bouton logique (`A`, `B`, `X`, `Y`, `LB`, `RB`, `LT`, `RT`, `Back`, `Start`, `Guide`, `LS`, `RS`, `Up`, `Down`, `Left`, `Right`) et de chaque axe (`LX`, `LY`, `RX`, `RY`, `LT`, `RT`). Le détail de la procédure de relevé est dans [docs/MAPPING.md](docs/MAPPING.md).

Si une gâchette n'a pas d'axe défini mais un bouton, elle est traitée comme un bouton : 1 si pressée, 0 sinon.

## Limites connues

- **Mappage incomplet** pour la DualSense et absent pour les autres modèles HID. Tant que le profil est vide, la conversion affiche « Mappage à compléter ».
- **Gâchettes en bouton** : sur la DualSense, L2 et R2 sont lues comme des boutons numériques. Elles passent donc de 0 à 1 sans valeur intermédiaire.
- **Double saisie** : si la manette physique est une Xbox 360 déjà reconnue, le jeu reçoit à la fois la manette physique et la manette virtuelle. Il faut alors ne pas activer la conversion pour cette manette, ou masquer la manette physique avec un outil comme HidHide.
- **Une seule manette** alimente la conversion à la fois : celle qui est sélectionnée dans la liste.
- **Joy-Con** : chaque Joy-Con a ses propres index. La fusion des deux en une manette virtuelle n'est pas implémentée.
- **MFi** : la certification MFi est une norme Apple. Sous Windows, une manette MFi apparaît comme une manette HID générique ou XInput.
- **Appairage** : l'application ouvre les paramètres Bluetooth et explique la procédure, mais n'appaire pas la manette elle-même.
- **Fonctions avancées absentes** : retour haptique DualSense, gâchettes adaptatives, gyroscope, micro.

## Feuille de route

1. Compléter le mappage de la DualSense (sticks, croix directionnelle, Create, Options, PS).
2. Renseigner les profils PS4 et Switch Pro, puis tester la conversion dans un jeu.
3. Ajouter un profil par Joy-Con, puis une option de fusion en paire.
4. Ajouter une option pour masquer automatiquement la manette physique (HidHide).
5. Remplacer les index bruts par des noms de boutons dans le testeur, à partir de `mappings.json`.
