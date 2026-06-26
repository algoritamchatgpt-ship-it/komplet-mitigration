#!/usr/bin/env python3
"""Decompile a Visual FoxPro .scx form (+ its .SCT memo file) into a readable
text dump: control hierarchy, PROPERTIES, and the literal FoxPro source code
of every event handler (METHODS memo).

.scx is itself a DBF table where every field is a memo (M) pointer into the
paired .SCT file (same block-memo format as a normal .FPT):
  header bytes 6-7 (big-endian) = block size
  each block = 4-byte big-endian signature + 4-byte big-endian length + content

Usage: python scx_dump.py <file.scx> [output.txt]
       python scx_dump.py --all <folder> <output_folder>
"""
import struct
import sys
import os


def _read_dbf_fields(data):
    off = 32
    fields = []
    while data[off] != 0x0D:
        name = data[off:off + 11].split(b"\x00")[0].decode("latin1")
        ftype = chr(data[off + 11])
        flen = data[off + 16]
        fields.append((name, ftype, flen))
        off += 32
    return fields


def read_scx(scx_path, sct_path):
    with open(scx_path, "rb") as f:
        data = f.read()
    numrec = struct.unpack("<I", data[4:8])[0]
    headerlen = struct.unpack("<H", data[8:10])[0]
    recordlen = struct.unpack("<H", data[10:12])[0]
    fields = _read_dbf_fields(data)

    with open(sct_path, "rb") as f:
        memo = f.read()
    blocksize = struct.unpack(">H", memo[6:8])[0] or 64

    def get_memo(blockno):
        if blockno == 0:
            return ""
        start = blockno * blocksize
        if start + 8 > len(memo):
            return ""
        length = struct.unpack(">I", memo[start + 4:start + 8])[0]
        return memo[start + 8:start + 8 + length].decode("cp1250", errors="replace")

    records = []
    for i in range(numrec):
        rec = data[headerlen + i * recordlen: headerlen + (i + 1) * recordlen]
        deleted = rec[0:1] == b"*"
        vals = {}
        pos = 1
        for name, ftype, flen in fields:
            raw = rec[pos:pos + flen]
            pos += flen
            if ftype == "M":
                blockno = struct.unpack("<I", raw)[0]
                vals[name] = get_memo(blockno)
            else:
                vals[name] = raw.decode("cp1250", errors="replace").strip()
        if not deleted:
            records.append(vals)
    return records


def dump_text(records):
    out = []
    out.append(f"num records {len(records)}")
    for v in records:
        cls = v.get("CLASS", "").strip()
        base = v.get("BASECLASS", "").strip()
        name = v.get("OBJNAME", "").strip()
        parent = v.get("PARENT", "").strip()
        if not (cls or base or name):
            continue
        out.append(f"\n=== {base or cls} | {name} | parent={parent or '(root)'}")
        props = v.get("PROPERTIES", "").strip()
        if props:
            out.append("  PROPERTIES:")
            for line in props.splitlines():
                out.append(f"    {line}")
        methods = v.get("METHODS", "").strip()
        if methods:
            out.append("  METHODS (literal FoxPro source):")
            for line in methods.splitlines():
                out.append(f"    {line}")
    return "\n".join(out)


def find_sct(scx_path):
    base, _ = os.path.splitext(scx_path)
    for ext in (".SCT", ".sct", ".Sct"):
        cand = base + ext
        if os.path.exists(cand):
            return cand
    # Windows filesystems are usually case-insensitive; try direct swap
    cand = base + ".SCT"
    if os.path.exists(cand):
        return cand
    raise FileNotFoundError(f"no .SCT memo file found for {scx_path}")


def dump_one(scx_path, out_path=None):
    sct_path = find_sct(scx_path)
    records = read_scx(scx_path, sct_path)
    text = dump_text(records)
    if out_path:
        with open(out_path, "w", encoding="utf-8") as f:
            f.write(text)
        print(f"wrote {out_path} ({len(records)} records)")
    else:
        print(text)


def dump_all(folder, out_folder):
    os.makedirs(out_folder, exist_ok=True)
    seen = set()
    for fname in os.listdir(folder):
        if not fname.lower().endswith(".scx"):
            continue
        key = fname.lower()
        if key in seen:
            continue
        seen.add(key)
        scx_path = os.path.join(folder, fname)
        out_name = os.path.splitext(fname)[0] + ".txt"
        out_path = os.path.join(out_folder, out_name)
        try:
            dump_one(scx_path, out_path)
        except Exception as e:
            print(f"FAILED {fname}: {e}")


if __name__ == "__main__":
    if len(sys.argv) >= 2 and sys.argv[1] == "--all":
        dump_all(sys.argv[2], sys.argv[3])
    elif len(sys.argv) >= 2:
        dump_one(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
    else:
        print(__doc__)
        sys.exit(1)
