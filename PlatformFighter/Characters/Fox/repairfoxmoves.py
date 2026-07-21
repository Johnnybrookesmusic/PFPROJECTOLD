#!/usr/bin/env python3

from pathlib import Path
import shutil
import re
import sys


def find_match(text, start, open_char="(", close_char=")"):
    depth = 0
    for i in range(start, len(text)):
        c = text[i]
        if c == open_char:
            depth += 1
        elif c == close_char:
            depth -= 1
            if depth == 0:
                return i
    return -1


def repair(text):

    fixed = 0
    pos = 0
    out = []

    while True:

        m = re.search(r'new\s*\(', text[pos:])

        if not m:
            out.append(text[pos:])
            break

        start = pos + m.start()

        open_paren = text.find("(", start)
        close_paren = find_match(text, open_paren)

        if close_paren == -1:
            out.append(text[pos:])
            break

        constructor = text[start:close_paren+1]

        out.append(text[pos:start])

        # already has hitboxes
        if "hitboxes:" in constructor:
            out.append(constructor)
            pos = close_paren + 1
            continue

        i = close_paren + 1

        while i < len(text) and text[i].isspace():
            i += 1

        if i < len(text) and text[i] == ",":
            i += 1

        while i < len(text) and text[i].isspace():
            i += 1

        if not text.startswith("new HitboxSpec[]", i):
            out.append(constructor)
            pos = close_paren + 1
            continue

        brace = text.find("{", i)
        end = find_match(text, brace, "{", "}")

        if end == -1:
            out.append(constructor)
            pos = close_paren + 1
            continue

        hitboxes = text[i:end+1]

        repaired = constructor[:-1].rstrip()

        if not repaired.endswith(","):
            repaired += ","

        repaired += "\n\t\thitboxes: "
        repaired += hitboxes
        repaired += ")"

        out.append(repaired)

        semi = text.find(";", end)

        if semi == -1:
            pos = end + 1
        else:
            out.append(";")
            pos = semi + 1

        fixed += 1

    return "".join(out), fixed


def main():

    if len(sys.argv) != 2:
        print("Usage:")
        print("py repairfoxmoves.py FoxMoves.cs")
        return

    path = Path(sys.argv[1])

    backup = path.with_suffix(path.suffix + ".bak")
    shutil.copy2(path, backup)

    text = path.read_text(encoding="utf8")

    new_text, fixed = repair(text)

    path.write_text(new_text, encoding="utf8")

    print(f"TOTAL FIXED: {fixed}")
    print("Backup:", backup)


if __name__ == "__main__":
    main()