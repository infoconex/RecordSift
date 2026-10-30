#!/usr/bin/env python3
"""Supplement Roslyn IDE1006 for C# const spellings containing underscores.

Microsoft guidance requires PascalCase for ALL C# const declarations. Some
uppercase snake-case names pass IDE1006 even with the intended EditorConfig
configuration. This is a deliberately narrow, zero-dependency CI backstop;
Roslyn remains the authoritative general naming analyzer.
"""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parent.parent
# Conventional single-line C# const declarations; field and local constants.
DECLARATION = re.compile(
    r"\bconst\s+(?:[A-Za-z_][\w.<>,?\[\]]*\s+)+(?P<name>[A-Za-z_]\w*)\s*="
)
PASCAL = re.compile(r"^[A-Z][A-Za-z0-9]*$")


def invalid_names(source):
    names = []
    in_block = False
    for number, line in enumerate(source.splitlines(), 1):
        cleaned = ""
        i = 0
        while i < len(line):
            if in_block:
                end = line.find("*/", i)
                if end == -1:
                    break
                in_block = False
                i = end + 2
                continue
            if line.startswith("/*", i):
                in_block = True
                i += 2
            elif line.startswith("//", i):
                break
            else:
                cleaned += line[i]
                i += 1
        for match in DECLARATION.finditer(cleaned):
            name = match.group("name")
            if not PASCAL.fullmatch(name):
                names.append((number, name))
    return names


def self_test():
    invalid = """public const string BAD_CONSTANT_NAME = "x";
        private const int bad_local_value = 1;
        const string _prefixed = "x";"""
    valid = """public const string DefaultDelimiter = ",";
        const int MinimumLength = 1;"""
    assert [name for _, name in invalid_names(invalid)] == [
        "BAD_CONSTANT_NAME", "bad_local_value", "_prefixed"
    ], "Const-name guard must reject uppercase snake-case, camelCase and underscores"
    assert not invalid_names(valid), "Const-name guard must allow PascalCase declarations"


def main():
    self_test()
    violations = []
    files = sorted(path for folder in ("src", "tests")
                   for path in (ROOT / folder).rglob("*.cs"))
    for path in files:
        for line, name in invalid_names(path.read_text(encoding="utf-8")):
            violations.append(f"{path.relative_to(ROOT)}:{line}: const '{name}' "
                              "must be PascalCase without underscores")
    if violations:
        print("Constant identifier naming violations:")
        for violation in violations:
            print(" - " + violation)
        return 1
    print(f"Constant naming guard passed ({len(files)} C# files); "
          "negative and positive self-tests passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
