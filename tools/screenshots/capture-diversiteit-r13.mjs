import fs from 'fs';
import path from 'path';
import { pathToFileURL } from 'url';

const src = fs.readFileSync(path.join(path.dirname(new URL(import.meta.url).pathname), 'capture-diversiteit-r12.mjs'), 'utf8');
const patched = src.replace('/opt/cursor/artifacts/diversiteit-r12', '/opt/cursor/artifacts/diversiteit-r13');
const tmp = path.join(path.dirname(new URL(import.meta.url).pathname), '.capture-diversiteit-r13-run.mjs');
fs.writeFileSync(tmp, patched);
await import(pathToFileURL(tmp).href);
