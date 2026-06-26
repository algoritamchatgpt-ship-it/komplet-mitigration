from __future__ import annotations
import os
import struct
from xml.sax.saxutils import escape


def map_codepage(code: int) -> str:
    return {
        0x01: "cp437",
        0x02: "cp850",
        0x03: "cp1252",
        0x64: "cp852",
        0x65: "cp866",
        0xC8: "cp1250",
        0xC9: "cp1251",
        0xCB: "cp1253",
    }.get(code, "cp1250")


def read_dbf(path: str):
    with open(path, "rb") as f:
        header = f.read(32)
        if len(header) < 32:
            raise ValueError(f"Invalid DBF header: {path}")

        num_records = struct.unpack("<I", header[4:8])[0]
        header_len = struct.unpack("<H", header[8:10])[0]
        record_len = struct.unpack("<H", header[10:12])[0]
        codepage = header[29]

        fields = []
        while True:
            first = f.read(1)
            if not first:
                raise ValueError(
                    f"Unexpected EOF while reading field descriptors: {path}"
                )
            if first == b"\x0D":
                break
            rest = f.read(31)
            name_bytes = first + rest[0:10]
            name = name_bytes.split(b"\x00", 1)[0].decode("ascii", errors="ignore")
            ftype = chr(rest[10])
            length = rest[16]
            decimals = rest[17]
            fields.append(
                {
                    "name": name,
                    "type": ftype,
                    "length": length,
                    "decimals": decimals,
                }
            )

        f.seek(header_len)
        encoding = map_codepage(codepage)

        rows = []
        for _ in range(num_records):
            rec = f.read(record_len)
            if not rec or len(rec) < record_len:
                break

            # rec[0] is deletion flag, we do NOT skip deleted rows
            offset = 1
            row = {}
            for field in fields:
                raw = rec[offset : offset + field["length"]]
                offset += field["length"]
                row[field["name"]] = parse_value(field, raw, encoding)
            rows.append(row)

        return fields, rows


def parse_value(field, raw, encoding: str):
    text = raw.decode(encoding, errors="ignore").strip()
    ftype = field["type"]

    if ftype == "L":
        t = text.upper()
        if t in ("T", "Y", ".T.", "1"):
            return True
        if t in ("F", "N", ".F.", "0"):
            return False
        return None

    if ftype in ("N", "F"):
        if text == "":
            return None
        if "," in text and "." not in text:
            return text.replace(",", ".")
        return text

    if ftype == "D":
        if len(text) == 8 and text.isdigit():
            return f"{text[0:4]}-{text[4:6]}-{text[6:8]}"
        return None

    # C, M, or other types
    return text


def sql_type(field) -> str:
    ftype = field["type"]
    length = field["length"]
    decimals = field["decimals"]

    if ftype == "C":
        return f"VARCHAR({length})"
    if ftype in ("N", "F"):
        if decimals > 0:
            return f"NUMERIC({length},{decimals})"
        return f"NUMERIC({length})"
    if ftype == "L":
        return "BOOLEAN"
    if ftype == "D":
        return "DATE"
    if ftype == "M":
        return "TEXT"
    return "TEXT"


def sql_literal(field, value) -> str:
    if value is None:
        return "NULL"

    ftype = field["type"]
    if ftype in ("N", "F"):
        return str(value)
    if ftype == "L":
        return "1" if value else "0"
    if ftype == "D":
        return f"'{value}'"

    return "'" + str(value).replace("'", "''") + "'"


def write_sql(path: str, table_name: str, fields, rows):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(f"-- Source: {table_name}\n")
        f.write(f'CREATE TABLE IF NOT EXISTS "{table_name}" (\n')
        for i, field in enumerate(fields):
            comma = "," if i < len(fields) - 1 else ""
            f.write(f'  "{field["name"]}" {sql_type(field)}{comma}\n')
        f.write(");\n\n")

        if rows:
            f.write("BEGIN TRANSACTION;\n")
            col_names = ", ".join([f'"{fld["name"]}"' for fld in fields])
            for row in rows:
                values = ", ".join(
                    [sql_literal(fld, row.get(fld["name"])) for fld in fields]
                )
                f.write(
                    f'INSERT INTO "{table_name}" ({col_names}) VALUES ({values});\n'
                )
            f.write("COMMIT;\n")


def write_xml(path: str, table_name: str, fields, rows):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write('<?xml version="1.0" encoding="UTF-8"?>\n')
        f.write(f'<dbf name="{escape(table_name)}">\n')
        f.write("  <columns>\n")
        for field in fields:
            f.write(
                "    <column "
                f'name="{escape(field["name"])}" '
                f'type="{escape(field["type"])}" '
                f'length="{field["length"]}" '
                f'decimals="{field["decimals"]}" />\n'
            )
        f.write("  </columns>\n")
        f.write("  <rows>\n")
        for row in rows:
            f.write("    <row>\n")
            for field in fields:
                name = field["name"]
                val = row.get(name)
                if val is None:
                    f.write(f"      <{name} />\n")
                else:
                    f.write(f"      <{name}>{escape(str(val))}</{name}>\n")
            f.write("    </row>\n")
        f.write("  </rows>\n")
        f.write("</dbf>\n")


def main():
    base_dir = os.path.dirname(os.path.abspath(__file__))
    src_dir = base_dir
    out_dir = os.path.join(base_dir, "_out")
    sql_dir = os.path.join(out_dir, "sql")
    xml_dir = os.path.join(out_dir, "xml")
    os.makedirs(sql_dir, exist_ok=True)
    os.makedirs(xml_dir, exist_ok=True)

    dbfs = [f for f in os.listdir(src_dir) if f.lower().endswith(".dbf")]
    dbfs.sort(key=lambda s: s.lower())

    if not dbfs:
        print("No .dbf files found.")
        return

    for fname in dbfs:
        fpath = os.path.join(src_dir, fname)
        table_name = os.path.splitext(fname)[0]
        fields, rows = read_dbf(fpath)

        sql_path = os.path.join(sql_dir, f"{table_name}.sql")
        xml_path = os.path.join(xml_dir, f"{table_name}.xml")

        write_sql(sql_path, table_name, fields, rows)
        write_xml(xml_path, table_name, fields, rows)

        rel_sql = os.path.relpath(sql_path, base_dir)
        rel_xml = os.path.relpath(xml_path, base_dir)
        print(f"OK: {fname} -> {rel_sql}, {rel_xml}")


if __name__ == "__main__":
    main()
