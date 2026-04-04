"""
expand_whfmt.py
Rewrites every SourceCode .whfmt so that:
  1. The "formattingRules" object is pretty-printed (one key per line, indented).
  2. A "previewSnippet" string is added.
  3. A "previewSamples" object (before/after per rule) is added.
  4. The JSON "version" field is bumped to VERSION_NEW.
  5. The header comment "Updated:" date is refreshed to TODAY.

Usage:  python expand_whfmt.py
Run from any directory — uses __file__ to locate the whfmt folder.
"""

import json, re, sys, pathlib
from datetime import date

ROOT        = pathlib.Path(__file__).parent.parent / "FormatDefinitions/Programming/SourceCode"
TODAY       = date.today().strftime("%Y-%m-%d")   # e.g. "2026-04-10"
VERSION_NEW = "2.1"

# ── Per-language metadata ──────────────────────────────────────────────────────
LANGS = {
"CSharp": {
    "snippet": "using System.IO;\nusing System;\n\nclass Example{\nvoid Go(int x,int y){\nif(x>0){\nswitch(x){\ncase 1:\nreturn;\n}\n}\n}\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if(x>0){",                                    "after": "if (x > 0) {"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2;",                                    "after": "a = b + c * 2;"},
        "spaceAfterComma":             {"before": "Go(x,y,z);",                                  "after": "Go(x, y, z);"},
        "indentCaseLabels":            {"before": "switch(x){\ncase 1:\nbreak;",                 "after": "switch (x) {\n  case 1:\n    break;"},
        "organizeImports":             {"before": "using System.IO;\nusing System;",             "after": "using System;\nusing System.IO;"},
        "trimTrailingWhitespace":      {"before": "int x = 1;   ",                              "after": "int x = 1;"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"JavaScript": {
    "snippet": "import path from 'path';\nimport fs from 'fs';\n\nfunction go(x,y){\nif(x>0){\nswitch(x){\ncase 1:\nreturn;\n}\n}\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if(x>0){",                                    "after": "if (x > 0) {"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2;",                                    "after": "a = b + c * 2;"},
        "spaceAfterComma":             {"before": "go(x,y,z);",                                  "after": "go(x, y, z);"},
        "organizeImports":             {"before": "import path from 'path';\nimport fs from 'fs';","after": "import fs from 'fs';\nimport path from 'path';"},
        "trimTrailingWhitespace":      {"before": "let x = 1;   ",                              "after": "let x = 1;"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"TypeScript": {
    "snippet": "import {readFile} from 'fs';\nimport {join} from 'path';\n\nfunction go(x:number,y:number):void{\nif(x>0){\nswitch(x){\ncase 1:\nreturn;\n}\n}\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if(x>0){",                                    "after": "if (x > 0) {"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2;",                                    "after": "a = b + c * 2;"},
        "spaceAfterComma":             {"before": "go(x,y,z);",                                  "after": "go(x, y, z);"},
        "organizeImports":             {"before": "import {readFile} from 'fs';\nimport {join} from 'path';","after": "import {join} from 'path';\nimport {readFile} from 'fs';"},
        "trimTrailingWhitespace":      {"before": "let x: number = 1;   ",                      "after": "let x: number = 1;"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"Python": {
    "snippet": "import os\nimport abc\n\ndef go(x,y):\n    if x>0:\n        return x+y",
    "samples": {
        "spaceAfterKeywords":          {"before": "if x>0:",                                     "after": "if x > 0:"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2",                                     "after": "a = b + c * 2"},
        "spaceAfterComma":             {"before": "go(x,y,z)",                                   "after": "go(x, y, z)"},
        "organizeImports":             {"before": "import os\nimport abc",                       "after": "import abc\nimport os"},
        "trimTrailingWhitespace":      {"before": "x = 1   ",                                   "after": "x = 1"},
        "insertFinalNewline":          {"before": "return x",                                    "after": "return x\n"},
    }
},
"CSS": {
    "snippet": ".box{display:flex;margin:0 auto;}\n@media(max-width:768px){.box{display:block;}}",
    "samples": {
        "spaceAfterKeywords":          {"before": "@media(max-width:768px)",                     "after": "@media (max-width: 768px)"},
        "spaceAfterComma":             {"before": ".a,.b,.c {",                                  "after": ".a, .b, .c {"},
        "trimTrailingWhitespace":      {"before": "color: red;   ",                              "after": "color: red;"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"Java": {
    "snippet": "import java.util.List;\nimport java.io.File;\n\nclass Example{\nvoid go(int x,int y){\nif(x>0){\nswitch(x){\ncase 1:\nreturn;\n}\n}\n}\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if(x>0){",                                    "after": "if (x > 0) {"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2;",                                    "after": "a = b + c * 2;"},
        "spaceAfterComma":             {"before": "go(x,y,z);",                                  "after": "go(x, y, z);"},
        "organizeImports":             {"before": "import java.util.List;\nimport java.io.File;","after": "import java.io.File;\nimport java.util.List;"},
        "trimTrailingWhitespace":      {"before": "int x = 1;   ",                              "after": "int x = 1;"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"Kotlin": {
    "snippet": "import kotlin.io.*\nimport kotlin.math.*\n\nfun go(x:Int,y:Int){\nif(x>0){\nwhen(x){\n1->return\n}\n}\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if(x>0){",                                    "after": "if (x > 0) {"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2",                                     "after": "a = b + c * 2"},
        "spaceAfterComma":             {"before": "go(x,y,z)",                                   "after": "go(x, y, z)"},
        "trimTrailingWhitespace":      {"before": "val x = 1   ",                               "after": "val x = 1"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"Go": {
    "snippet": "import (\n    \"fmt\"\n    \"os\"\n)\n\nfunc goFunc(x,y int){\nif x>0{\nswitch x{\ncase 1:\nreturn\n}\n}\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if x>0{",                                     "after": "if x > 0 {"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2",                                     "after": "a = b + c * 2"},
        "spaceAfterComma":             {"before": "goFunc(x,y,z)",                               "after": "goFunc(x, y, z)"},
        "trimTrailingWhitespace":      {"before": "x := 1   ",                                  "after": "x := 1"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"Rust": {
    "snippet": "use std::io;\nuse std::fs;\n\nfn go(x:i32,y:i32){\nif x>0{\nmatch x{\n1=>return,\n_=>{}\n}\n}\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if x>0{",                                     "after": "if x > 0 {"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2;",                                    "after": "a = b + c * 2;"},
        "spaceAfterComma":             {"before": "go(x,y,z);",                                  "after": "go(x, y, z);"},
        "organizeImports":             {"before": "use std::io;\nuse std::fs;",                  "after": "use std::fs;\nuse std::io;"},
        "trimTrailingWhitespace":      {"before": "let x = 1;   ",                              "after": "let x = 1;"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"Swift": {
    "snippet": "import Foundation\nimport UIKit\n\nfunc go(x:Int,y:Int){\nif x>0{\nswitch x{\ncase 1:\nreturn\ndefault:\nbreak\n}\n}\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if x>0{",                                     "after": "if x > 0 {"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2",                                     "after": "a = b + c * 2"},
        "spaceAfterComma":             {"before": "go(x,y,z)",                                   "after": "go(x, y, z)"},
        "trimTrailingWhitespace":      {"before": "let x = 1   ",                               "after": "let x = 1"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"Ruby": {
    "snippet": "require 'ostruct'\nrequire 'json'\n\ndef go(x,y)\nif x>0\ncase x\nwhen 1\nreturn\nend\nend\nend",
    "samples": {
        "spaceAfterKeywords":          {"before": "if x>0",                                      "after": "if x > 0"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2",                                     "after": "a = b + c * 2"},
        "spaceAfterComma":             {"before": "go(x,y,z)",                                   "after": "go(x, y, z)"},
        "trimTrailingWhitespace":      {"before": "x = 1   ",                                   "after": "x = 1"},
        "insertFinalNewline":          {"before": "end",                                         "after": "end\n"},
    }
},
"Dart": {
    "snippet": "import 'dart:io';\nimport 'dart:math';\n\nvoid go(int x,int y){\nif(x>0){\nswitch(x){\ncase 1:\nreturn;\n}\n}\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if(x>0){",                                    "after": "if (x > 0) {"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2;",                                    "after": "a = b + c * 2;"},
        "spaceAfterComma":             {"before": "go(x,y,z);",                                  "after": "go(x, y, z);"},
        "trimTrailingWhitespace":      {"before": "int x = 1;   ",                              "after": "int x = 1;"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"PHP": {
    "snippet": "<?php\nuse Foo\\Bar;\nuse Abc\\Def;\n\nfunction go($x,$y){\nif($x>0){\nswitch($x){\ncase 1:\nreturn;\n}\n}\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if($x>0){",                                   "after": "if ($x > 0) {"},
        "spaceAroundBinaryOperators":  {"before": "$a=$b+$c*2;",                                 "after": "$a = $b + $c * 2;"},
        "spaceAfterComma":             {"before": "go($x,$y,$z);",                               "after": "go($x, $y, $z);"},
        "trimTrailingWhitespace":      {"before": "$x = 1;   ",                                 "after": "$x = 1;"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"Perl": {
    "snippet": "use strict;\nuse warnings;\n\nsub go{\nmy($x,$y)=@_;\nif($x>0){\nreturn $x+$y;\n}\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if($x>0){",                                   "after": "if ($x > 0) {"},
        "spaceAroundBinaryOperators":  {"before": "$a=$b+$c*2;",                                 "after": "$a = $b + $c * 2;"},
        "spaceAfterComma":             {"before": "go($x,$y,$z);",                               "after": "go($x, $y, $z);"},
        "trimTrailingWhitespace":      {"before": "$x = 1;   ",                                 "after": "$x = 1;"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"Shell": {
    "snippet": "#!/bin/bash\n\ngo() {\n    if [ $1 -gt 0 ]; then\n        echo $1\n    fi\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if[$1 -gt 0];then",                           "after": "if [ $1 -gt 0 ]; then"},
        "spaceAfterComma":             {"before": "echo $1,$2",                                  "after": "echo $1, $2"},
        "trimTrailingWhitespace":      {"before": "echo ok   ",                                  "after": "echo ok"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"PowerShell": {
    "snippet": "function Go($x,$y){\nif($x -gt 0){\nswitch($x){\n1{ return }\n}\n}\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if($x -gt 0){",                               "after": "if ($x -gt 0) {"},
        "spaceAroundBinaryOperators":  {"before": "$a=$b+$c*2",                                  "after": "$a = $b + $c * 2"},
        "spaceAfterComma":             {"before": "Go($x,$y,$z)",                                "after": "Go($x, $y, $z)"},
        "trimTrailingWhitespace":      {"before": "$x = 1   ",                                  "after": "$x = 1"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"VBNet": {
    "snippet": "Imports System.IO\nImports System\n\nClass Example\n    Sub Go(x As Integer,y As Integer)\n        If x>0 Then\n            Select Case x\n                Case 1\n                    Return\n            End Select\n        End If\n    End Sub\nEnd Class",
    "samples": {
        "spaceAfterKeywords":          {"before": "If x>0 Then",                                 "after": "If x > 0 Then"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2",                                     "after": "a = b + c * 2"},
        "spaceAfterComma":             {"before": "Go(x,y,z)",                                   "after": "Go(x, y, z)"},
        "organizeImports":             {"before": "Imports System.IO\nImports System",           "after": "Imports System\nImports System.IO"},
        "trimTrailingWhitespace":      {"before": "Dim x = 1   ",                               "after": "Dim x = 1"},
        "insertFinalNewline":          {"before": "End Class",                                   "after": "End Class\n"},
    }
},
"FSharp": {
    "snippet": "open System.IO\nopen System\n\nlet go x y =\n    if x > 0 then\n        match x with\n        | 1 -> ()\n        | _ -> ()",
    "samples": {
        "spaceAfterKeywords":          {"before": "if x>0 then",                                 "after": "if x > 0 then"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2",                                     "after": "a = b + c * 2"},
        "spaceAfterComma":             {"before": "go(x,y,z)",                                   "after": "go (x, y, z)"},
        "organizeImports":             {"before": "open System.IO\nopen System",                 "after": "open System\nopen System.IO"},
        "trimTrailingWhitespace":      {"before": "let x = 1   ",                               "after": "let x = 1"},
        "insertFinalNewline":          {"before": "()",                                          "after": "()\n"},
    }
},
"Lua": {
    "snippet": "local io = require('io')\nlocal math = require('math')\n\nfunction go(x,y)\n    if x>0 then\n        return x+y\n    end\nend",
    "samples": {
        "spaceAfterKeywords":          {"before": "if x>0 then",                                 "after": "if x > 0 then"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2",                                     "after": "a = b + c * 2"},
        "spaceAfterComma":             {"before": "go(x,y,z)",                                   "after": "go(x, y, z)"},
        "trimTrailingWhitespace":      {"before": "local x = 1   ",                             "after": "local x = 1"},
        "insertFinalNewline":          {"before": "end",                                         "after": "end\n"},
    }
},
"SQL": {
    "snippet": "select id,name,email\nfrom users\nwhere id>0\norder by name",
    "samples": {
        "sqlKeywordsUppercase":        {"before": "select id from users where id>0",             "after": "SELECT id FROM users WHERE id > 0"},
        "spaceAfterKeywords":          {"before": "WHERE(id>0)",                                 "after": "WHERE (id > 0)"},
        "spaceAroundBinaryOperators":  {"before": "id>0",                                        "after": "id > 0"},
        "spaceAfterComma":             {"before": "id,name,email",                               "after": "id, name, email"},
        "trimTrailingWhitespace":      {"before": "SELECT id   ",                               "after": "SELECT id"},
        "insertFinalNewline":          {"before": "ORDER BY name",                               "after": "ORDER BY name\n"},
    }
},
"C": {
    "snippet": "#include <stdio.h>\n#include <stdlib.h>\n\nvoid go(int x,int y){\nif(x>0){\nswitch(x){\ncase 1:\nreturn;\n}\n}\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if(x>0){",                                    "after": "if (x > 0) {"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2;",                                    "after": "a = b + c * 2;"},
        "spaceAfterComma":             {"before": "go(x,y,z);",                                  "after": "go(x, y, z);"},
        "indentCaseLabels":            {"before": "switch(x){\ncase 1:\nbreak;",                 "after": "switch (x) {\n  case 1:\n    break;"},
        "trimTrailingWhitespace":      {"before": "int x = 1;   ",                              "after": "int x = 1;"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"Cpp": {
    "snippet": "#include <iostream>\n#include <vector>\n\nvoid go(int x,int y){\nif(x>0){\nswitch(x){\ncase 1:\nreturn;\n}\n}\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if(x>0){",                                    "after": "if (x > 0) {"},
        "spaceAroundBinaryOperators":  {"before": "a=b+c*2;",                                    "after": "a = b + c * 2;"},
        "spaceAfterComma":             {"before": "go(x,y,z);",                                  "after": "go(x, y, z);"},
        "indentCaseLabels":            {"before": "switch(x){\ncase 1:\nbreak;",                 "after": "switch (x) {\n  case 1:\n    break;"},
        "trimTrailingWhitespace":      {"before": "int x = 1;   ",                              "after": "int x = 1;"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
"HTML": {
    "snippet": "<!DOCTYPE html>\n<html>\n<head><title>Test</title></head>\n<body>\n<div class=\"box\"><p>Hello</p></div>\n</body>\n</html>",
    "samples": {
        "trimTrailingWhitespace":      {"before": "<p>Hello</p>   ",                             "after": "<p>Hello</p>"},
        "insertFinalNewline":          {"before": "</html>",                                     "after": "</html>\n"},
    }
},
"XAML": {
    "snippet": "<Window xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">\n    <Grid>\n        <Button Content=\"Click\" Width=\"100\"/>\n    </Grid>\n</Window>",
    "samples": {
        "trimTrailingWhitespace":      {"before": "    <Button Content=\"Click\"/>   ",           "after": "    <Button Content=\"Click\"/>"},
        "insertFinalNewline":          {"before": "</Window>",                                   "after": "</Window>\n"},
    }
},
"XMLMarkup": {
    "snippet": "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root>\n    <item id=\"1\" name=\"test\"/>\n    <item id=\"2\" name=\"other\"/>\n</root>",
    "samples": {
        "trimTrailingWhitespace":      {"before": "    <item id=\"1\"/>   ",                     "after": "    <item id=\"1\"/>"},
        "insertFinalNewline":          {"before": "</root>",                                     "after": "</root>\n"},
    }
},
"Markdown": {
    "snippet": "# Title\n\nSome paragraph text.\n\n## Section\n\n- item one\n- item two",
    "samples": {
        "trimTrailingWhitespace":      {"before": "Some text   ",                               "after": "Some text"},
        "insertFinalNewline":          {"before": "- item two",                                  "after": "- item two\n"},
    }
},
"Batch": {
    "snippet": "@echo off\nSETLOCAL\n\n:go\nIF \"%1\" == \"\" GOTO end\nECHO %1\n:end",
    "samples": {
        "trimTrailingWhitespace":      {"before": "ECHO %1   ",                                 "after": "ECHO %1"},
        "insertFinalNewline":          {"before": ":end",                                        "after": ":end\n"},
    }
},
"Assembly": {
    "snippet": "section .text\nglobal _start\n\n_start:\n    mov eax,1\n    mov ebx,0\n    int 0x80",
    "samples": {
        "spaceAfterComma":             {"before": "mov eax,1",                                   "after": "mov eax, 1"},
        "trimTrailingWhitespace":      {"before": "    mov eax, 1   ",                          "after": "    mov eax, 1"},
        "insertFinalNewline":          {"before": "    int 0x80",                               "after": "    int 0x80\n"},
    }
},
"CSharpScript": {
    "snippet": "using System;\n\nvar x = 1;\nif(x>0){\nConsole.WriteLine(x+1);\n}",
    "samples": {
        "spaceAfterKeywords":          {"before": "if(x>0){",                                    "after": "if (x > 0) {"},
        "spaceAroundBinaryOperators":  {"before": "x+1",                                         "after": "x + 1"},
        "spaceAfterComma":             {"before": "Console.Write(x,y);",                         "after": "Console.Write(x, y);"},
        "trimTrailingWhitespace":      {"before": "var x = 1;   ",                              "after": "var x = 1;"},
        "insertFinalNewline":          {"before": "}",                                           "after": "}\n"},
    }
},
}

# ── Key ordering for formattingRules ──────────────────────────────────────────
RULE_KEY_ORDER = [
    "indentSize","useTabs","trimTrailingWhitespace","insertFinalNewline","lineEnding",
    "braceStyle","spaceBeforeOpenBrace","spaceAfterKeywords","spaceInsideParens",
    "indentCaseLabels","maxConsecutiveBlankLines","blankLineBeforeMethod","blankLineAfterImports",
    "spaceAroundBinaryOperators","spaceAfterComma",
    "organizeImports","separateSystemImports",
    "quoteStyle","trailingCommas","maxLineLength","sqlKeywordsUppercase",
]

def pretty_formatting_rules(rules_dict: dict, base_indent: int) -> str:
    """Render formattingRules as a pretty-printed multi-line block."""
    ind  = "  " * base_indent
    ind2 = "  " * (base_indent + 1)
    lines = ["{"]
    ordered_keys = [k for k in RULE_KEY_ORDER if k in rules_dict]
    remaining    = [k for k in rules_dict if k not in RULE_KEY_ORDER]
    all_keys     = ordered_keys + remaining
    for i, key in enumerate(all_keys):
        val   = rules_dict[key]
        comma = "," if i < len(all_keys) - 1 else ""
        # align values at column 32 relative to ind2
        key_str = f'"{key}":'
        padding = max(1, 34 - len(key_str))
        lines.append(f'{ind2}{key_str}{" " * padding}{json.dumps(val)}{comma}')
    lines.append(f"{ind}}}")
    return "\n".join(lines)

def process_file(path: pathlib.Path, lang_key: str):
    raw  = path.read_bytes()
    # strip UTF-8 BOM if present
    if raw.startswith(b'\xef\xbb\xbf'):
        raw = raw[3:]
    text = raw.decode("utf-8")

    # strip leading block comment /* ... */ (not valid JSON)
    header = ""
    json_text = text
    stripped = text.lstrip()
    if stripped.startswith("/*"):
        end = stripped.find("*/")
        if end != -1:
            comment_raw = stripped[:end + 2]          # everything inside /* ... */
            json_text   = stripped[end + 2:].lstrip("\r\n")

            # ── Normalise the header ─────────────────────────────────────
            # 1. Strip the /* and */ delimiters
            inner = comment_raw[2:-2]
            # 2. Split into lines, strip, drop empty lines
            lines = [l.strip() for l in inner.splitlines()]
            lines = [l for l in lines if l]

            is_sep = lambda l: l.replace("=", "").replace(" ", "") == ""

            # 3. Separate content lines from separator lines
            content_lines = [l for l in lines if not is_sep(l)]
            sep = "   " + "=" * 60

            # 4. Remove any pre-existing Updated: lines
            content_lines = [l for l in content_lines if not l.startswith("Updated:")]

            # 5. Add fresh Updated: line at the end of content
            content_lines.append(f"Updated:   {TODAY}")

            # 6. Re-build: opening sep, content (indented), closing sep
            body_lines = [sep] + ["   " + l for l in content_lines] + [sep]
            header = "/*\n" + "\n".join(body_lines) + "\n*/"

    data = json.loads(json_text)

    meta    = LANGS.get(lang_key, {})
    snippet = meta.get("snippet")
    samples = meta.get("samples", {})

    # ── Bump JSON version + updated date ────────────────────────────────────
    if "version" in data:
        data["version"] = VERSION_NEW
    # Some files have a top-level "updated" field
    if "updated" in data:
        data["updated"] = TODAY

    # Walk the syntaxDefinition block (top-level or inside it)
    syn = data.get("syntaxDefinition", data)

    # Inject / overwrite previewSnippet + previewSamples in the model
    if snippet:
        syn["previewSnippet"] = snippet
    if samples:
        syn["previewSamples"] = samples

    if "syntaxDefinition" in data:
        data["syntaxDefinition"] = syn

    # ── Re-serialize the whole file with pretty formattingRules ──────────────
    # We use a custom encoder pass: serialize normally then post-process the
    # formattingRules line to expand it.
    serialized = json.dumps(data, indent=2, ensure_ascii=False)

    # Find all inline formattingRules and expand them
    def expand_match(m):
        indent_spaces = len(m.group(1))
        base_indent   = indent_spaces // 2
        try:
            obj = json.loads(m.group(2))
        except json.JSONDecodeError:
            return m.group(0)  # leave as-is if parse fails
        pretty = pretty_formatting_rules(obj, base_indent)
        return f'{m.group(1)}"formattingRules": {pretty}'

    serialized = re.sub(
        r'^( *)"formattingRules": (\{[^\n]+\})',
        expand_match,
        serialized,
        flags=re.MULTILINE
    )

    # re-attach the normalised header comment
    final = (header + "\n" + serialized) if header else serialized
    path.write_text(final, encoding="utf-8")
    print(f"  OK {path.name}")
# ── Main ──────────────────────────────────────────────────────────────────────
if __name__ == "__main__":
    print(f"Processing whfmt files in: {ROOT}")
    ok = err = 0
    for path in sorted(ROOT.glob("*.whfmt")):
        lang_key = path.stem          # e.g. "CSharp", "JavaScript"
        try:
            process_file(path, lang_key)
            ok += 1
        except Exception as e:
            print(f"  ERR {path.name}: {e}", file=sys.stderr)
            err += 1
print(f"\nDone: {ok} OK, {err} errors.")
