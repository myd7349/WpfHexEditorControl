#!/usr/bin/env python3
# Script to add ColorPicker translations to all .resx files

import os
import re

# Define translations for all languages
translations = {
    "Resources.resx": {  # English (default)
        "ColorPicker_Tab_Custom": "Custom",
        "ColorPicker_Tab_Palette": "Palette",
        "ColorPicker_Preview_Original": "Original",
        "ColorPicker_Preview_New": "New",
        "ColorPicker_Section_ColorSelector": "Color Selector",
        "ColorPicker_Section_RGBValues": "RGB Values",
        "ColorPicker_Section_HexColor": "Hex Color",
        "ColorPicker_Section_StandardColors": "Standard Colors",
        "ColorPicker_Section_RecentColors": "Recent Colors",
    },
    "Resources.ar-SA.resx": {  # Arabic
        "ColorPicker_Tab_Custom": "مخصص",
        "ColorPicker_Tab_Palette": "لوحة الألوان",
        "ColorPicker_Preview_Original": "الأصلي",
        "ColorPicker_Preview_New": "جديد",
        "ColorPicker_Section_ColorSelector": "منتقي الألوان",
        "ColorPicker_Section_RGBValues": "قيم RGB",
        "ColorPicker_Section_HexColor": "لون سداسي عشري",
        "ColorPicker_Section_StandardColors": "الألوان القياسية",
        "ColorPicker_Section_RecentColors": "الألوان الحديثة",
    },
    "Resources.de-DE.resx": {  # German
        "ColorPicker_Tab_Custom": "Benutzerdefiniert",
        "ColorPicker_Tab_Palette": "Palette",
        "ColorPicker_Preview_Original": "Original",
        "ColorPicker_Preview_New": "Neu",
        "ColorPicker_Section_ColorSelector": "Farbauswahl",
        "ColorPicker_Section_RGBValues": "RGB-Werte",
        "ColorPicker_Section_HexColor": "Hex-Farbe",
        "ColorPicker_Section_StandardColors": "Standardfarben",
        "ColorPicker_Section_RecentColors": "Zuletzt verwendete Farben",
    },
    "Resources.es-419.resx": {  # Spanish Latin America
        "ColorPicker_Tab_Custom": "Personalizado",
        "ColorPicker_Tab_Palette": "Paleta",
        "ColorPicker_Preview_Original": "Original",
        "ColorPicker_Preview_New": "Nuevo",
        "ColorPicker_Section_ColorSelector": "Selector de color",
        "ColorPicker_Section_RGBValues": "Valores RGB",
        "ColorPicker_Section_HexColor": "Color hexadecimal",
        "ColorPicker_Section_StandardColors": "Colores estándar",
        "ColorPicker_Section_RecentColors": "Colores recientes",
    },
    "Resources.es-ES.resx": {  # Spanish Spain
        "ColorPicker_Tab_Custom": "Personalizado",
        "ColorPicker_Tab_Palette": "Paleta",
        "ColorPicker_Preview_Original": "Original",
        "ColorPicker_Preview_New": "Nuevo",
        "ColorPicker_Section_ColorSelector": "Selector de color",
        "ColorPicker_Section_RGBValues": "Valores RGB",
        "ColorPicker_Section_HexColor": "Color hexadecimal",
        "ColorPicker_Section_StandardColors": "Colores estándar",
        "ColorPicker_Section_RecentColors": "Colores recientes",
    },
    "Resources.fr-CA.resx": {  # French Canada
        "ColorPicker_Tab_Custom": "Personnalisé",
        "ColorPicker_Tab_Palette": "Palette",
        "ColorPicker_Preview_Original": "Original",
        "ColorPicker_Preview_New": "Nouveau",
        "ColorPicker_Section_ColorSelector": "Sélecteur de couleur",
        "ColorPicker_Section_RGBValues": "Valeurs RVB",
        "ColorPicker_Section_HexColor": "Couleur hexadécimale",
        "ColorPicker_Section_StandardColors": "Couleurs standard",
        "ColorPicker_Section_RecentColors": "Couleurs récentes",
    },
    "Resources.fr-FR.resx": {  # French France
        "ColorPicker_Tab_Custom": "Personnalisé",
        "ColorPicker_Tab_Palette": "Palette",
        "ColorPicker_Preview_Original": "Original",
        "ColorPicker_Preview_New": "Nouveau",
        "ColorPicker_Section_ColorSelector": "Sélecteur de couleur",
        "ColorPicker_Section_RGBValues": "Valeurs RVB",
        "ColorPicker_Section_HexColor": "Couleur hexadécimale",
        "ColorPicker_Section_StandardColors": "Couleurs standard",
        "ColorPicker_Section_RecentColors": "Couleurs récentes",
    },
    "Resources.hi-IN.resx": {  # Hindi
        "ColorPicker_Tab_Custom": "कस्टम",
        "ColorPicker_Tab_Palette": "पैलेट",
        "ColorPicker_Preview_Original": "मूल",
        "ColorPicker_Preview_New": "नया",
        "ColorPicker_Section_ColorSelector": "रंग चयनकर्ता",
        "ColorPicker_Section_RGBValues": "RGB मान",
        "ColorPicker_Section_HexColor": "हेक्स रंग",
        "ColorPicker_Section_StandardColors": "मानक रंग",
        "ColorPicker_Section_RecentColors": "हाल के रंग",
    },
    "Resources.it-IT.resx": {  # Italian
        "ColorPicker_Tab_Custom": "Personalizzato",
        "ColorPicker_Tab_Palette": "Tavolozza",
        "ColorPicker_Preview_Original": "Originale",
        "ColorPicker_Preview_New": "Nuovo",
        "ColorPicker_Section_ColorSelector": "Selettore colore",
        "ColorPicker_Section_RGBValues": "Valori RGB",
        "ColorPicker_Section_HexColor": "Colore esadecimale",
        "ColorPicker_Section_StandardColors": "Colori standard",
        "ColorPicker_Section_RecentColors": "Colori recenti",
    },
    "Resources.ja-JP.resx": {  # Japanese
        "ColorPicker_Tab_Custom": "カスタム",
        "ColorPicker_Tab_Palette": "パレット",
        "ColorPicker_Preview_Original": "元の色",
        "ColorPicker_Preview_New": "新しい色",
        "ColorPicker_Section_ColorSelector": "カラーセレクター",
        "ColorPicker_Section_RGBValues": "RGB値",
        "ColorPicker_Section_HexColor": "16進数カラー",
        "ColorPicker_Section_StandardColors": "標準色",
        "ColorPicker_Section_RecentColors": "最近使用した色",
    },
    "Resources.ko-KR.resx": {  # Korean
        "ColorPicker_Tab_Custom": "사용자 지정",
        "ColorPicker_Tab_Palette": "팔레트",
        "ColorPicker_Preview_Original": "원본",
        "ColorPicker_Preview_New": "새로운",
        "ColorPicker_Section_ColorSelector": "색상 선택기",
        "ColorPicker_Section_RGBValues": "RGB 값",
        "ColorPicker_Section_HexColor": "16진수 색상",
        "ColorPicker_Section_StandardColors": "표준 색상",
        "ColorPicker_Section_RecentColors": "최근 색상",
    },
    "Resources.nl-NL.resx": {  # Dutch
        "ColorPicker_Tab_Custom": "Aangepast",
        "ColorPicker_Tab_Palette": "Palet",
        "ColorPicker_Preview_Original": "Origineel",
        "ColorPicker_Preview_New": "Nieuw",
        "ColorPicker_Section_ColorSelector": "Kleurenkiezer",
        "ColorPicker_Section_RGBValues": "RGB-waarden",
        "ColorPicker_Section_HexColor": "Hex-kleur",
        "ColorPicker_Section_StandardColors": "Standaardkleuren",
        "ColorPicker_Section_RecentColors": "Recente kleuren",
    },
    "Resources.pl-PL.resx": {  # Polish
        "ColorPicker_Tab_Custom": "Niestandardowe",
        "ColorPicker_Tab_Palette": "Paleta",
        "ColorPicker_Preview_Original": "Oryginał",
        "ColorPicker_Preview_New": "Nowy",
        "ColorPicker_Section_ColorSelector": "Wybór koloru",
        "ColorPicker_Section_RGBValues": "Wartości RGB",
        "ColorPicker_Section_HexColor": "Kolor hex",
        "ColorPicker_Section_StandardColors": "Kolory standardowe",
        "ColorPicker_Section_RecentColors": "Ostatnie kolory",
    },
    "Resources.pt-BR.resx": {  # Portuguese Brazil
        "ColorPicker_Tab_Custom": "Personalizado",
        "ColorPicker_Tab_Palette": "Paleta",
        "ColorPicker_Preview_Original": "Original",
        "ColorPicker_Preview_New": "Novo",
        "ColorPicker_Section_ColorSelector": "Seletor de cores",
        "ColorPicker_Section_RGBValues": "Valores RGB",
        "ColorPicker_Section_HexColor": "Cor hexadecimal",
        "ColorPicker_Section_StandardColors": "Cores padrão",
        "ColorPicker_Section_RecentColors": "Cores recentes",
    },
    "Resources.pt-PT.resx": {  # Portuguese Portugal
        "ColorPicker_Tab_Custom": "Personalizado",
        "ColorPicker_Tab_Palette": "Paleta",
        "ColorPicker_Preview_Original": "Original",
        "ColorPicker_Preview_New": "Novo",
        "ColorPicker_Section_ColorSelector": "Seletor de cores",
        "ColorPicker_Section_RGBValues": "Valores RGB",
        "ColorPicker_Section_HexColor": "Cor hexadecimal",
        "ColorPicker_Section_StandardColors": "Cores padrão",
        "ColorPicker_Section_RecentColors": "Cores recentes",
    },
    "Resources.ru-RU.resx": {  # Russian
        "ColorPicker_Tab_Custom": "Пользовательский",
        "ColorPicker_Tab_Palette": "Палитра",
        "ColorPicker_Preview_Original": "Исходный",
        "ColorPicker_Preview_New": "Новый",
        "ColorPicker_Section_ColorSelector": "Выбор цвета",
        "ColorPicker_Section_RGBValues": "Значения RGB",
        "ColorPicker_Section_HexColor": "Hex цвет",
        "ColorPicker_Section_StandardColors": "Стандартные цвета",
        "ColorPicker_Section_RecentColors": "Недавние цвета",
    },
    "Resources.sv-SE.resx": {  # Swedish
        "ColorPicker_Tab_Custom": "Anpassad",
        "ColorPicker_Tab_Palette": "Palett",
        "ColorPicker_Preview_Original": "Original",
        "ColorPicker_Preview_New": "Ny",
        "ColorPicker_Section_ColorSelector": "Färgväljare",
        "ColorPicker_Section_RGBValues": "RGB-värden",
        "ColorPicker_Section_HexColor": "Hex-färg",
        "ColorPicker_Section_StandardColors": "Standardfärger",
        "ColorPicker_Section_RecentColors": "Senaste färger",
    },
    "Resources.tr-TR.resx": {  # Turkish
        "ColorPicker_Tab_Custom": "Özel",
        "ColorPicker_Tab_Palette": "Palet",
        "ColorPicker_Preview_Original": "Orijinal",
        "ColorPicker_Preview_New": "Yeni",
        "ColorPicker_Section_ColorSelector": "Renk Seçici",
        "ColorPicker_Section_RGBValues": "RGB Değerleri",
        "ColorPicker_Section_HexColor": "Onaltılık Renk",
        "ColorPicker_Section_StandardColors": "Standart Renkler",
        "ColorPicker_Section_RecentColors": "Son Renkler",
    },
    "Resources.zh-CN.resx": {  # Chinese Simplified
        "ColorPicker_Tab_Custom": "自定义",
        "ColorPicker_Tab_Palette": "调色板",
        "ColorPicker_Preview_Original": "原始",
        "ColorPicker_Preview_New": "新建",
        "ColorPicker_Section_ColorSelector": "颜色选择器",
        "ColorPicker_Section_RGBValues": "RGB值",
        "ColorPicker_Section_HexColor": "十六进制颜色",
        "ColorPicker_Section_StandardColors": "标准颜色",
        "ColorPicker_Section_RecentColors": "最近使用的颜色",
    },
}

# Base path
base_path = r"c:\Users\khens\source\repos\WpfHexEditorControl\Sources\Samples\WpfHexEditor.Sample.Main\Properties"

# Process each file
for filename, trans in translations.items():
    filepath = os.path.join(base_path, filename)

    print(f"Processing {filename}...")

    try:
        # Read the file
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()

        # Check if already has ColorPicker translations
        if "ColorPicker_Tab_Custom" in content:
            print(f"  Already has ColorPicker translations, skipping")
            continue

        # Find the </root> tag
        root_pos = content.rfind('</root>')
        if root_pos == -1:
            print(f"  ERROR: Could not find </root> tag")
            continue

        # Build the new entries
        new_entries = "\n  <!-- ColorPicker Localizations -->\n"
        for key, value in trans.items():
            comment = key.replace("ColorPicker_", "").replace("_", " ")
            new_entries += f'  <data name="{key}" xml:space="preserve">\n'
            new_entries += f'    <value>{value}</value>\n'
            new_entries += f'    <comment>ColorPicker - {comment}</comment>\n'
            new_entries += f'  </data>\n'

        new_entries += "\n"

        # Insert before </root>
        new_content = content[:root_pos] + new_entries + content[root_pos:]

        # Write back
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(new_content)

        print(f"  OK - Added {len(trans)} translations")

    except Exception as e:
        print(f"  ERROR: {e}")

print("\nDone! All translations added.")
