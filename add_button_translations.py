# -*- coding: utf-8 -*-
import xml.etree.ElementTree as ET
import os
import sys

# Configure console for UTF-8 on Windows
if sys.platform == 'win32':
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

# Translations for button labels
translations = {
    'en-US': {
        'HexSettings_SaveButton': 'Save State',
        'HexSettings_LoadButton': 'Load State',
        'HexSettings_ResetButton': 'Reset to Defaults'
    },
    'fr-CA': {
        'HexSettings_SaveButton': 'Sauvegarder l\'état',
        'HexSettings_LoadButton': 'Charger l\'état',
        'HexSettings_ResetButton': 'Réinitialiser tout par défaut'
    },
    'fr-FR': {
        'HexSettings_SaveButton': 'Sauvegarder l\'état',
        'HexSettings_LoadButton': 'Charger l\'état',
        'HexSettings_ResetButton': 'Réinitialiser tout par défaut'
    },
    'es-ES': {
        'HexSettings_SaveButton': 'Guardar estado',
        'HexSettings_LoadButton': 'Cargar estado',
        'HexSettings_ResetButton': 'Restablecer valores predeterminados'
    },
    'es-419': {
        'HexSettings_SaveButton': 'Guardar estado',
        'HexSettings_LoadButton': 'Cargar estado',
        'HexSettings_ResetButton': 'Restablecer valores predeterminados'
    },
    'de-DE': {
        'HexSettings_SaveButton': 'Zustand speichern',
        'HexSettings_LoadButton': 'Zustand laden',
        'HexSettings_ResetButton': 'Auf Standardwerte zurücksetzen'
    },
    'it-IT': {
        'HexSettings_SaveButton': 'Salva stato',
        'HexSettings_LoadButton': 'Carica stato',
        'HexSettings_ResetButton': 'Ripristina impostazioni predefinite'
    },
    'ja-JP': {
        'HexSettings_SaveButton': '状態を保存',
        'HexSettings_LoadButton': '状態を読み込む',
        'HexSettings_ResetButton': 'デフォルトにリセット'
    },
    'ko-KR': {
        'HexSettings_SaveButton': '상태 저장',
        'HexSettings_LoadButton': '상태 불러오기',
        'HexSettings_ResetButton': '기본값으로 재설정'
    },
    'pl-PL': {
        'HexSettings_SaveButton': 'Zapisz stan',
        'HexSettings_LoadButton': 'Wczytaj stan',
        'HexSettings_ResetButton': 'Przywróć domyślne'
    },
    'pt-BR': {
        'HexSettings_SaveButton': 'Salvar estado',
        'HexSettings_LoadButton': 'Carregar estado',
        'HexSettings_ResetButton': 'Redefinir para padrão'
    },
    'pt-PT': {
        'HexSettings_SaveButton': 'Guardar estado',
        'HexSettings_LoadButton': 'Carregar estado',
        'HexSettings_ResetButton': 'Repor predefinições'
    },
    'ru-RU': {
        'HexSettings_SaveButton': 'Сохранить состояние',
        'HexSettings_LoadButton': 'Загрузить состояние',
        'HexSettings_ResetButton': 'Сбросить до значений по умолчанию'
    },
    'zh-CN': {
        'HexSettings_SaveButton': '保存状态',
        'HexSettings_LoadButton': '加载状态',
        'HexSettings_ResetButton': '重置为默认值'
    },
    'zh-TW': {
        'HexSettings_SaveButton': '保存狀態',
        'HexSettings_LoadButton': '載入狀態',
        'HexSettings_ResetButton': '重置為預設值'
    },
    'ar-SA': {
        'HexSettings_SaveButton': 'حفظ الحالة',
        'HexSettings_LoadButton': 'تحميل الحالة',
        'HexSettings_ResetButton': 'إعادة تعيين إلى الافتراضي'
    },
    'tr-TR': {
        'HexSettings_SaveButton': 'Durumu Kaydet',
        'HexSettings_LoadButton': 'Durumu Yükle',
        'HexSettings_ResetButton': 'Varsayılanlara Sıfırla'
    },
    'nl-NL': {
        'HexSettings_SaveButton': 'Status opslaan',
        'HexSettings_LoadButton': 'Status laden',
        'HexSettings_ResetButton': 'Standaardwaarden herstellen'
    },
    'sv-SE': {
        'HexSettings_SaveButton': 'Spara tillstånd',
        'HexSettings_LoadButton': 'Ladda tillstånd',
        'HexSettings_ResetButton': 'Återställ till standard'
    },
    'hi-IN': {
        'HexSettings_SaveButton': 'स्थिति सहेजें',
        'HexSettings_LoadButton': 'स्थिति लोड करें',
        'HexSettings_ResetButton': 'डिफ़ॉल्ट पर रीसेट करें'
    },
    'zh-TW': {
        'HexSettings_SaveButton': '保存狀態',
        'HexSettings_LoadButton': '載入狀態',
        'HexSettings_ResetButton': '重置為預設值'
    }
}

def add_button_translations():
    base_path = r"C:\Users\khens\source\repos\WpfHexEditorControl\Sources\Samples\WpfHexEditor.Sample.Main\Properties"

    total_added = 0

    for culture, trans in translations.items():
        file_path = os.path.join(base_path, f"Resources.{culture}.resx")

        if not os.path.exists(file_path):
            print(f"Skipping {culture} (file not found)")
            continue

        try:
            # Parse XML
            tree = ET.parse(file_path)
            root = tree.getroot()

            # Check which keys already exist
            existing_keys = {elem.attrib['name'] for elem in root.findall(".//data[@name]")}

            added_count = 0

            # Add each translation
            for key, value in trans.items():
                if key not in existing_keys:
                    # Create new data element
                    data = ET.Element('data', name=key)
                    data.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')

                    value_elem = ET.SubElement(data, 'value')
                    value_elem.text = value

                    # Add to root
                    root.append(data)
                    added_count += 1

            if added_count > 0:
                # Save with UTF-8 BOM
                tree.write(file_path, encoding='utf-8', xml_declaration=True)
                print(f"Added {added_count} translations to {culture}")
                total_added += added_count
            else:
                print(f"No new translations needed for {culture}")

        except Exception as e:
            print(f"Error processing {culture}: {e}")

    print(f"\nTotal: Added {total_added} translations across all languages")

if __name__ == '__main__':
    add_button_translations()
