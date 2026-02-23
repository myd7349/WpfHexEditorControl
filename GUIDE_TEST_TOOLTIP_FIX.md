# GUIDE DE TEST - Correctif Tooltip Persistence

## ✅ Correctif Appliqué avec Succès

Le fichier [SettingsStateService.cs](Sources/WPFHexaEditor/Core/Settings/SettingsStateService.cs) a été modifié pour ajouter :

1. **Logging détaillé** dans SaveState() et LoadState()
2. **Parsing case-insensitive** des enums pour plus de robustesse
3. **Messages d'erreur explicites** avec les valeurs valides en cas d'échec

## 📋 Procédure de Test

### Étape 1 : Compiler le Projet

```bash
cd Sources/WPFHexaEditor
dotnet build WpfHexEditorCore.csproj
```

✅ **Résultat attendu :** Compilation réussie (0 erreurs)

### Étape 2 : Lancer l'Application Sample.Main

1. Ouvrir la solution `WpfHexEditor.Sample.Main` dans Visual Studio
2. Configurer le build en **Debug** (important pour voir les logs)
3. Lancer l'application (F5)

### Étape 3 : Ouvrir la Fenêtre de Debug Output

Dans Visual Studio :
- Menu **Affichage** → **Sortie** (ou Ctrl+Alt+O)
- Sélectionner "Déboguer" dans le dropdown

### Étape 4 : Modifier les Paramètres Tooltip

1. Dans l'application, ouvrir le panneau **Settings** (bouton ⚙️)
2. Faire défiler jusqu'à la section **"Tooltip"**
3. Modifier les valeurs :
   - **Byte Tool Tip Display Mode** → "Everywhere"
   - **Byte Tool Tip Detail Level** → "Detailed"

### Étape 5 : Observer les Logs de Sauvegarde

1. **Fermer l'application** (cela déclenche `MainWindow_Closing`)
2. Dans la **fenêtre Output**, chercher les logs :

```
[SettingsStateService.SaveState] Starting save for XX properties
...
[SaveState] OK ByteToolTipDisplayMode = Everywhere (Enum: ByteToolTipDisplayMode)
[SaveState] OK ByteToolTipDetailLevel = Detailed (Enum: ByteToolTipDetailLevel)
...
[SettingsStateService.SaveState] Generated JSON (XXXX chars):
{
  ...
  "ByteToolTipDisplayMode": "Everywhere",
  "ByteToolTipDetailLevel": "Detailed",
  ...
}
```

**✅ Si vous voyez ces lignes :** Les propriétés sont **correctement découvertes et sauvegardées** !

**❌ Si les lignes sont manquantes :** Les propriétés ne sont **pas découvertes** → Problème dans PropertyDiscoveryService

**⚠️ Si vous voyez `[SaveState] SKIP ByteToolTipDisplayMode`** → La propriété est découverte mais pas sérialisée (problème de lecture)

### Étape 6 : Vérifier le Chargement

1. **Relancer l'application** (cela déclenche `MainWindow_Loaded`)
2. Dans la **fenêtre Output**, chercher les logs :

```
[SettingsStateService.LoadState] Loading from JSON (XXXX chars)
{
  ...
  "ByteToolTipDisplayMode": "Everywhere",
  "ByteToolTipDetailLevel": "Detailed",
  ...
}
[LoadState] Parsed XX settings from JSON
[LoadState] Discovered XX properties to restore
...
[LoadState] Parsing enum ByteToolTipDisplayMode: 'Everywhere' as ByteToolTipDisplayMode
[LoadState] OK ByteToolTipDisplayMode = Everywhere (ByteToolTipDisplayMode)
[LoadState] Parsing enum ByteToolTipDetailLevel: 'Detailed' as ByteToolTipDetailLevel
[LoadState] OK ByteToolTipDetailLevel = Detailed (ByteToolTipDetailLevel)
...
[SettingsStateService.LoadState] Completed
```

**✅ Si vous voyez ces lignes :** Les propriétés sont **correctement chargées et restaurées** !

### Étape 7 : Vérifier Visuellement

1. Ouvrir le panneau **Settings** à nouveau
2. Vérifier que les valeurs Tooltip sont **bien restaurées** :
   - **Byte Tool Tip Display Mode** = "Everywhere" ✓
   - **Byte Tool Tip Detail Level** = "Detailed" ✓

## 🔍 Analyse des Résultats

### Scénario A : ✅ Tout Fonctionne

Si les logs montrent que les propriétés sont sauvegardées ET chargées, le problème est **résolu** !

### Scénario B : ❌ Propriétés Non Découvertes

Si vous voyez :
```
[SettingsStateService.SaveState] Starting save for XX properties
```

Mais **PAS** de ligne pour `ByteToolTipDisplayMode`, alors :

**Cause :** PropertyDiscoveryService ne trouve pas ces propriétés
**Solution :** Vérifier que :
1. Les propriétés ont bien `[Category("Tooltip")]`
2. Les DependencyProperty sont bien nommés `ByteToolTipDisplayModeProperty`
3. Les propriétés ne sont pas `[Browsable(false)]`

### Scénario C : ❌ Propriétés Découvertes mais SKIP

Si vous voyez :
```
[SaveState] SKIP ByteToolTipDisplayMode - Property not found or not readable
```

**Cause :** La propriété CLR n'est pas accessible via réflexion
**Solution :** Vérifier que la propriété est `public` et a un getter

### Scénario D : ❌ Erreur de Parsing

Si vous voyez :
```
[LoadState] ERROR: Failed to parse 'SomeValue' as ByteToolTipDisplayMode
  Valid values: None, OnCustomBackgroundBlocks, Everywhere
```

**Cause :** La valeur dans le JSON ne correspond pas à une valeur enum valide
**Solution :** Vérifier l'orthographe et la casse dans le JSON

## 📝 Rapport de Test

Après avoir exécuté les tests, remplissez ce rapport :

### Résultats de la Sauvegarde
- [ ] Les logs `[SaveState]` apparaissent dans Output
- [ ] `ByteToolTipDisplayMode` est sauvegardé
- [ ] `ByteToolTipDetailLevel` est sauvegardé
- [ ] Le JSON complet est affiché dans les logs

### Résultats du Chargement
- [ ] Les logs `[LoadState]` apparaissent dans Output
- [ ] `ByteToolTipDisplayMode` est chargé depuis le JSON
- [ ] `ByteToolTipDetailLevel` est chargé depuis le JSON
- [ ] Les valeurs sont correctement appliquées au HexEditor

### Vérification Visuelle
- [ ] Les paramètres Tooltip persistent après redémarrage
- [ ] Les tooltips s'affichent selon les paramètres configurés

### Problèmes Rencontrés
_Décrivez ici tout problème observé dans les logs..._

## 🎯 Prochaines Étapes

Selon les résultats du test :

1. **Si le problème persiste :** Copiez les logs complets de SaveState/LoadState
2. **Si le problème est résolu :** On peut procéder à l'ajout des 24 propriétés manquantes
3. **Si un nouveau problème apparaît :** Analysez les messages d'erreur dans les logs

## 📞 Support

Si vous avez besoin d'aide pour interpréter les logs, copiez-les et partagez-les pour analyse détaillée.
