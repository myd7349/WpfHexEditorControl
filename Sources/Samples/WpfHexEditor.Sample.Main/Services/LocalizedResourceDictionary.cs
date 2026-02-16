using System;
using System.Collections;
using System.Resources;
using System.Windows;
using WpfHexEditor.Sample.Main.Properties;

namespace WpfHexEditor.Sample.Main.Services
{
    /// <summary>
    /// A dynamic ResourceDictionary that populates itself with localized strings
    /// from the application's Resources.resx files and automatically updates
    /// when the culture changes.
    ///
    /// Usage in XAML:
    /// &lt;Text="{DynamicResource Menu_File_Open}"/&gt;
    /// </summary>
    public class LocalizedResourceDictionary : ResourceDictionary
    {
        private readonly ResourceManager _resourceManager;

        public LocalizedResourceDictionary()
        {
            _resourceManager = Resources.ResourceManager;

            // Subscribe to culture changes
            DynamicResourceManager.CultureChanged += OnCultureChanged;

            // Initial population with current culture
            UpdateResources();

            System.Diagnostics.Debug.WriteLine("[LocalizedResourceDictionary] Created and populated with current culture resources");
        }

        /// <summary>
        /// Called when the application culture changes.
        /// Clears and repopulates the dictionary with the new culture's strings.
        /// </summary>
        private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[LocalizedResourceDictionary] Culture changed from '{e.OldCulture.Name}' to '{e.NewCulture.Name}', updating resources...");
            UpdateResources();
        }

        /// <summary>
        /// Populates the ResourceDictionary with all localized strings from the current culture.
        /// </summary>
        private void UpdateResources()
        {
            // Clear existing resources
            Clear();

            try
            {
                // Get the resource set for the current culture
                var culture = DynamicResourceManager.CurrentCulture;
                var resourceSet = _resourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: true);

                if (resourceSet != null)
                {
                    int count = 0;
                    // Add all resources to the dictionary
                    foreach (DictionaryEntry entry in resourceSet)
                    {
                        if (entry.Key is string key && entry.Value is string value)
                        {
                            this[key] = value;
                            count++;
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[LocalizedResourceDictionary] Loaded {count} resources for culture '{culture.Name}'");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[LocalizedResourceDictionary] WARNING: No resource set found for culture '{culture.Name}'");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalizedResourceDictionary] ERROR updating resources: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LocalizedResourceDictionary] Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Cleanup: unsubscribe from events when the dictionary is disposed.
        /// </summary>
        ~LocalizedResourceDictionary()
        {
            DynamicResourceManager.CultureChanged -= OnCultureChanged;
        }
    }
}
