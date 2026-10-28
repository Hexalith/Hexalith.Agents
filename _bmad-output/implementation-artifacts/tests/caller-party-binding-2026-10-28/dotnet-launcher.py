#!/usr/bin/env python3
"""Validation-only launcher: generated source mapping applies solely to build children."""
import json
import os
from pathlib import Path
import sys
mapping = json.loads(Path('/tmp/agents-caller-identity-mapping/environment.json').read_text())
environment = dict(os.environ)
if len(sys.argv) > 1 and sys.argv[1] == 'build':
    environment.update(mapping)
else:
    for name in mapping:
        environment.pop(name, None)
real_dotnet = '/home/administrator/.dotnet/dotnet'
os.execve(real_dotnet, [real_dotnet, *sys.argv[1:]], environment)
