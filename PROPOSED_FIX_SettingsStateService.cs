// PROPOSED FIX: Enhanced SettingsStateService with better enum handling and logging
// Replace the SaveState and LoadState methods in SettingsStateService.cs with these improved versions

/// <summary>
/// Saves all discovered properties to JSON string.
/// Automatically serializes all properties with [Category] attribute.
/// </summary>
public string SaveState(object hexEditorInstance)
{
    if (hexEditorInstance == null)
        throw new ArgumentNullException(nameof(hexEditorInstance));

    var properties = _discoveryService.DiscoverProperties();
    var state = new Dictionary<string, object>();

    System.Diagnostics.Debug.WriteLine($"[SettingsStateService.SaveState] Starting save for {properties.Count} properties");

    foreach (var prop in properties)
    {
        try
        {
            var propInfo = hexEditorInstance.GetType().GetProperty(prop.PropertyName);
            if (propInfo == null || !propInfo.CanRead)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveState] SKIP {prop.PropertyName} - Property not found or not readable");
                continue;
            }

            var value = propInfo.GetValue(hexEditorInstance);
            if (value == null)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveState] SKIP {prop.PropertyName} - Value is null");
                continue;
            }

            // Serialize based on type
            if (prop.PropertyType == typeof(Color))
            {
                var hexColor = ColorToHex((Color)value);
                state[prop.PropertyName] = hexColor;
                System.Diagnostics.Debug.WriteLine($"[SaveState] OK {prop.PropertyName} = {hexColor} (Color)");
            }
            else if (prop.PropertyType.IsEnum)
            {
                var enumString = value.ToString();
                state[prop.PropertyName] = enumString;
                System.Diagnostics.Debug.WriteLine($"[SaveState] OK {prop.PropertyName} = {enumString} (Enum: {prop.PropertyType.Name})");
            }
            else if (prop.PropertyType == typeof(bool) ||
                     prop.PropertyType == typeof(int) ||
                     prop.PropertyType == typeof(double) ||
                     prop.PropertyType == typeof(long))
            {
                state[prop.PropertyName] = value;
                System.Diagnostics.Debug.WriteLine($"[SaveState] OK {prop.PropertyName} = {value} ({prop.PropertyType.Name})");
            }
            else if (prop.PropertyType == typeof(string))
            {
                state[prop.PropertyName] = value.ToString();
                System.Diagnostics.Debug.WriteLine($"[SaveState] OK {prop.PropertyName} = {value} (string)");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SaveState] SKIP {prop.PropertyName} - Unsupported type: {prop.PropertyType.Name}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SaveState] ERROR {prop.PropertyName}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"  Exception type: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"  Stack trace: {ex.StackTrace}");
        }
    }

    var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
    System.Diagnostics.Debug.WriteLine($"[SettingsStateService.SaveState] Generated JSON ({json.Length} chars):");
    System.Diagnostics.Debug.WriteLine(json);

    return json;
}

/// <summary>
/// Loads settings from JSON string and applies them to HexEditor instance.
/// Automatically deserializes all discovered properties.
/// </summary>
public void LoadState(object hexEditorInstance, string json)
{
    if (hexEditorInstance == null)
        throw new ArgumentNullException(nameof(hexEditorInstance));

    if (string.IsNullOrEmpty(json))
    {
        System.Diagnostics.Debug.WriteLine("[SettingsStateService.LoadState] JSON is empty - nothing to load");
        return;
    }

    try
    {
        System.Diagnostics.Debug.WriteLine($"[SettingsStateService.LoadState] Loading from JSON ({json.Length} chars)");
        System.Diagnostics.Debug.WriteLine(json);

        var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        if (settings == null)
        {
            System.Diagnostics.Debug.WriteLine("[LoadState] ERROR: Deserialization returned null");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[LoadState] Parsed {settings.Count} settings from JSON");

        var properties = _discoveryService.DiscoverProperties();
        System.Diagnostics.Debug.WriteLine($"[LoadState] Discovered {properties.Count} properties to restore");

        foreach (var prop in properties)
        {
            if (!settings.TryGetValue(prop.PropertyName, out var jsonValue))
            {
                System.Diagnostics.Debug.WriteLine($"[LoadState] SKIP {prop.PropertyName} - Not found in JSON");
                continue;
            }

            try
            {
                var propInfo = hexEditorInstance.GetType().GetProperty(prop.PropertyName);
                if (propInfo == null || !propInfo.CanWrite)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadState] SKIP {prop.PropertyName} - Property not found or not writable");
                    continue;
                }

                object value = null;

                // Deserialize based on type
                if (prop.PropertyType == typeof(bool))
                {
                    value = jsonValue.GetBoolean();
                }
                else if (prop.PropertyType == typeof(int))
                {
                    value = jsonValue.GetInt32();
                }
                else if (prop.PropertyType == typeof(double))
                {
                    value = jsonValue.GetDouble();
                }
                else if (prop.PropertyType == typeof(long))
                {
                    value = jsonValue.GetInt64();
                }
                else if (prop.PropertyType == typeof(Color))
                {
                    value = HexToColor(jsonValue.GetString());
                }
                else if (prop.PropertyType.IsEnum)
                {
                    var enumString = jsonValue.GetString();
                    System.Diagnostics.Debug.WriteLine($"[LoadState] Parsing enum {prop.PropertyName}: '{enumString}' as {prop.PropertyType.Name}");

                    // Try parsing with case-insensitive first
                    if (Enum.TryParse(prop.PropertyType, enumString, true, out var enumValue))
                    {
                        value = enumValue;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadState] ERROR: Failed to parse '{enumString}' as {prop.PropertyType.Name}");
                        System.Diagnostics.Debug.WriteLine($"  Valid values: {string.Join(", ", Enum.GetNames(prop.PropertyType))}");
                        continue;
                    }
                }
                else if (prop.PropertyType == typeof(string))
                {
                    value = jsonValue.GetString();
                }

                if (value != null)
                {
                    propInfo.SetValue(hexEditorInstance, value);
                    System.Diagnostics.Debug.WriteLine($"[LoadState] OK {prop.PropertyName} = {value} ({prop.PropertyType.Name})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadState] SKIP {prop.PropertyName} - Value is null after parsing");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadState] ERROR {prop.PropertyName}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"  Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"  Stack trace: {ex.StackTrace}");
            }
        }

        System.Diagnostics.Debug.WriteLine("[SettingsStateService.LoadState] Completed");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[LoadState] FATAL ERROR: Failed to parse settings JSON: {ex.Message}");
        throw;
    }
}
