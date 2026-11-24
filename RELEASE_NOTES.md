# ControllerHub 0.3.0

## Nouveautés
- Jusqu'à 4 joueurs simultanés (P1 à P4), chacun avec sa propre manette Xbox 360 virtuelle.
- Boutons « Ajouter comme joueur suivant » et « Retirer le joueur sélectionné ».
- Libération automatique d'un joueur quand sa manette est débranchée.
- Validation CRC32 des rapports Bluetooth DualSense et DualShock 4 (rapports invalides ignorés et comptés).
- Lecture HID directe pour la DualSense, la DualShock 4 et la Switch Pro.

## Changements
- Seules les manettes attribuées à un joueur alimentent les pads virtuels.
- Le testeur reste disponible pour la manette sélectionnée.
- Fenêtre portée à 640 px de haut.

## Limites connues
- L'ordre des numéros Xbox dépend de l'ordre de création des pads.
- Maximum 4 joueurs (limite XInput).
- Croix directionnelle de la DualSense à valider sur matériel.
- Joy-Con en lecture HID directe non implémenté.
