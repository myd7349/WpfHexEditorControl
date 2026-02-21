#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script to add category header translations to all resource files
Categories: StatusBar, Display, Colors, Behavior, Data, Visual, Keyboard
"""

import os
import xml.etree.ElementTree as ET
from pathlib import Path

# Define translations for each category in each language
TRANSLATIONS = {
    "en": {  # Base (Resources.resx)
        "HexSettings_StatusBar_Title": "Status Bar",
        "HexSettings_Display_Title": "Display",
        "HexSettings_Colors_Title": "Colors",
        "HexSettings_Behavior_Title": "Behavior",
        "HexSettings_Data_Title": "Data",
        "HexSettings_Visual_Title": "Visual",
        "HexSettings_Keyboard_Title": "Keyboard",
    },
    "fr-FR": {
        "HexSettings_StatusBar_Title": "Barre d'état",
        "HexSettings_Display_Title": "Affichage",
        "HexSettings_Colors_Title": "Couleurs",
        "HexSettings_Behavior_Title": "Comportement",
        "HexSettings_Data_Title": "Données",
        "HexSettings_Visual_Title": "Visuel",
        "HexSettings_Keyboard_Title": "Clavier",
    },
    "fr-CA": {
        "HexSettings_StatusBar_Title": "Barre d'état",
        "HexSettings_Display_Title": "Affichage",
        "HexSettings_Colors_Title": "Couleurs",
        "HexSettings_Behavior_Title": "Comportement",
        "HexSettings_Data_Title": "Données",
        "HexSettings_Visual_Title": "Visuel",
        "HexSettings_Keyboard_Title": "Clavier",
    },
    "es-ES": {
        "HexSettings_StatusBar_Title": "Barra de estado",
        "HexSettings_Display_Title": "Visualización",
        "HexSettings_Colors_Title": "Colores",
        "HexSettings_Behavior_Title": "Comportamiento",
        "HexSettings_Data_Title": "Datos",
        "HexSettings_Visual_Title": "Visual",
        "HexSettings_Keyboard_Title": "Teclado",
    },
    "es-419": {  # Latin America Spanish
        "HexSettings_StatusBar_Title": "Barra de estado",
        "HexSettings_Display_Title": "Visualización",
        "HexSettings_Colors_Title": "Colores",
        "HexSettings_Behavior_Title": "Comportamiento",
        "HexSettings_Data_Title": "Datos",
        "HexSettings_Visual_Title": "Visual",
        "HexSettings_Keyboard_Title": "Teclado",
    },
    "de-DE": {
        "HexSettings_StatusBar_Title": "Statusleiste",
        "HexSettings_Display_Title": "Anzeige",
        "HexSettings_Colors_Title": "Farben",
        "HexSettings_Behavior_Title": "Verhalten",
        "HexSettings_Data_Title": "Daten",
        "HexSettings_Visual_Title": "Visuell",
        "HexSettings_Keyboard_Title": "Tastatur",
    },
    "it-IT": {
        "HexSettings_StatusBar_Title": "Barra di stato",
        "HexSettings_Display_Title": "Visualizzazione",
        "HexSettings_Colors_Title": "Colori",
        "HexSettings_Behavior_Title": "Comportamento",
        "HexSettings_Data_Title": "Dati",
        "HexSettings_Visual_Title": "Visivo",
        "HexSettings_Keyboard_Title": "Tastiera",
    },
    "pt-BR": {
        "HexSettings_StatusBar_Title": "Barra de status",
        "HexSettings_Display_Title": "Exibição",
        "HexSettings_Colors_Title": "Cores",
        "HexSettings_Behavior_Title": "Comportamento",
        "HexSettings_Data_Title": "Dados",
        "HexSettings_Visual_Title": "Visual",
        "HexSettings_Keyboard_Title": "Teclado",
    },
    "pt-PT": {
        "HexSettings_StatusBar_Title": "Barra de estado",
        "HexSettings_Display_Title": "Visualização",
        "HexSettings_Colors_Title": "Cores",
        "HexSettings_Behavior_Title": "Comportamento",
        "HexSettings_Data_Title": "Dados",
        "HexSettings_Visual_Title": "Visual",
        "HexSettings_Keyboard_Title": "Teclado",
    },
    "ru-RU": {
        "HexSettings_StatusBar_Title": "Строка состояния",
        "HexSettings_Display_Title": "Отображение",
        "HexSettings_Colors_Title": "Цвета",
        "HexSettings_Behavior_Title": "Поведение",
        "HexSettings_Data_Title": "Данные",
        "HexSettings_Visual_Title": "Визуальные",
        "HexSettings_Keyboard_Title": "Клавиатура",
    },
    "pl-PL": {
        "HexSettings_StatusBar_Title": "Pasek stanu",
        "HexSettings_Display_Title": "Wyświetlanie",
        "HexSettings_Colors_Title": "Kolory",
        "HexSettings_Behavior_Title": "Zachowanie",
        "HexSettings_Data_Title": "Dane",
        "HexSettings_Visual_Title": "Wizualne",
        "HexSettings_Keyboard_Title": "Klawiatura",
    },
    "nl-NL": {
        "HexSettings_StatusBar_Title": "Statusbalk",
        "HexSettings_Display_Title": "Weergave",
        "HexSettings_Colors_Title": "Kleuren",
        "HexSettings_Behavior_Title": "Gedrag",
        "HexSettings_Data_Title": "Gegevens",
        "HexSettings_Visual_Title": "Visueel",
        "HexSettings_Keyboard_Title": "Toetsenbord",
    },
    "sv-SE": {
        "HexSettings_StatusBar_Title": "Statusfält",
        "HexSettings_Display_Title": "Visning",
        "HexSettings_Colors_Title": "Färger",
        "HexSettings_Behavior_Title": "Beteende",
        "HexSettings_Data_Title": "Data",
        "HexSettings_Visual_Title": "Visuellt",
        "HexSettings_Keyboard_Title": "Tangentbord",
    },
    "ja-JP": {
        "HexSettings_StatusBar_Title": "ステータスバー",
        "HexSettings_Display_Title": "表示",
        "HexSettings_Colors_Title": "色",
        "HexSettings_Behavior_Title": "動作",
        "HexSettings_Data_Title": "データ",
        "HexSettings_Visual_Title": "ビジュアル",
        "HexSettings_Keyboard_Title": "キーボード",
    },
    "ko-KR": {
        "HexSettings_StatusBar_Title": "상태 표시줄",
        "HexSettings_Display_Title": "표시",
        "HexSettings_Colors_Title": "색상",
        "HexSettings_Behavior_Title": "동작",
        "HexSettings_Data_Title": "데이터",
        "HexSettings_Visual_Title": "시각적",
        "HexSettings_Keyboard_Title": "키보드",
    },
    "zh-CN": {
        "HexSettings_StatusBar_Title": "状态栏",
        "HexSettings_Display_Title": "显示",
        "HexSettings_Colors_Title": "颜色",
        "HexSettings_Behavior_Title": "行为",
        "HexSettings_Data_Title": "数据",
        "HexSettings_Visual_Title": "视觉",
        "HexSettings_Keyboard_Title": "键盘",
    },
    "ar-SA": {
        "HexSettings_StatusBar_Title": "شريط الحالة",
        "HexSettings_Display_Title": "العرض",
        "HexSettings_Colors_Title": "الألوان",
        "HexSettings_Behavior_Title": "السلوك",
        "HexSettings_Data_Title": "البيانات",
        "HexSettings_Visual_Title": "البصري",
        "HexSettings_Keyboard_Title": "لوحة المفاتيح",
    },
    "hi-IN": {
        "HexSettings_StatusBar_Title": "स्थिति पट्टी",
        "HexSettings_Display_Title": "प्रदर्शन",
        "HexSettings_Colors_Title": "रंग",
        "HexSettings_Behavior_Title": "व्यवहार",
        "HexSettings_Data_Title": "डेटा",
        "HexSettings_Visual_Title": "दृश्य",
        "HexSettings_Keyboard_Title": "कीबोर्ड",
    },
    "tr-TR": {
        "HexSettings_StatusBar_Title": "Durum Çubuğu",
        "HexSettings_Display_Title": "Görüntü",
        "HexSettings_Colors_Title": "Renkler",
        "HexSettings_Behavior_Title": "Davranış",
        "HexSettings_Data_Title": "Veri",
        "HexSettings_Visual_Title": "Görsel",
        "HexSettings_Keyboard_Title": "Klavye",
    },
}

def add_translations_to_resx(file_path, lang_code):
    """Add missing category translations to a .resx file"""

    # Register namespace
    ET.register_namespace('', 'http://www.w3.org/2005/ResourceDictionary')

    # Parse the XML file
    tree = ET.parse(file_path)
    root = tree.getroot()

    # Get translations for this language
    translations = TRANSLATIONS.get(lang_code, TRANSLATIONS["en"])

    # Find existing data elements
    existing_keys = set()
    for data_elem in root.findall('data'):
        name = data_elem.get('name')
        if name:
            existing_keys.add(name)

    # Count additions
    added_count = 0

    # Add missing translations
    for key, value in translations.items():
        if key not in existing_keys:
            # Create new data element
            data_elem = ET.Element('data')
            data_elem.set('name', key)
            data_elem.set('xml:space', 'preserve')

            # Add value sub-element
            value_elem = ET.SubElement(data_elem, 'value')
            value_elem.text = value

            # Append to root
            root.append(data_elem)
            added_count += 1
            print(f"  + Added: {key} = {value}")

    if added_count > 0:
        # Format and save
        indent_xml(root)
        tree.write(file_path, encoding='utf-8', xml_declaration=True)
        print(f"SUCCESS: {file_path.name}: Added {added_count} translations")
    else:
        print(f"SKIP: {file_path.name}: All translations already exist")

    return added_count

def indent_xml(elem, level=0):
    """Pretty print XML with proper indentation"""
    indent = "\n" + level * "  "
    if len(elem):
        if not elem.text or not elem.text.strip():
            elem.text = indent + "  "
        if not elem.tail or not elem.tail.strip():
            elem.tail = indent
        for child in elem:
            indent_xml(child, level + 1)
        if not child.tail or not child.tail.strip():
            child.tail = indent
    else:
        if level and (not elem.tail or not elem.tail.strip()):
            elem.tail = indent

def main():
    # Set UTF-8 encoding for Windows console
    import sys
    import io
    if sys.platform == 'win32':
        sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
        sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

    resources_dir = Path(r"C:\Users\khens\source\repos\WpfHexEditorControl\Sources\Samples\WpfHexEditor.Sample.Main\Properties")

    print("Adding Category Header Translations to Resource Files")
    print("=" * 60)

    total_added = 0

    # Process base file
    base_file = resources_dir / "Resources.resx"
    if base_file.exists():
        print(f"\nProcessing: {base_file.name}")
        total_added += add_translations_to_resx(base_file, "en")

    # Process localized files
    for resx_file in sorted(resources_dir.glob("Resources.*.resx")):
        # Extract language code from filename (e.g., Resources.fr-FR.resx -> fr-FR)
        lang_code = resx_file.stem.replace("Resources.", "")

        print(f"\nProcessing: {resx_file.name} ({lang_code})")
        total_added += add_translations_to_resx(resx_file, lang_code)

    print("\n" + "=" * 60)
    print(f"Done! Total translations added: {total_added}")
    print("\nThe category headers will now be localized in all 19 languages!")

if __name__ == "__main__":
    main()
