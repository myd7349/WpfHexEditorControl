# 🔍 API Methods NOT Yet Exposed on HexEditor V2

**Date** : 2026-02-19
**Status** : Analyse des méthodes manquantes

---

## 📊 Résumé

Sur les 187 membres Legacy analysés, **la plupart sont implémentés**, mais certaines méthodes importantes de `ByteProvider` ne sont **pas exposées publiquement** sur la classe `HexEditor`.

**Impact** :
- ✅ API Legacy (V1) : **100% compatible** (wrappers existent)
- ⚠️ API ByteProvider directe : **Partiellement exposée**

---

## ❌ Méthodes Principales Manquantes

### 1. **OpenStream() / OpenMemory()** - CRITIQUE

**Status** : ⛔ **NON EXPOSÉES** sur HexEditor

| Méthode | Existe sur ByteProvider | Existe sur HexEditor | Impact |
|---------|-------------------------|---------------------|---------|
| `OpenStream(Stream, bool)` | ✅ Oui | ❌ Non | **BLOQUANT** pour tests unitaires |
| `OpenMemory(byte[], bool)` | ✅ Oui | ❌ Non | **BLOQUANT** pour données mémoire |

**Détails** :

```csharp
// ✅ EXISTE sur ByteProvider (ligne 166, 176)
public void OpenStream(Stream stream, bool readOnly = false)
public void OpenMemory(byte[] data, bool readOnly = false)

// ❌ N'EXISTE PAS sur HexEditor
// HexEditor a seulement :
public void OpenFile(string filePath)  // Fichiers uniquement
public Stream Stream { get; }          // Read-only property
```

**Conséquence** :
- Impossible de charger un `MemoryStream` directement
- Tests unitaires doivent utiliser des fichiers temporaires
- Pas d'édition de données en mémoire pure

**Workaround actuel** :
```csharp
// Créer fichier temporaire
var tempFile = Path.GetTempFileName();
File.WriteAllBytes(tempFile, byteArray);
hexEditor.OpenFile(tempFile);
```

---

### 2. **Méthodes ByteProvider Avancées**

Ces méthodes existent sur `ByteProvider` mais ne sont pas directement exposées sur `HexEditor` :

| Méthode | ByteProvider | HexEditor | Notes |
|---------|--------------|-----------|-------|
| `GetLine(long, int)` | ✅ | ❌ | Accès via `_viewModel` interne |
| `GetLines(long, int, int)` | ✅ | ❌ | Utilisé par viewport |
| `BeginBatch()` / `EndBatch()` | ✅ | ❌ | Optimisation batch |
| `GetCacheStatistics()` | ✅ | ❌ | Debug/profiling |
| `ClearModifications()` | ✅ | ❌ | Existe comme `ClearAllChange()` |
| `ClearInsertions()` | ✅ | ❌ | Non exposé |
| `ClearDeletions()` | ✅ | ❌ | Non exposé |
| `ClearUndoRedoHistory()` | ✅ | ❌ | Existe comme `ClearUndoRedo()` |

---

## ✅ Méthodes DÉJÀ Exposées (Confirmation)

Ces méthodes **EXISTENT BIEN** sur HexEditor (contrairement au rapport de l'agent) :

| Méthode | Fichier | Ligne |
|---------|---------|-------|
| **ModifyByte** | HexEditor.ByteOperations.cs | 61 |
| **InsertByte(byte, long)** | HexEditor.ByteOperations.cs | 82 |
| **InsertByte(byte, long, long)** | HexEditor.ByteOperations.cs | 94 |
| **InsertBytes** | HexEditor.ByteOperations.cs | 113 |
| **DeleteBytesAtPosition** | HexEditor.ByteOperations.cs | 124 |
| **GetByte** | HexEditor.ByteOperations.cs | 28, 152 |
| **GetAllBytes** | HexEditor.ByteOperations.cs | 169 |
| **GetByteModifieds** | HexEditor.ByteOperations.cs | 356 |
| **Save** | HexEditor.FileOperations.cs | 159 |
| **SaveAs** | HexEditor.FileOperations.cs | 189 |
| **Undo** / **Redo** | HexEditor.CompatibilityLayer.Methods.cs | 92, 108 |
| **FindAll** / **FindFirst** | HexEditor.Search.cs | 23, 35 |
| **ReplaceAll** | HexEditor.FindReplace.cs | 23 |

**Note** : Ces méthodes sont dans les **PartialClasses** et donc font partie de l'API publique de `HexEditor`.

---

## 🎯 Recommandations

### Priorité 1 : OpenStream / OpenMemory (CRITIQUE)

**Ajouter ces wrappers dans** : `HexEditor.FileOperations.cs` ou nouveau fichier `HexEditor.StreamOperations.cs`

```csharp
/// <summary>
/// Open a stream for editing (V1 compatibility)
/// </summary>
/// <param name="stream">Stream to open</param>
/// <param name="readOnly">Read-only mode</param>
public void OpenStream(Stream stream, bool readOnly = false)
{
    if (stream == null)
        throw new ArgumentNullException(nameof(stream));

    // Close current file if any
    if (_viewModel != null)
        Close();

    // Create ByteProvider with stream
    var provider = new Core.Bytes.ByteProvider();
    provider.OpenStream(stream, readOnly);

    // Initialize ViewModel
    _viewModel = new HexEditorViewModel(provider);
    HexViewport.LinesSource = _viewModel.Lines;

    // Synchronize properties
    _viewModel.BytePerLine = BytePerLine;
    _viewModel.EditMode = EditMode;

    // Subscribe to events
    _viewModel.PropertyChanged += ViewModel_PropertyChanged;

    // Update UI
    IsFileOrStreamLoaded = true;
    IsModified = false;
    UpdateVisibleLines();

    OnFileOpened(EventArgs.Empty);
}

/// <summary>
/// Open byte array in memory for editing (V1 compatibility)
/// </summary>
/// <param name="data">Byte array to edit</param>
/// <param name="readOnly">Read-only mode</param>
public void OpenMemory(byte[] data, bool readOnly = false)
{
    if (data == null)
        throw new ArgumentNullException(nameof(data));

    // Same logic as OpenStream but with OpenMemory
    if (_viewModel != null)
        Close();

    var provider = new Core.Bytes.ByteProvider();
    provider.OpenMemory(data, readOnly);

    _viewModel = new HexEditorViewModel(provider);
    HexViewport.LinesSource = _viewModel.Lines;
    _viewModel.BytePerLine = BytePerLine;
    _viewModel.EditMode = EditMode;
    _viewModel.PropertyChanged += ViewModel_PropertyChanged;

    IsFileOrStreamLoaded = true;
    IsModified = false;
    UpdateVisibleLines();

    OnFileOpened(EventArgs.Empty);
}
```

**Impact** :
- ✅ Tests unitaires simplifiés (pas de fichiers temporaires)
- ✅ Compatibilité 100% avec V1 (Stream setter)
- ✅ Édition de données en mémoire

---

### Priorité 2 : Batch Operations (Optionnel)

**Exposer** : `BeginBatch()` / `EndBatch()` pour optimisations

```csharp
/// <summary>
/// Begin batch operation (improves performance for multiple edits)
/// </summary>
public void BeginBatch() => _viewModel?.Provider?.BeginBatch();

/// <summary>
/// End batch operation and apply changes
/// </summary>
public void EndBatch() => _viewModel?.Provider?.EndBatch();
```

**Usage** :
```csharp
editor.BeginBatch();
for (int i = 0; i < 1000; i++)
    editor.ModifyByte(0xFF, i);
editor.EndBatch(); // Single undo entry, faster
```

---

### Priorité 3 : Cache Statistics (Debug)

**Exposer** : `GetCacheStatistics()` pour profiling

```csharp
/// <summary>
/// Get cache statistics (for debugging/profiling)
/// </summary>
public string GetCacheStatistics()
    => _viewModel?.Provider?.GetCacheStatistics() ?? "No data loaded";
```

---

## 📊 Tableau de Compatibilité Mis à Jour

| Catégorie | Total Membres | Exposés | Manquants | % |
|-----------|---------------|---------|-----------|---|
| **Récupération données** | 6 | 6 | 0 | ✅ 100% |
| **Sélection/Navigation** | 12 | 12 | 0 | ✅ 100% |
| **Modification bytes** | 8 | 8 | 0 | ✅ 100% |
| **Recherche/Remplacement** | 38 | 38 | 0 | ✅ 100% |
| **Signets/Surlignages** | 11 | 11 | 0 | ✅ 100% |
| **Clipboard/Fichiers** | 13 | 13 | 0 | ✅ 100% |
| **Propriétés UI** | 93 | 93 | 0 | ✅ 100% |
| **Chargement Stream** | 2 | 0 | **2** | ⚠️ **0%** |
| **Batch operations** | 2 | 0 | 2 | ⚠️ 0% |
| **Cache debug** | 1 | 0 | 1 | ⚠️ 0% |
| **TOTAL** | **186** | **181** | **5** | **97.3%** |

**Note** : OpenStream/OpenMemory sont critiques pour compatibilité V1 complète.

---

## 🔧 Plan d'Action

### Phase 1 : OpenStream/OpenMemory (URGENT)
1. Créer `HexEditor.StreamOperations.cs` dans PartialClasses/Core
2. Implémenter `OpenStream()` et `OpenMemory()`
3. Mettre à jour tests Phase 1 pour utiliser `OpenMemory()`
4. Tester avec cas d'usage Legacy

**Durée estimée** : 2-3 heures

### Phase 2 : Batch Operations (Optionnel)
1. Exposer `BeginBatch()` / `EndBatch()`
2. Documenter usage
3. Ajouter tests de performance

**Durée estimée** : 1 heure

### Phase 3 : Cache Statistics (Optionnel)
1. Exposer `GetCacheStatistics()`
2. Documenter format de sortie

**Durée estimée** : 30 minutes

---

## 📝 Conclusion

**HexEditor V2 est à 97.3% compatible avec l'API ByteProvider**, mais manque de **2 méthodes critiques** :
- ❌ `OpenStream(Stream, bool)`
- ❌ `OpenMemory(byte[], bool)`

Ces méthodes sont **essentielles pour** :
- Compatibilité 100% avec V1 (propriété `Stream` settable)
- Tests unitaires simplifiés
- Édition de données en mémoire

**Recommandation** : Implémenter Phase 1 (OpenStream/OpenMemory) avant release.

---

**Auteur** : Claude Sonnet 4.5
**Date** : 2026-02-19
**Révision** : 1.0
