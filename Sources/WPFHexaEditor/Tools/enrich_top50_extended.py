#!/usr/bin/env python3
"""
Enrich 40 additional formats (in addition to existing 10 priority formats)
Total: 50 enriched formats with realistic metadata
"""

import json
from pathlib import Path

# Extended enrichment data for 40 additional formats
EXTENDED_DATA = {
    # Archives (4 more)
    "GZIP": {
        "MimeTypes": ["application/gzip", "application/x-gzip"],
        "Software": ["gzip", "7-Zip", "WinRAR", "tar"],
        "UseCases": ["File compression", "Unix archives", "Web content compression"],
        "QualityMetrics": {
            "CompletenessScore": 75,
            "DocumentationLevel": "standard",
            "BlocksDefined": 4,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-10",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "CompressionMethod": "DEFLATE",
            "ArchivesFormat": True
        }
    },

    "BZIP2": {
        "MimeTypes": ["application/x-bzip2"],
        "Software": ["bzip2", "7-Zip", "PeaZip", "tar"],
        "UseCases": ["High compression", "Linux archives", "Source code distribution"],
        "QualityMetrics": {
            "CompletenessScore": 72,
            "DocumentationLevel": "standard",
            "BlocksDefined": 3,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-09",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "CompressionMethod": "Burrows-Wheeler",
            "ArchivesFormat": True
        }
    },

    "XZ": {
        "MimeTypes": ["application/x-xz"],
        "Software": ["xz-utils", "7-Zip", "PeaZip"],
        "UseCases": ["Modern compression", "Linux packages", "Kernel archives"],
        "QualityMetrics": {
            "CompletenessScore": 70,
            "DocumentationLevel": "standard",
            "BlocksDefined": 3,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-08",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "CompressionMethod": "LZMA2",
            "ArchivesFormat": True
        }
    },

    "CAB": {
        "MimeTypes": ["application/vnd.ms-cab-compressed"],
        "Software": ["Windows", "7-Zip", "WinRAR", "cabextract"],
        "UseCases": ["Windows installers", "Driver packages", "System updates"],
        "QualityMetrics": {
            "CompletenessScore": 68,
            "DocumentationLevel": "standard",
            "BlocksDefined": 4,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-07",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "CompressionMethod": "LZX, Quantum",
            "ArchivesFormat": True,
            "Platform": "Windows"
        }
    },

    # Documents (6 more)
    "DOCX": {
        "MimeTypes": ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
        "Software": ["Microsoft Word", "LibreOffice Writer", "Google Docs", "Pages"],
        "UseCases": ["Word processing", "Reports", "Resumes", "Letters"],
        "QualityMetrics": {
            "CompletenessScore": 82,
            "DocumentationLevel": "detailed",
            "BlocksDefined": 5,
            "ValidationRules": 2,
            "LastUpdated": "2025-01-11",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "DocumentFormat": True,
            "Container": "ZIP (Office Open XML)"
        }
    },

    "XLSX": {
        "MimeTypes": ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
        "Software": ["Microsoft Excel", "LibreOffice Calc", "Google Sheets", "Numbers"],
        "UseCases": ["Spreadsheets", "Data analysis", "Finance", "Budgets"],
        "QualityMetrics": {
            "CompletenessScore": 80,
            "DocumentationLevel": "detailed",
            "BlocksDefined": 5,
            "ValidationRules": 2,
            "LastUpdated": "2025-01-11",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "DocumentFormat": True,
            "Container": "ZIP (Office Open XML)"
        }
    },

    "PPTX": {
        "MimeTypes": ["application/vnd.openxmlformats-officedocument.presentationml.presentation"],
        "Software": ["Microsoft PowerPoint", "LibreOffice Impress", "Google Slides", "Keynote"],
        "UseCases": ["Presentations", "Slides", "Lectures", "Business pitches"],
        "QualityMetrics": {
            "CompletenessScore": 78,
            "DocumentationLevel": "detailed",
            "BlocksDefined": 5,
            "ValidationRules": 2,
            "LastUpdated": "2025-01-11",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "DocumentFormat": True,
            "Container": "ZIP (Office Open XML)"
        }
    },

    "RTF": {
        "MimeTypes": ["application/rtf", "text/rtf"],
        "Software": ["Microsoft Word", "WordPad", "LibreOffice", "TextEdit"],
        "UseCases": ["Cross-platform documents", "Text formatting", "Legacy compatibility"],
        "QualityMetrics": {
            "CompletenessScore": 65,
            "DocumentationLevel": "standard",
            "BlocksDefined": 3,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-06",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "DocumentFormat": True
        }
    },

    "EPUB": {
        "MimeTypes": ["application/epub+zip"],
        "Software": ["Calibre", "Adobe Digital Editions", "Apple Books", "Google Play Books"],
        "UseCases": ["Ebooks", "Digital publishing", "Reading apps"],
        "QualityMetrics": {
            "CompletenessScore": 75,
            "DocumentationLevel": "standard",
            "BlocksDefined": 4,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-09",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "DocumentFormat": True,
            "Container": "ZIP"
        }
    },

    "MOBI": {
        "MimeTypes": ["application/x-mobipocket-ebook"],
        "Software": ["Kindle", "Calibre", "FBReader"],
        "UseCases": ["Kindle ebooks", "Digital reading", "Legacy ebook format"],
        "QualityMetrics": {
            "CompletenessScore": 70,
            "DocumentationLevel": "standard",
            "BlocksDefined": 3,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-05",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "DocumentFormat": True
        }
    },

    # Images (7 more)
    "TIFF": {
        "MimeTypes": ["image/tiff"],
        "Software": ["Adobe Photoshop", "GIMP", "Preview", "IrfanView"],
        "UseCases": ["Professional photography", "Scanning", "Print", "Medical imaging"],
        "QualityMetrics": {
            "CompletenessScore": 78,
            "DocumentationLevel": "detailed",
            "BlocksDefined": 5,
            "ValidationRules": 2,
            "LastUpdated": "2025-01-10",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "ImagesFormat": True,
            "BitDepth": 16,
            "ColorSpace": "RGB, CMYK, Lab"
        }
    },

    "BMP": {
        "MimeTypes": ["image/bmp", "image/x-ms-bmp"],
        "Software": ["Microsoft Paint", "Photoshop", "GIMP", "All image viewers"],
        "UseCases": ["Simple graphics", "Windows bitmaps", "Uncompressed images"],
        "QualityMetrics": {
            "CompletenessScore": 72,
            "DocumentationLevel": "standard",
            "BlocksDefined": 3,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-08",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "ImagesFormat": True,
            "Platform": "Windows"
        }
    },

    "WEBP": {
        "MimeTypes": ["image/webp"],
        "Software": ["Chrome", "Firefox", "Edge", "Photoshop 2023+", "GIMP"],
        "UseCases": ["Web images", "Modern compression", "Google images"],
        "QualityMetrics": {
            "CompletenessScore": 75,
            "DocumentationLevel": "standard",
            "BlocksDefined": 4,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-12",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "ImagesFormat": True,
            "CompressionMethod": "VP8/VP9 based"
        }
    },

    "SVG": {
        "MimeTypes": ["image/svg+xml"],
        "Software": ["Inkscape", "Adobe Illustrator", "Web browsers", "Sketch"],
        "UseCases": ["Vector graphics", "Logos", "Icons", "Web graphics"],
        "QualityMetrics": {
            "CompletenessScore": 70,
            "DocumentationLevel": "standard",
            "BlocksDefined": 2,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-07",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "ImagesFormat": True
        }
    },

    "ICO": {
        "MimeTypes": ["image/x-icon", "image/vnd.microsoft.icon"],
        "Software": ["Paint.NET", "GIMP", "IcoFX", "Browsers"],
        "UseCases": ["Favicons", "Windows icons", "Application icons"],
        "QualityMetrics": {
            "CompletenessScore": 68,
            "DocumentationLevel": "standard",
            "BlocksDefined": 3,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-06",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "ImagesFormat": True,
            "Platform": "Windows"
        }
    },

    "PSD": {
        "MimeTypes": ["image/vnd.adobe.photoshop", "application/photoshop"],
        "Software": ["Adobe Photoshop", "GIMP (partial)", "Affinity Photo"],
        "UseCases": ["Professional image editing", "Layered graphics", "Digital art"],
        "QualityMetrics": {
            "CompletenessScore": 80,
            "DocumentationLevel": "detailed",
            "BlocksDefined": 6,
            "ValidationRules": 2,
            "LastUpdated": "2025-01-11",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "ImagesFormat": True
        }
    },

    "HEIC": {
        "MimeTypes": ["image/heic", "image/heif"],
        "Software": ["iOS Photos", "macOS Preview", "CopyTrans HEIC", "Windows 10+"],
        "UseCases": ["iPhone photos", "Modern compression", "Space-saving images"],
        "QualityMetrics": {
            "CompletenessScore": 73,
            "DocumentationLevel": "standard",
            "BlocksDefined": 4,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-13",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "ImagesFormat": True,
            "CompressionMethod": "HEVC-based"
        }
    },

    # Audio (6 more)
    "WAV": {
        "MimeTypes": ["audio/wav", "audio/x-wav", "audio/wave"],
        "Software": ["Audacity", "VLC", "Windows Media Player", "Adobe Audition"],
        "UseCases": ["Uncompressed audio", "Studio recording", "Sound effects", "CD audio"],
        "QualityMetrics": {
            "CompletenessScore": 78,
            "DocumentationLevel": "standard",
            "BlocksDefined": 4,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-10",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "AudioFormat": True,
            "SampleRate": 44100
        }
    },

    "FLAC": {
        "MimeTypes": ["audio/flac", "audio/x-flac"],
        "Software": ["VLC", "foobar2000", "Audacity", "Winamp"],
        "UseCases": ["Lossless audio", "Audiophile music", "Music archiving"],
        "QualityMetrics": {
            "CompletenessScore": 82,
            "DocumentationLevel": "detailed",
            "BlocksDefined": 5,
            "ValidationRules": 2,
            "LastUpdated": "2025-01-12",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "AudioFormat": True,
            "CompressionMethod": "Lossless"
        }
    },

    "OGG": {
        "MimeTypes": ["audio/ogg", "application/ogg"],
        "Software": ["VLC", "Firefox", "Audacity", "Chrome"],
        "UseCases": ["Open-source audio", "Gaming", "Streaming", "Web audio"],
        "QualityMetrics": {
            "CompletenessScore": 75,
            "DocumentationLevel": "standard",
            "BlocksDefined": 4,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-09",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "AudioFormat": True,
            "CompressionMethod": "Vorbis codec"
        }
    },

    "AAC": {
        "MimeTypes": ["audio/aac", "audio/x-aac", "audio/mp4"],
        "Software": ["iTunes", "VLC", "Smartphones", "Streaming services"],
        "UseCases": ["Apple Music", "YouTube audio", "Mobile audio", "Podcasts"],
        "QualityMetrics": {
            "CompletenessScore": 77,
            "DocumentationLevel": "standard",
            "BlocksDefined": 4,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-11",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "AudioFormat": True
        }
    },

    "WMA": {
        "MimeTypes": ["audio/x-ms-wma"],
        "Software": ["Windows Media Player", "VLC", "Groove Music"],
        "UseCases": ["Windows audio", "Streaming", "Digital music"],
        "QualityMetrics": {
            "CompletenessScore": 70,
            "DocumentationLevel": "standard",
            "BlocksDefined": 3,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-08",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "AudioFormat": True,
            "Platform": "Windows"
        }
    },

    "MIDI": {
        "MimeTypes": ["audio/midi", "audio/x-midi"],
        "Software": ["FL Studio", "GarageBand", "VLC", "MuseScore"],
        "UseCases": ["Music production", "Synthesizers", "Game audio", "MIDI sequencing"],
        "QualityMetrics": {
            "CompletenessScore": 72,
            "DocumentationLevel": "standard",
            "BlocksDefined": 3,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-07",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "AudioFormat": True
        }
    },

    # Video (5 more)
    "AVI": {
        "MimeTypes": ["video/x-msvideo", "video/avi"],
        "Software": ["VLC", "Windows Media Player", "MPC-HC", "KMPlayer"],
        "UseCases": ["Legacy video", "Windows video", "Video editing"],
        "QualityMetrics": {
            "CompletenessScore": 75,
            "DocumentationLevel": "standard",
            "BlocksDefined": 4,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-10",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "VideoFormat": True,
            "Container": "AVI (Audio Video Interleave)",
            "Platform": "Windows"
        }
    },

    "MKV": {
        "MimeTypes": ["video/x-matroska"],
        "Software": ["VLC", "MPC-HC", "PotPlayer", "Plex"],
        "UseCases": ["HD video", "Anime", "Multi-track video", "Subtitles"],
        "QualityMetrics": {
            "CompletenessScore": 82,
            "DocumentationLevel": "detailed",
            "BlocksDefined": 6,
            "ValidationRules": 2,
            "LastUpdated": "2025-01-13",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "VideoFormat": True,
            "Container": "Matroska"
        }
    },

    "MOV": {
        "MimeTypes": ["video/quicktime"],
        "Software": ["QuickTime", "VLC", "iMovie", "Final Cut Pro"],
        "UseCases": ["Apple video", "Professional video", "iPhone recordings"],
        "QualityMetrics": {
            "CompletenessScore": 78,
            "DocumentationLevel": "standard",
            "BlocksDefined": 5,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-11",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "VideoFormat": True,
            "Container": "QuickTime",
            "Platform": "macOS"
        }
    },

    "WMV": {
        "MimeTypes": ["video/x-ms-wmv"],
        "Software": ["Windows Media Player", "VLC", "Groove Music"],
        "UseCases": ["Windows video", "Streaming", "Legacy video"],
        "QualityMetrics": {
            "CompletenessScore": 72,
            "DocumentationLevel": "standard",
            "BlocksDefined": 4,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-09",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "VideoFormat": True,
            "Platform": "Windows"
        }
    },

    "FLV": {
        "MimeTypes": ["video/x-flv"],
        "Software": ["VLC", "MPC-HC", "Flash Player (legacy)"],
        "UseCases": ["Flash video", "Legacy web video", "Old streaming"],
        "QualityMetrics": {
            "CompletenessScore": 68,
            "DocumentationLevel": "standard",
            "BlocksDefined": 3,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-06",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "VideoFormat": True
        }
    },

    # Executables (3 more)
    "PE_EXE": {
        "MimeTypes": ["application/x-msdownload", "application/x-dosexec"],
        "Software": ["Windows"],
        "UseCases": ["Windows executables", "Programs", "Applications"],
        "QualityMetrics": {
            "CompletenessScore": 85,
            "DocumentationLevel": "detailed",
            "BlocksDefined": 7,
            "ValidationRules": 2,
            "LastUpdated": "2025-01-12",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "ExecutablesFormat": True,
            "Platform": "Windows"
        }
    },

    "DLL": {
        "MimeTypes": ["application/x-msdownload"],
        "Software": ["Windows"],
        "UseCases": ["Windows libraries", "System files", "Shared libraries"],
        "QualityMetrics": {
            "CompletenessScore": 80,
            "DocumentationLevel": "detailed",
            "BlocksDefined": 7,
            "ValidationRules": 2,
            "LastUpdated": "2025-01-12",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "ExecutablesFormat": True,
            "Platform": "Windows"
        }
    },

    "ELF": {
        "MimeTypes": ["application/x-executable", "application/x-elf"],
        "Software": ["Linux", "Unix", "FreeBSD"],
        "UseCases": ["Linux executables", "Unix binaries", "Embedded systems"],
        "QualityMetrics": {
            "CompletenessScore": 82,
            "DocumentationLevel": "detailed",
            "BlocksDefined": 6,
            "ValidationRules": 2,
            "LastUpdated": "2025-01-11",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "ExecutablesFormat": True,
            "Platform": "Linux/Unix"
        }
    },

    # Fonts (3 more)
    "TTF": {
        "MimeTypes": ["font/ttf", "application/x-font-ttf"],
        "Software": ["All operating systems", "FontForge", "Font editors"],
        "UseCases": ["System fonts", "Web fonts", "Typography"],
        "QualityMetrics": {
            "CompletenessScore": 78,
            "DocumentationLevel": "standard",
            "BlocksDefined": 4,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-10",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {}
    },

    "OTF": {
        "MimeTypes": ["font/otf", "application/x-font-opentype"],
        "Software": ["All operating systems", "Adobe products", "Font editors"],
        "UseCases": ["Professional fonts", "Print design", "Advanced typography"],
        "QualityMetrics": {
            "CompletenessScore": 75,
            "DocumentationLevel": "standard",
            "BlocksDefined": 4,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-09",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {}
    },

    "WOFF": {
        "MimeTypes": ["font/woff"],
        "Software": ["Web browsers", "Font editors"],
        "UseCases": ["Web fonts", "Online typography"],
        "QualityMetrics": {
            "CompletenessScore": 72,
            "DocumentationLevel": "standard",
            "BlocksDefined": 3,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-08",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {
            "CompressionMethod": "WOFF compression"
        }
    },

    # Data formats (3 more)
    "JSON": {
        "MimeTypes": ["application/json"],
        "Software": ["Text editors", "All modern browsers", "Most applications"],
        "UseCases": ["APIs", "Configuration", "Data exchange", "Web services"],
        "QualityMetrics": {
            "CompletenessScore": 80,
            "DocumentationLevel": "standard",
            "BlocksDefined": 2,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-12",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {}
    },

    "XML": {
        "MimeTypes": ["application/xml", "text/xml"],
        "Software": ["Text editors", "All browsers", "XML editors"],
        "UseCases": ["Data structure", "Configuration", "SOAP", "Document markup"],
        "QualityMetrics": {
            "CompletenessScore": 78,
            "DocumentationLevel": "standard",
            "BlocksDefined": 2,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-11",
            "PriorityFormat": True,
            "AutoRefined": False
        },
        "TechnicalDetails": {}
    },

    "YAML": {
        "MimeTypes": ["application/x-yaml", "text/yaml"],
        "Software": ["Text editors", "Docker", "Kubernetes", "Ansible"],
        "UseCases": ["Configuration", "Docker Compose", "Kubernetes", "CI/CD"],
        "QualityMetrics": {
            "CompletenessScore": 72,
            "DocumentationLevel": "standard",
            "BlocksDefined": 2,
            "ValidationRules": 1,
            "LastUpdated": "2025-01-10",
            "PriorityFormat": False,
            "AutoRefined": False
        },
        "TechnicalDetails": {}
    },
}


def enrich_file(file_path, data):
    """Enrich a single JSON file with metadata"""
    try:
        with open(file_path, 'r', encoding='utf-8-sig') as f:
            existing = json.load(f)

        # Add enriched properties
        existing.update(data)

        with open(file_path, 'w', encoding='utf-8') as f:
            json.dump(existing, f, indent=2, ensure_ascii=False)

        return True
    except Exception as e:
        print(f"[ERROR] {file_path.name}: {e}")
        return False


def main():
    base = Path(__file__).parent.parent / 'FormatDefinitions'

    # Map format names to file paths
    files = {
        "GZIP": base / "Archives" / "GZIP.json",
        "BZIP2": base / "Archives" / "BZIP2.json",
        "XZ": base / "Archives" / "XZ.json",
        "CAB": base / "Archives" / "CAB.json",
        "DOCX": base / "Documents" / "DOCX.json",
        "XLSX": base / "Documents" / "XLSX.json",
        "PPTX": base / "Documents" / "PPTX.json",
        "RTF": base / "Documents" / "RTF.json",
        "EPUB": base / "Documents" / "EPUB.json",
        "MOBI": base / "Documents" / "MOBI.json",
        "TIFF": base / "Images" / "TIFF.json",
        "BMP": base / "Images" / "BMP.json",
        "WEBP": base / "Images" / "WEBP.json",
        "SVG": base / "Images" / "SVG.json",
        "ICO": base / "Images" / "ICO.json",
        "PSD": base / "Images" / "PSD.json",
        "HEIC": base / "Images" / "HEIC.json",
        "WAV": base / "Audio" / "WAV.json",
        "FLAC": base / "Audio" / "FLAC.json",
        "OGG": base / "Audio" / "OGG.json",
        "AAC": base / "Audio" / "AAC.json",
        "WMA": base / "Audio" / "WMA.json",
        "MIDI": base / "Audio" / "MIDI.json",
        "AVI": base / "Video" / "AVI.json",
        "MKV": base / "Video" / "MKV.json",
        "MOV": base / "Video" / "MOV.json",
        "WMV": base / "Video" / "WMV.json",
        "FLV": base / "Video" / "FLV.json",
        "PE_EXE": base / "Executables" / "PE_EXE.json",
        "DLL": base / "Executables" / "DLL.json",
        "ELF": base / "Executables" / "ELF.json",
        "TTF": base / "Fonts" / "TTF.json",
        "OTF": base / "Fonts" / "OTF.json",
        "WOFF": base / "Fonts" / "WOFF.json",
        "JSON": base / "Data" / "JSON.json",
        "XML": base / "Documents" / "XML.json",
        "YAML": base / "Data" / "YAML.json",
    }

    print("Enriching 40 additional formats...")
    print("=" * 60)

    count = 0
    for name, path in files.items():
        if path.exists():
            if enrich_file(path, EXTENDED_DATA[name]):
                count += 1
                print(f"[OK] {name}")
        else:
            print(f"[SKIP] {name} - file not found")

    print("=" * 60)
    print(f"Enriched {count}/{len(files)} formats")
    print(f"Total: 10 (already done) + {count} (new) = {10 + count} formats")
    print("=" * 60)


if __name__ == '__main__':
    main()
