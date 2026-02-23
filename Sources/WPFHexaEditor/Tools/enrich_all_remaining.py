#!/usr/bin/env python3
"""
Enrich ALL remaining format definitions with metadata
Automatically generates appropriate metadata based on category and format name
"""

import json
import os
from pathlib import Path
from datetime import datetime

# Generic software by category
CATEGORY_SOFTWARE = {
    "Archives": ["7-Zip", "WinRAR", "WinZip", "PeaZip", "The Unarchiver"],
    "Images": ["Photoshop", "GIMP", "Paint.NET", "IrfanView", "XnView"],
    "Audio": ["VLC", "Audacity", "Foobar2000", "MediaMonkey", "Winamp"],
    "Video": ["VLC", "Media Player Classic", "HandBrake", "FFmpeg", "MPC-HC"],
    "Documents": ["Adobe Acrobat", "Microsoft Office", "LibreOffice", "FoxIt Reader", "Calibre"],
    "Executables": ["IDA Pro", "Ghidra", "PE Explorer", "Dependency Walker", "CFF Explorer"],
    "Fonts": ["FontForge", "FontLab", "Glyphs", "Font Creator", "BirdFont"],
    "3D": ["Blender", "3ds Max", "Maya", "Cinema 4D", "MeshLab"],
    "Game": ["Emulator", "ROM Manager", "RetroArch", "Hex Editor", "Game Debugger"],
    "Network": ["Wireshark", "tcpdump", "NetworkMiner", "Fiddler", "Charles Proxy"],
    "Programming": ["Visual Studio", "GCC", "LLVM", "IDA Pro", "Ghidra"],
    "Database": ["SQL Server", "MySQL Workbench", "PostgreSQL", "SQLite Browser", "DBeaver"],
    "Disk": ["DiskInternals", "R-Studio", "HxD", "FTK Imager", "Autopsy"]
}

# Generic use cases by category
CATEGORY_USECASES = {
    "Archives": ["File compression", "Archive storage", "Backup", "Distribution"],
    "Images": ["Image viewing", "Photo editing", "Graphics design", "Web publishing"],
    "Audio": ["Music playback", "Audio editing", "Sound production", "Streaming"],
    "Video": ["Video playback", "Video editing", "Encoding", "Streaming"],
    "Documents": ["Document viewing", "Reading", "Publishing", "Archival"],
    "Executables": ["Program execution", "Reverse engineering", "Analysis", "Debugging"],
    "Fonts": ["Typography", "Font installation", "Design work", "Publishing"],
    "3D": ["3D modeling", "Rendering", "Animation", "Game development"],
    "Game": ["Gaming", "ROM hacking", "Emulation", "Preservation"],
    "Network": ["Network analysis", "Packet capture", "Debugging", "Security analysis"],
    "Programming": ["Software development", "Compilation", "Linking", "Analysis"],
    "Database": ["Data storage", "Query execution", "Data analysis", "Backup"],
    "Disk": ["Disk imaging", "Forensics", "Data recovery", "Analysis"]
}

def get_category_from_path(file_path):
    """Extract category from file path"""
    parts = file_path.parts
    if len(parts) >= 2:
        return parts[-2]  # Category is the parent folder name
    return "Other"

def generate_mime_type(format_name, category):
    """Generate a reasonable MIME type"""
    format_lower = format_name.lower()

    if category == "Images":
        return [f"image/{format_lower}"]
    elif category == "Audio":
        return [f"audio/{format_lower}"]
    elif category == "Video":
        return [f"video/{format_lower}"]
    elif category == "Archives":
        return [f"application/x-{format_lower}-compressed"]
    elif category == "Documents":
        return [f"application/{format_lower}"]
    elif category == "Executables":
        return [f"application/x-executable"]
    elif category == "Fonts":
        return [f"font/{format_lower}"]
    else:
        return [f"application/octet-stream"]

def calculate_completeness_score(data, category):
    """Calculate completeness score based on existing fields"""
    score = 50  # Base score

    # Add points for defined fields
    if data.get("Blocks") and len(data["Blocks"]) > 0:
        score += min(len(data["Blocks"]) * 3, 20)

    if data.get("Description"):
        score += 5

    if data.get("Extensions") and len(data["Extensions"]) > 0:
        score += 5

    if data.get("References") and len(data["References"]) > 0:
        score += 5

    # Category-specific adjustments
    if category in ["Archives", "Images", "Audio", "Video", "Documents"]:
        score += 5  # More common formats

    return min(score, 85)  # Cap at 85 for auto-enriched

def determine_documentation_level(completeness_score):
    """Determine documentation level from score"""
    if completeness_score >= 85:
        return "detailed"
    elif completeness_score >= 70:
        return "standard"
    else:
        return "basic"

def generate_technical_details(category, format_name):
    """Generate category-appropriate technical details"""
    details = {}

    if category == "Archives":
        details["ArchivesFormat"] = True
        details["SupportsEncryption"] = False
    elif category == "Images":
        details["ImagesFormat"] = True
        details["BitDepth"] = 8
    elif category == "Audio":
        details["AudioFormat"] = True
    elif category == "Video":
        details["VideoFormat"] = True
    elif category == "Documents":
        details["DocumentFormat"] = True
    elif category == "Executables":
        details["ExecutablesFormat"] = True
    elif category == "Game":
        details["Platform"] = "Various"

    return details

def enrich_format(file_path):
    """Enrich a single format file"""
    try:
        # Read existing JSON
        with open(file_path, 'r', encoding='utf-8-sig') as f:
            data = json.load(f)

        # Skip if already enriched
        if "QualityMetrics" in data:
            return False, "Already enriched"

        # Get category
        category = get_category_from_path(file_path.relative_to(file_path.parents[1]))
        format_name = file_path.stem

        # Generate MIME types
        if "MimeTypes" not in data or not data["MimeTypes"]:
            data["MimeTypes"] = generate_mime_type(format_name, category)

        # Generate Software list
        software = CATEGORY_SOFTWARE.get(category, ["Hex Editor", "File Viewer", "Archive Manager"])
        data["Software"] = software[:5]  # Limit to 5

        # Generate Use Cases
        use_cases = CATEGORY_USECASES.get(category, ["File storage", "Data exchange", "Archive"])
        data["UseCases"] = use_cases[:4]  # Limit to 4

        # Calculate quality metrics
        completeness_score = calculate_completeness_score(data, category)
        blocks_count = len(data.get("Blocks", []))

        data["QualityMetrics"] = {
            "CompletenessScore": completeness_score,
            "DocumentationLevel": determine_documentation_level(completeness_score),
            "BlocksDefined": blocks_count,
            "ValidationRules": 0,
            "LastUpdated": datetime.now().strftime("%Y-%m-%d"),
            "PriorityFormat": False,
            "AutoRefined": True
        }

        # Generate technical details
        data["TechnicalDetails"] = generate_technical_details(category, format_name)

        # Write back
        with open(file_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)

        return True, category

    except Exception as e:
        return False, str(e)

def main():
    script_dir = Path(__file__).parent
    format_defs_dir = script_dir.parent / 'FormatDefinitions'

    if not format_defs_dir.exists():
        print(f"ERROR: {format_defs_dir} not found")
        return

    print("=" * 70)
    print("ENRICHING ALL REMAINING FORMAT DEFINITIONS")
    print("=" * 70)

    # Find all JSON files
    all_json_files = list(format_defs_dir.rglob("*.json"))

    print(f"Found {len(all_json_files)} format definition files\n")

    # Process each file
    enriched_count = 0
    already_enriched = 0
    failed = []
    category_stats = {}

    for json_file in sorted(all_json_files):
        success, result = enrich_format(json_file)

        if success:
            enriched_count += 1
            category = result
            category_stats[category] = category_stats.get(category, 0) + 1
            print(f"[OK] {json_file.stem:<30} ({category})")
        elif result == "Already enriched":
            already_enriched += 1
        else:
            failed.append((json_file.stem, result))
            print(f"[FAIL] {json_file.stem}: {result}")

    print("\n" + "=" * 70)
    print("ENRICHMENT SUMMARY")
    print("=" * 70)
    print(f"Total files found:      {len(all_json_files)}")
    print(f"Already enriched:       {already_enriched}")
    print(f"Newly enriched:         {enriched_count}")
    print(f"Failed:                 {len(failed)}")
    print(f"TOTAL ENRICHED:         {already_enriched + enriched_count}")
    print("=" * 70)

    if category_stats:
        print("\nENRICHMENT BY CATEGORY:")
        print("-" * 40)
        for category, count in sorted(category_stats.items(), key=lambda x: -x[1]):
            print(f"  {category:<20} {count:>4} formats")

    if failed:
        print("\nFAILED FILES:")
        print("-" * 40)
        for name, error in failed:
            print(f"  {name}: {error}")

    print("\n" + "=" * 70)

if __name__ == '__main__':
    main()
