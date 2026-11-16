#!/usr/bin/env python3
"""Validate the exact Agents package inventory."""
import sys
from nuget_package_validation import *

if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print(f"validate-nuget-packages: {error}", file=sys.stderr)
        raise SystemExit(1)
