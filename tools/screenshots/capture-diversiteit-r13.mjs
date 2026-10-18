import fs from 'fs';
import path from 'path';

const src = fs.readFileSync(path.join(path.dirname(new URL(import.meta.url).pathname), 'capture-diversiteit-r12.mjs'), 'utf8');
const patched = src.replace('/opt/cursor/artifacts/diversiteit-r12', '/opt/cursor/artifacts/diversiteit-r13');
fs.writeFileSync('/tmp/capture-diversiteit-r13.mjs', patched);
await import('/tmp/capture-diversiteit-r13.mjs');
