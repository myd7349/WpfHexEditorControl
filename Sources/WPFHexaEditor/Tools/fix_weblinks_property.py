#!/usr/bin/env python3
"""
Fix web_links property name to WebLinks for C# compatibility
Changes snake_case to PascalCase in references section
"""

import json
from pathlib import Path

def fix_json_file(file_path):
    """Fix web_links property in a single JSON file"""
    try:
        # Read JSON
        with open(file_path, 'r', encoding='utf-8-sig') as f:
            data = json.load(f)

        # Check if file has references with web_links
        if 'references' in data and isinstance(data['references'], dict):
            if 'web_links' in data['references']:
                # Rename web_links to WebLinks
                data['references']['WebLinks'] = data['references'].pop('web_links')

                # Write back
                with open(file_path, 'w', encoding='utf-8') as f:
                    json.dump(data, f, indent=2, ensure_ascii=False)

                return True, "Fixed"

        return False, "No web_links found"

    except Exception as e:
        return False, f"ERROR: {e}"

def main():
    script_dir = Path(__file__).parent
    format_defs_dir = script_dir.parent / 'FormatDefinitions'

    if not format_defs_dir.exists():
        print(f"ERROR: {format_defs_dir} not found")
        return

    print("=" * 70)
    print("FIXING web_links -> WebLinks IN ALL JSON FILES")
    print("=" * 70)

    # Find all JSON files
    all_json_files = list(format_defs_dir.rglob("*.json"))
    print(f"Found {len(all_json_files)} JSON files\n")

    fixed_count = 0
    skipped_count = 0
    error_count = 0

    for json_file in sorted(all_json_files):
        success, message = fix_json_file(json_file)

        if success:
            fixed_count += 1
            print(f"[FIXED] {json_file.stem}")
        elif "ERROR" in message:
            error_count += 1
            print(f"[ERROR] {json_file.stem}: {message}")
        else:
            skipped_count += 1

    print("\n" + "=" * 70)
    print("SUMMARY")
    print("=" * 70)
    print(f"Total files:        {len(all_json_files)}")
    print(f"Fixed:              {fixed_count}")
    print(f"Skipped (no links): {skipped_count}")
    print(f"Errors:             {error_count}")
    print("=" * 70)

if __name__ == '__main__':
    main()
