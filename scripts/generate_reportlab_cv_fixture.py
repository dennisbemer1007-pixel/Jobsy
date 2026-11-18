#!/usr/bin/env python3
"""Regenerate Jobsy.Tests/Fixtures/Cv/ReportLabDeLindeCv.pdf (ReportLab metadata + Dutch CV sections)."""
from pathlib import Path

from reportlab.lib.pagesizes import A4
from reportlab.pdfgen import canvas

OUT = Path(__file__).resolve().parents[1] / "Jobsy.Tests/Fixtures/Cv/ReportLabDeLindeCv.pdf"


def main() -> None:
    OUT.parent.mkdir(parents=True, exist_ok=True)
    c = canvas.Canvas(str(OUT), pagesize=A4)
    c._doc.info.producer = "ReportLab PDF Library - (opensource)"
    c._doc.info.creator = "ReportLab PDF Library - (opensource)"

    y = 750

    def line(text: str, dy: int = 20) -> None:
        nonlocal y
        c.drawString(72, y, text)
        y -= dy

    line("CV Test Kandidaat", 30)
    line("Werkervaring")
    line("De Linde - medewerker horeca")
    line("Dierenasiel - vrijwilliger")
    line("Opleiding")
    line("MBO niveau 3 aan ROC Mondriaan (2018)")
    line("Rijbewijs")
    line("Rijbewijs B")
    line("Certificaten")
    line("BHV certificaat")
    line("VCA-basis")
    c.save()
    print(f"Wrote {OUT} ({OUT.stat().st_size} bytes)")


if __name__ == "__main__":
    main()