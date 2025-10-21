import { readFileSync, writeFileSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

// Obtener __dirname en ESM
const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

// Ruta al archivo de declaraciones generado
const dtsPath = join(__dirname, 'src', 'index.d.ts');

// Leer el archivo
let content = readFileSync(dtsPath, 'utf8');

// Reemplazar 'type IconName =' con 'export type IconName ='
content = content.replace(/^type IconName =/m, 'export type IconName =');

// Escribir el archivo modificado
writeFileSync(dtsPath, content, 'utf8');

console.log('✅ Post-build: IconName exportado correctamente');
