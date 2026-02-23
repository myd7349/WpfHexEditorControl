// Test file to verify enum serialization in SettingsStateService
// This should be compiled as a test project or console app

using System;
using System.Collections.Generic;
using System.Text.Json;
using WpfHexaEditor.Core.Settings;
using WpfHexaEditor.Models;

namespace TestEnumSerialization
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Testing Enum Serialization ===\n");

            // Test 1: Direct enum serialization
            Console.WriteLine("Test 1: Direct enum serialization");
            var displayMode = ByteToolTipDisplayMode.Everywhere;
            var detailLevel = ByteToolTipDetailLevel.Detailed;

            var testDict = new Dictionary<string, object>
            {
                ["ByteToolTipDisplayMode"] = displayMode.ToString(),
                ["ByteToolTipDetailLevel"] = detailLevel.ToString()
            };

            var json = JsonSerializer.Serialize(testDict, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine($"Serialized JSON:\n{json}\n");

            // Test 2: Deserialization
            Console.WriteLine("Test 2: Deserialization");
            var deserialized = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            foreach (var kvp in deserialized)
            {
                Console.WriteLine($"  {kvp.Key} = {kvp.Value.GetString()}");

                // Try parsing back to enum
                if (kvp.Key == "ByteToolTipDisplayMode")
                {
                    var parsed = Enum.Parse(typeof(ByteToolTipDisplayMode), kvp.Value.GetString());
                    Console.WriteLine($"    Parsed back to: {parsed} (Type: {parsed.GetType().Name})");
                }
                else if (kvp.Key == "ByteToolTipDetailLevel")
                {
                    var parsed = Enum.Parse(typeof(ByteToolTipDetailLevel), kvp.Value.GetString());
                    Console.WriteLine($"    Parsed back to: {parsed} (Type: {parsed.GetType().Name})");
                }
            }

            // Test 3: PropertyDiscoveryService
            Console.WriteLine("\nTest 3: PropertyDiscoveryService");
            var discoveryService = new PropertyDiscoveryService(typeof(WpfHexaEditor.HexEditor));
            var properties = discoveryService.DiscoverProperties();

            Console.WriteLine($"Total properties discovered: {properties.Count}");

            var tooltipProps = properties.FindAll(p => p.Category == "Tooltip");
            Console.WriteLine($"Tooltip category properties: {tooltipProps.Count}");

            foreach (var prop in tooltipProps)
            {
                Console.WriteLine($"  - {prop.PropertyName} ({prop.PropertyType.Name})");
            }

            // Test 4: SettingsStateService
            Console.WriteLine("\nTest 4: SettingsStateService (would need actual HexEditor instance)");
            Console.WriteLine("  This requires running in the actual application context.");

            Console.WriteLine("\n=== Press any key to exit ===");
            Console.ReadKey();
        }
    }
}
