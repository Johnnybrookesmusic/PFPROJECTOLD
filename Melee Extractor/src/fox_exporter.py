from pathlib import Path
import struct

MELEE_ROOT = Path(r"C:\Users\Johnn\Downloads\Melee Extractor\src\Input\Melee Deconstructed")

# Automatically find PlFx.dat anywhere in the extracted folder
matches = list(MELEE_ROOT.rglob("PlFx.dat"))

if not matches:
    print("ERROR: Couldn't find PlFx.dat")
    exit()

FILE = matches[0]

print(f"Using: {FILE}")

with open(FILE, "rb") as f:
    data = f.read()

print("=" * 60)
print("FOX DAT INFORMATION")
print("=" * 60)

print(f"Size: {len(data):,} bytes")

# HSD DAT header
file_size = struct.unpack(">I", data[0:4])[0]
reloc_offset = struct.unpack(">I", data[4:8])[0]
reloc_count = struct.unpack(">I", data[8:12])[0]
root_count = struct.unpack(">I", data[12:16])[0]
ref_count = struct.unpack(">I", data[16:20])[0]

print()
print("Header")
print("------")
print(f"Reported File Size : {file_size}")
print(f"Relocation Offset  : 0x{reloc_offset:X}")
print(f"Relocation Entries : {reloc_count}")
print(f"Root Nodes         : {root_count}")
print(f"Reference Nodes    : {ref_count}")

print()
print("First 64 bytes")
print("--------------")
print(data[:64].hex(" "))