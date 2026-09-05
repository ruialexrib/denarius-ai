#!/usr/bin/env python3
"""Validate merged Cobertura coverage against repository thresholds."""

from __future__ import annotations

import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def main() -> int:
    """Read coverage metrics, publish a concise summary, and enforce thresholds."""
    if len(sys.argv) != 3:
        print("usage: check_coverage.py <cobertura.xml> <thresholds.json>", file=sys.stderr)
        return 2

    report_path = Path(sys.argv[1])
    threshold_path = Path(sys.argv[2])
    root = ET.parse(report_path).getroot()
    thresholds = json.loads(threshold_path.read_text(encoding="utf-8"))

    line_rate = float(root.attrib.get("line-rate", "0")) * 100.0
    branch_rate = float(root.attrib.get("branch-rate", "0")) * 100.0
    line_minimum = float(thresholds["line"])
    branch_minimum = float(thresholds["branch"])

    summary = (
        f"Line coverage {line_rate:.2f}% (minimum {line_minimum:.2f}%); "
        f"branch coverage {branch_rate:.2f}% (minimum {branch_minimum:.2f}%)."
    )
    print(summary)
    print(f"::notice title=Coverage baseline::{summary}")

    github_summary = Path(__import__("os").environ.get("GITHUB_STEP_SUMMARY", ""))
    if str(github_summary):
        with github_summary.open("a", encoding="utf-8") as handle:
            handle.write("## Coverage\n\n")
            handle.write(f"- Lines: **{line_rate:.2f}%** (minimum {line_minimum:.2f}%)\n")
            handle.write(f"- Branches: **{branch_rate:.2f}%** (minimum {branch_minimum:.2f}%)\n")

    failures: list[str] = []
    if line_rate < line_minimum:
        failures.append(f"line coverage {line_rate:.2f}% is below {line_minimum:.2f}%")
    if branch_rate < branch_minimum:
        failures.append(f"branch coverage {branch_rate:.2f}% is below {branch_minimum:.2f}%")

    if failures:
        print("Coverage gate failed: " + "; ".join(failures), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
