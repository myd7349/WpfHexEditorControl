#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script to add TBL category header translation to all resource files
"""

import os
import xml.etree.ElementTree as ET
from pathlib import Path

# Define TBL translation for each language
TBL_TRANSLATIONS = {
    "en": "Character Table",  # Base (Resources.resx)
    "fr-FR": "Table de caracteres",
    "fr-CA": "Table de caracteres",
    "es-ES": "Tabla de caracteres",
    "es-419": "Tabla de caracteres",  # Latin America Spanish
    "de-DE": "Zeichentabelle",
    "it-IT": "Tabella caratteri",
    "pt-BR": "Tabela de caracteres",
    "pt-PT": "Tabela de caracteres",
    "ru-RU": "Таблица символов",
    "pl-PL": "Tabela znakow",
    "nl-NL": "Tekentabel",
    "sv-SE": "Teckentabell",
    "ja-JP": "文字テーブル",
    "ko-KR": "문자 테이블",
    "zh-CN": "字符表",
    "ar-SA": "جدول الأحرف",
    "hi-IN": "वर्ण तालिका",
    "tr-TR": "Karakter Tablosu",
}

def add_translation_to_resx(file_path, lang_code):
    """Add TBL category translation to a .resx file"""

    # Register namespace
    ET.register_namespace('', 'http://www.w3.org/2005/ResourceDictionary')

    # Parse the XML file
    tree = ET.parse(file_path)
    root = tree.getroot()

    # Get translation for this language
    translation = TBL_TRANSLATIONS.get(lang_code, TBL_TRANSLATIONS["en"])

    # Find existing data elements
    existing_keys = set()
    for data_elem in root.findall('data'):
        name = data_elem.get('name')
        if name:
            existing_keys.add(name)

    key = "HexSettings_TBL_Title"

    if key not in existing_keys:
        # Create new data element
        data_elem = ET.Element('data')
        data_elem.set('name', key)
        data_elem.set('xml:space', 'preserve')

        # Add value sub-element
        value_elem = ET.SubElement(data_elem, 'value')
        value_elem.text = translation

        # Append to root
        root.append(data_elem)

        # Format and save
        indent_xml(root)
        tree.write(file_path, encoding='utf-8', xml_declaration=True)
        print(f"SUCCESS: {file_path.name}: Added {key} = {translation}")
        return True
    else:
        print(f"SKIP: {file_path.name}: Translation already exists")
        return False

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

    print("Adding TBL Category Header Translation to Resource Files")
    print("=" * 60)

    total_added = 0

    # Process base file
    base_file = resources_dir / "Resources.resx"
    if base_file.exists():
        print(f"\nProcessing: {base_file.name}")
        if add_translation_to_resx(base_file, "en"):
            total_added += 1

    # Process localized files
    for resx_file in sorted(resources_dir.glob("Resources.*.resx")):
        # Extract language code from filename (e.g., Resources.fr-FR.resx -> fr-FR)
        lang_code = resx_file.stem.replace("Resources.", "")

        print(f"\nProcessing: {resx_file.name} ({lang_code})")
        if add_translation_to_resx(resx_file, lang_code):
            total_added += 1

    print("\n" + "=" * 60)
    print(f"Done! Total translations added: {total_added}")
    print("\nThe TBL category header will now be localized in all 19 languages!")

if __name__ == "__main__":
    main()
