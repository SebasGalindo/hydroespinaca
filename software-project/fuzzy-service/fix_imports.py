#!/usr/bin/env python3
"""
Script para convertir importaciones relativas a absolutas en el FuzzyEngine.
"""

import os
import re
from pathlib import Path

def fix_relative_imports(file_path):
    """Convierte importaciones relativas a absolutas en un archivo."""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Patrón para encontrar importaciones relativas
    pattern = r'from \.([A-Za-z][A-Za-z0-9_]*) import'
    
    # Reemplazar con importaciones absolutas
    new_content = re.sub(pattern, r'from \1 import', content)
    
    # Solo escribir si hay cambios
    if new_content != content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        print(f"Fixed imports in: {file_path}")
        return True
    return False

def main():
    """Función principal."""
    fuzzy_engine_dir = Path("FuzzyService/Infrastructure/FuzzyEngine")
    
    if not fuzzy_engine_dir.exists():
        print(f"Directory not found: {fuzzy_engine_dir}")
        return
    
    fixed_count = 0
    
    # Procesar todos los archivos .py en el directorio
    for py_file in fuzzy_engine_dir.glob("*.py"):
        if py_file.name != "__init__.py":  # Skip __init__.py for now
            if fix_relative_imports(py_file):
                fixed_count += 1
    
    print(f"\nFixed imports in {fixed_count} files.")

if __name__ == "__main__":
    main()