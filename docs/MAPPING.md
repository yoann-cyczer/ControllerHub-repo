# Guide de mappage : remplir `mappings.json`

Ce guide explique comment renseigner le mappage d'une manette HID (PlayStation, Switch, Joy-Con, générique) pour qu'elle soit convertie en manette Xbox 360.

## Principe

Windows ne donne pas de noms aux boutons des manettes HID : chaque bouton est un index (`B0`, `B1`, …) et chaque axe un numéro (`Axe 0`, `Axe 1`, …). Le fichier `mappings.json` associe ces index aux boutons d'une Xbox 360.

Chaque entrée correspond à un modèle, identifié par son VID:PID affiché dans l'application.

## Procédure de relevé

1. Lancez ControllerHub et sélectionnez la manette dans la liste.
2. Regardez le champ **VID:PID** dans le panneau de détails. C'est l'identifiant à reporter dans `"Id"`.
3. Appuyez sur chaque bouton **un par un** et notez l'index `B…` qui apparaît dans le testeur.
4. Bougez chaque stick vers le haut, puis vers la droite, et appuyez à fond sur les gâchettes. Notez les `Axe …` qui changent.
5. Si un stick donne une valeur négative vers le haut, notez-le : il faudra l'inverser.

## Clés à remplir

### Boutons (`Buttons`)

| Clé | Bouton Xbox 360 | Exemple PlayStation |
|---|---|---|
| `A` | A (bas) | Croix |
| `B` | B (droite) | Rond |
| `X` | X (gauche) | Carré |
| `Y` | Y (haut) | Triangle |
| `LB` | Épaule gauche | L1 |
| `RB` | Épaule droite | R1 |
| `LT` | Gâchette gauche (si bouton) | L2 |
| `RT` | Gâchette droite (si bouton) | R2 |
| `Back` | Back / Vue | Create |
| `Start` | Start / Menu | Options |
| `Guide` | Bouton Xbox | PS |
| `LS` | Clic stick gauche | L3 |
| `RS` | Clic stick droit | R3 |
| `Up`, `Down`, `Left`, `Right` | Croix directionnelle | flèches |

### Axes (`Axes`)

| Clé | Signification |
|---|---|
| `LX`, `LY` | Stick gauche, horizontal et vertical |
| `RX`, `RY` | Stick droit, horizontal et vertical |
| `LT`, `RT` | Gâchettes, si elles sont analogiques |

Une gâchette peut être soit dans `Axes` (valeur analogique), soit dans `Buttons` (1 si pressée). Ne la mettez que dans l'un des deux.

### Autres champs

- `InvertedAxes` : liste des axes à inverser, par exemple `["LY", "RY"]`.
- `SignedTriggers` : `true` si les gâchettes vont de -1 à 1 au lieu de 0 à 1.

## Exemple complet

```json
{
  "Id": "054C:0CE6",
  "Name": "PlayStation 5 — DualSense",
  "Buttons": {
    "A": 1, "B": 2, "X": 0, "Y": 3,
    "LB": 4, "RB": 5,
    "LT": 6, "RT": 7,
    "LS": 10, "RS": 11
  },
  "Axes": {
    "LX": 0, "LY": 1,
    "RX": 2, "RY": 3
  },
  "InvertedAxes": ["LY"],
  "SignedTriggers": false
}
```

Les valeurs ci-dessus sont un exemple de structure. Remplacez-les par les index relevés sur votre manette.

## Vérification

1. Enregistrez `mappings.json`.
2. Si vous lancez depuis Visual Studio ou `dotnet run`, relancez l'application. Si vous utilisez l'exécutable, éditez le `mappings.json` à côté de `ControllerHub.exe` et relancez-le.
3. Cochez la conversion Xbox 360, puis ouvrez le **Contrôleur de jeu Windows** (`joy.cpl`) : la manette virtuelle doit réagir comme une Xbox 360.

## Erreurs courantes

- **« Mappage à compléter »** : le profil existe mais `Buttons` est vide, ou le VID:PID ne correspond pas. Vérifiez la casse et le format `054C:0CE6`.
- **Bouton qui en déclenche un autre** : les index relevés ne correspondent pas. Refaites le relevé bouton par bouton.
- **Stick inversé** : ajoutez l'axe à `InvertedAxes`.
- **Aucune réaction** : `mappings.json` n'est pas à jour à côté de l'exécutable, ou le fichier est invalide (virgule en trop, guillemets manquants). Les erreurs de lecture ne sont pas affichées dans l'interface.
