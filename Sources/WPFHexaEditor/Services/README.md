# Services Architecture

Ce dossier contient les services qui encapsulent la logique métier du HexEditor.
L'objectif est de réduire la complexité de la classe `HexEditor` (actuellement 6115 lignes) en extrayant les responsabilités dans des services dédiés.

## 📋 Services Disponibles

### ✅ ClipboardService
**Responsabilité:** Gestion des opérations de copier/coller/couper

**Méthodes principales:**
- `CopyToClipboard()` - Copie vers le presse-papier
- `CopyToStream()` - Copie vers un flux
- `GetCopyData()` - Récupère les données copiées
- `FillWithByte()` - Remplit une sélection avec un byte
- `CanCopy()` - Vérifie si la copie est possible
- `CanDelete()` - Vérifie si la suppression est possible

**Utilisation:**
```csharp
var clipboardService = new ClipboardService
{
    DefaultCopyMode = CopyPasteMode.HexaString
};

// Copier la sélection
clipboardService.CopyToClipboard(_provider, SelectionStart, SelectionStop, _tblCharacterTable);

// Vérifier si on peut copier
if (clipboardService.CanCopy(SelectionLength, _provider))
{
    // ...
}
```

---

### 🔄 FindReplaceService
**Responsabilité:** Recherche et remplacement de données avec cache optimisé

**Méthodes principales:**
- `FindFirst()` - Trouve la première occurrence
- `FindNext()` - Trouve l'occurrence suivante
- `FindLast()` - Trouve la dernière occurrence
- `FindAll()` - Trouve toutes les occurrences
- `ReplaceFirst()` - Remplace la première occurrence
- `ReplaceAll()` - Remplace toutes les occurrences
- `ClearCache()` - Efface le cache de recherche

**Utilisation:**
```csharp
var findReplaceService = new FindReplaceService();

// Rechercher
byte[] searchData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
long position = findReplaceService.FindFirst(_provider, searchData);

// Remplacer
byte[] replaceData = new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 };
var replacedPositions = findReplaceService.ReplaceAll(_provider, searchData, replaceData, false, false);
```

**Note:** Le service inclut un cache de recherche avec timeout de 5 secondes pour optimiser les performances.

---

### ↩️ UndoRedoService
**Responsabilité:** Gestion de l'historique d'annulation/rétablissement

**Méthodes principales:**
- `Undo()` - Annule la dernière action (retourne la position du byte)
- `Redo()` - Rétablit une action annulée (retourne la position du byte)
- `ClearAll()` - Efface tout l'historique
- `CanUndo()` - Vérifie si l'annulation est possible
- `CanRedo()` - Vérifie si le rétablissement est possible
- `GetUndoCount()` - Récupère le nombre d'actions dans l'historique
- `GetUndoStack()` - Récupère la pile d'annulation

**Utilisation:**
```csharp
var undoRedoService = new UndoRedoService();

// Annuler
if (undoRedoService.CanUndo(_provider))
{
    long position = undoRedoService.Undo(_provider);
    // Met à jour la position dans l'UI
}

// Rétablir
if (undoRedoService.CanRedo(_provider))
{
    long position = undoRedoService.Redo(_provider, repeat: 3);
    // Rétablit les 3 dernières actions annulées
}
```

---

### 🎯 SelectionService
**Responsabilité:** Gestion de la sélection et validation

**Méthodes principales:**
- `IsValidSelection()` - Vérifie si une sélection est valide
- `GetSelectionLength()` - Calcule la longueur de la sélection
- `FixSelectionRange()` - Corrige l'ordre start/stop
- `ValidateSelection()` - Valide et ajuste la sélection aux limites
- `GetSelectionBytes()` - Récupère les bytes sélectionnés
- `GetAllBytes()` - Récupère tous les bytes du provider
- `GetSelectAllStart()` / `GetSelectAllStop()` - Calcule les positions pour "Sélectionner tout"
- `IsAllSelected()` - Vérifie si tout est sélectionné
- `HasSelection()` - Vérifie si une sélection existe
- `ExtendSelection()` - Étend la sélection avec un offset
- `GetSelectionByte()` - Récupère le byte à une position

**Utilisation:**
```csharp
var selectionService = new SelectionService();

// Vérifier la sélection
if (selectionService.IsValidSelection(SelectionStart, SelectionStop))
{
    long length = selectionService.GetSelectionLength(SelectionStart, SelectionStop);
    byte[] data = selectionService.GetSelectionBytes(_provider, SelectionStart, SelectionStop);
}

// Corriger la sélection si inversée
var (start, stop) = selectionService.FixSelectionRange(SelectionStart, SelectionStop);

// Valider et ajuster aux limites
var (validStart, validStop) = selectionService.ValidateSelection(_provider, SelectionStart, SelectionStop);
```

---

## 🏗️ Architecture

```
HexEditor (Contrôleur principal)
    ├── ClipboardService
    ├── FindReplaceService
    ├── UndoRedoService
    └── SelectionService
```

## 📦 Avantages de cette architecture

1. **Séparation des responsabilités** - Chaque service a une responsabilité unique
2. **Testabilité** - Les services peuvent être testés unitairement de manière isolée
3. **Réutilisabilité** - Les services peuvent être utilisés dans d'autres contextes
4. **Maintenabilité** - Code plus facile à comprendre et à modifier
5. **Extensibilité** - Facile d'ajouter de nouveaux services

## 🔧 Migration Progressive

L'intégration des services se fait progressivement :

**Phase 1 (Complétée ✅):** Création des services sans modifier HexEditor
- ✅ ClipboardService créé et testé
- ✅ FindReplaceService créé et testé
- ✅ UndoRedoService créé et testé
- ✅ SelectionService créé et testé

**Phase 2:** Intégration optionnelle des services dans HexEditor
**Phase 3:** Refactoring complet de HexEditor pour utiliser les services

Cette approche permet de :
- ✅ Garder le code existant fonctionnel
- ✅ Tester chaque service individuellement
- ✅ Migrer progressivement sans régression

## 📝 Notes de développement

- Tous les services sont dans le namespace `WpfHexaEditor.Services`
- Les services sont stateless (sans état) quand c'est possible
- Les dépendances sont passées en paramètres plutôt qu'injectées
- Les services ne dépendent PAS de HexEditor (découplage fort)
