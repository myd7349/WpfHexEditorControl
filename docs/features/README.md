# Features Documentation

Cette section contient la documentation détaillée des fonctionnalités principales de WPFHexaEditor.

---

## 📚 Documents disponibles

### Format Detection System

#### [FormatDetection_400.md](FormatDetection_400.md)
Documentation complète de la bibliothèque de 400 formats auto-détectables.

**Contenu** :
- Vue d'ensemble des 400 formats
- Répartition par 20 catégories
- Guide d'utilisation avec exemples de code
- Comparaison avec autres hex editors
- Instructions pour ajouter de nouveaux formats

**Pour qui** : Développeurs et utilisateurs voulant comprendre et utiliser la détection automatique de formats.

---

#### [VALIDATION_400_FORMATS.md](VALIDATION_400_FORMATS.md)
Document de validation technique des 400 formats.

**Contenu** :
- Résultats des tests unitaires
- Métriques de performance
- Statistiques de couverture
- Comparaisons avec versions précédentes
- Records établis vs concurrence

**Pour qui** : Développeurs et mainteneurs voulant des détails techniques et validation.

---

## 📖 Documents connexes

### Documentation d'architecture
- [docs/architecture/services/format-detection.md](../architecture/services/format-detection.md) - Architecture du service de détection

### Documentation API
- [docs/api-reference/features/format-detection.md](../api-reference/features/format-detection.md) - API Reference complète

### Documents historiques
- [docs/archive/VALIDATION_159_FORMATS_OBSOLETE.md](../archive/VALIDATION_159_FORMATS_OBSOLETE.md) - Validation V3 (159 formats)
- [docs/archive/FORMAT_LIBRARY_159_OBSOLETE.md](../archive/FORMAT_LIBRARY_159_OBSOLETE.md) - Bibliothèque V3 (obsolète)
- [docs/archive/IMPLEMENTATION_COMPLETE_OBSOLETE.md](../archive/IMPLEMENTATION_COMPLETE_OBSOLETE.md) - Implémentation V2/V3

### Plans d'implémentation
- [docs/planning/ByteSize_Implementation_Plan.md](../planning/ByteSize_Implementation_Plan.md) - Plan ByteSize/ByteOrder

---

## 🔍 Navigation rapide

### Par fonctionnalité
- **Format Detection** → [FormatDetection_400.md](FormatDetection_400.md)
- **TBL Character Tables** → [docs/api-reference/features/tbl.md](../api-reference/features/tbl.md)
- **Bookmarks** → [docs/api-reference/features/bookmarks.md](../api-reference/features/bookmarks.md)
- **Highlights** → [docs/api-reference/features/highlights.md](../api-reference/features/highlights.md)
- **Compare Mode** → [docs/api-reference/features/compare.md](../api-reference/features/compare.md)

### Par audience
- **Utilisateurs finaux** → [FormatDetection_400.md](FormatDetection_400.md) (section "Utilisation")
- **Développeurs d'applications** → [docs/api-reference/](../api-reference/)
- **Contributeurs** → [FormatDetection_400.md](FormatDetection_400.md) (section "Contribution")
- **Mainteneurs** → [VALIDATION_400_FORMATS.md](VALIDATION_400_FORMATS.md)

---

## 📊 Statistiques

**400 formats** répartis en **20 catégories** :
- Images (47), Game (38), Video (30), Audio (30), Documents (28), Archives (28), Science (27), Programming (25), CAD (21), System (20), 3D (19), Database (18), Data (15), Network (12), Medical (12), Disk (10), Executables (6), Crypto (6), Fonts (5), Certificates (3)

**Plus de 800 extensions** de fichiers supportées.

**Performance** :
- Chargement : ~128ms (400 formats)
- Détection : 5-25ms par fichier
- Mémoire : ~6.5MB

---

## 🚀 Démarrage rapide

### Activer la détection automatique

```csharp
// Dans votre code XAML ou C#
hexEditor.EnableAutoFormatDetection = true;
hexEditor.FormatDefinitionsPath = "FormatDefinitions/";

// Ou via XAML
<hex:HexEditor
    EnableAutoFormatDetection="True"
    FormatDefinitionsPath="FormatDefinitions/" />
```

### Détecter manuellement

```csharp
var result = hexEditor.AutoDetectAndApplyFormat("example.dwg");
if (result.Success)
{
    Console.WriteLine($"Format: {result.Format.FormatName}");
    Console.WriteLine($"Blocks: {result.Blocks.Count}");
}
```

Voir [FormatDetection_400.md](FormatDetection_400.md) pour plus d'exemples.

---

## 📝 Contribution

Pour ajouter un nouveau format :

1. Créer un fichier JSON dans `FormatDefinitions/Category/`
2. Suivre la structure documentée dans [FormatDetection_400.md](FormatDetection_400.md)
3. Tester avec `dotnet test --filter FormatDetection_Tests`
4. Soumettre une Pull Request

---

**Dernière mise à jour** : 2026-02-22
**Version** : V4 (400 formats)
