"""Verify that Getting Started examples and downloadable CSV/XLSX datasets agree.

Runs with Python 3 stdlib; no spreadsheet packages required.
"""
from __future__ import annotations

import csv
from pathlib import Path
import re
import sys
from xml.etree import ElementTree as ET
from zipfile import ZipFile

ROOT = Path(__file__).resolve().parents[1]
SAMPLES = ROOT / "docs" / "samples"
GUIDE = ROOT / "docs" / "getting-started.md"
COLUMNS = (
    "Id", "Name", "Email", "RegistrationDate", "LoyaltyPoints",
    "CreditLimit", "IsActive", "CustomerTier", "LastPurchaseDate",
    "ReferralCode",
)
NS = "{http://schemas.openxmlformats.org/spreadsheetml/2006/main}"


def csv_rows(path: Path) -> list[list[str]]:
    with path.open(newline="", encoding="utf-8") as stream:
        return list(csv.reader(stream))


def workbook_rows(path: Path) -> list[list[str]]:
    with ZipFile(path) as archive:
        if archive.testzip() is not None:
            raise AssertionError(f"Corrupted XLSX ZIP: {path}")
        root = ET.fromstring(archive.read("xl/worksheets/sheet1.xml"))
    result = []
    for row in root.findall(f".//{NS}sheetData/{NS}row"):
        values = [""] * len(COLUMNS)
        for cell in row.findall(f"{NS}c"):
            reference = cell.attrib["r"]
            letters = re.match(r"[A-Z]+", reference).group()
            column = 0
            for char in letters:
                column = column * 26 + ord(char) - ord("A") + 1
            index = column - 1
            assert 0 <= index < len(COLUMNS), reference
            inline = cell.find(f"{NS}is/{NS}t")
            value = cell.find(f"{NS}v")
            values[index] = (
                inline.text or "" if inline is not None
                else value.text or "" if value is not None
                else ""
            )
        result.append(values)
    return result


def validate_pair(name: str, count: int) -> list[list[str]]:
    csv_path = SAMPLES / (name + ".csv")
    xlsx_path = SAMPLES / (name + ".xlsx")
    csv_data = csv_rows(csv_path)
    excel_data = workbook_rows(xlsx_path)
    assert csv_data[0] == list(COLUMNS), f"Unexpected CSV headers: {csv_path}"
    assert excel_data[0] == list(COLUMNS), f"Unexpected XLSX headers: {xlsx_path}"
    assert len(csv_data) == count + 1, f"Unexpected CSV row count: {csv_path}"
    assert csv_data == excel_data, f"CSV/XLSX cell mismatch: {name}"
    assert all(len(row) == len(COLUMNS) for row in csv_data), name
    return csv_data


def main() -> None:
    valid = validate_pair("customers", 20)
    validate_pair("customers-invalid", 10)

    text = GUIDE.read_text(encoding="utf-8")
    marker = "```csv" + chr(10)
    if marker not in text:
        raise AssertionError("Cannot find the complete customers.csv block")
    block = text.split(marker, 1)[1].split("```", 1)[0]
    displayed = list(csv.reader(block.splitlines()))
    assert displayed == valid, "Inline Getting Started CSV differs from downloadable CSV"
    for name in ("customers.csv", "customers.xlsx",
                 "customers-invalid.csv", "customers-invalid.xlsx"):
        assert f"samples/{name}" in text, f"Missing download link: {name}"
    print("Getting Started: 20 valid, 10 invalid; CSV/XLSX and inline documentation match.")


if __name__ == "__main__":
    try:
        main()
    except (AssertionError, OSError, ValueError) as error:
        print(f"Sample verification failed: {error}", file=sys.stderr)
        sys.exit(1)
