#!/usr/bin/env python3
"""Fail when the NuGet audit contains high or critical vulnerabilities."""

from __future__ import annotations

import json
import sys
from pathlib import Path
from typing import Any

BLOCKING = {"high", "critical"}


def collect(node: Any, package: str | None = None, version: str | None = None) -> list[tuple[str, str, str, str]]:
    """Collect vulnerability entries while retaining the nearest package identity."""
    findings: list[tuple[str, str, str, str]] = []
    if isinstance(node, dict):
        current_package = str(node.get("id", package or "unknown"))
        current_version = str(node.get("resolvedVersion", node.get("version", version or "unknown")))
        vulnerabilities = node.get("vulnerabilities")
        if isinstance(vulnerabilities, list):
            for vulnerability in vulnerabilities:
                if not isinstance(vulnerability, dict):
                    continue
                severity = str(vulnerability.get("severity", "unknown"))
                advisory = str(vulnerability.get("advisoryurl", vulnerability.get("advisoryUrl", "unknown")))
                findings.append((current_package, current_version, severity, advisory))
        for value in node.values():
            findings.extend(collect(value, current_package, current_version))
    elif isinstance(node, list):
        for value in node:
            findings.extend(collect(value, package, version))
    return findings


def main() -> int:
    """Parse the .NET JSON report and apply the documented severity gate."""
    if len(sys.argv) != 2:
        print("usage: check_nuget_vulnerabilities.py <report.json>", file=sys.stderr)
        return 2
    payload = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
    findings = collect(payload)
    if not findings:
        print("No known NuGet vulnerabilities were reported.")
        return 0

    blocking = []
    for package, version, severity, advisory in findings:
        print(f"{severity}: {package} {version} - {advisory}")
        if severity.lower() in BLOCKING:
            blocking.append((package, version, severity, advisory))

    if blocking:
        print(f"Blocking NuGet vulnerabilities: {len(blocking)} high/critical finding(s).", file=sys.stderr)
        return 1
    print("Only non-blocking low/moderate vulnerabilities were reported.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
