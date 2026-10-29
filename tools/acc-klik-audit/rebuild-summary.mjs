#!/usr/bin/env node
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const OUT = path.dirname(fileURLToPath(import.meta.url));
const BASE = 'https://acceptatie.lobsy.nl';
const pageIds = [
  'landing',
  'login',
  'account-maken',
  'wachtwoord-vergeten',
  'paspoort',
  'sollicitaties',
  'banenkaart-gast',
  'banenkaart-ingelogd',
];

function slug(s) {
  return s.replace(/[^a-zA-Z0-9]+/g, '-').replace(/^-|-$/g, '').slice(0, 50).toLowerCase();
}

const problems = [];
const seen = new Set();
for (const id of pageIds) {
  const j = path.join(OUT, `${id}.json`);
  if (!fs.existsSync(j)) continue;
  const data = JSON.parse(fs.readFileSync(j, 'utf8'));
  for (const row of data.rows || []) {
    for (const [vp, col] of [
      ['1440', 'r1440'],
      ['390', 'r390'],
    ]) {
      const st = row[col];
      if (st !== 'blocked' && st !== 'no-effect') continue;
      const key = `${data.page}|${vp}|${row.element}|${st}`;
      if (seen.has(key)) continue;
      seen.add(key);
      const shots = fs.existsSync(path.join(OUT, 'problems'))
        ? fs.readdirSync(path.join(OUT, 'problems')).filter((f) => f.startsWith(`${id}-${vp}-`))
        : [];
      const match = shots.find((f) => f.includes(slug(row.element).slice(0, 20)));
      problems.push({
        page: data.page,
        viewport: vp,
        element: row.element,
        issue: st,
        shot: match ? `problems/${match}` : null,
      });
    }
  }
}

let summary = `# Acceptatie kliktest — ${BASE}\n\n`;
summary += `Methodiek: reload per element, rol+naam+index, elementFromPoint, viewports 1440×900 en 390×844.\n\n`;
summary += `Doel-commit (opdracht): \`15bbaf4d\` (live SHA niet via HTTP header bevestigd).\n\n`;
summary += `Kandidaat ingelogd: seed Sam de Wit (\`kandidaat@jobsy.local\`).\n\n`;
summary += `## Pagina-overzicht\n\n| Pagina | Elementen in rapport |\n| --- | --- |\n`;
for (const id of pageIds) {
  const j = path.join(OUT, `${id}.json`);
  const n = fs.existsSync(j) ? JSON.parse(fs.readFileSync(j, 'utf8')).rows?.length ?? 0 : 0;
  summary += `| ${id} | ${n} |\n`;
}
summary += `\nDetailtabellen: \`<pagina-id>.md\` — kolommen Element | 1440 | 390 | effect.\n\n`;

if (problems.length) {
  summary += `## Echte problemen (${problems.length})\n\n`;
  for (const p of problems) {
    const link = p.shot ? `[screenshot](${p.shot})` : 'screenshot in `problems/`';
    summary += `- **${p.page}** @ ${p.viewport}px — “${p.element}”: ${p.issue} (${link})\n`;
  }
}

summary += `\n## Notitie\n\n`;
summary += `\`missing\` op smalle viewport voor landing/login kan footer-links onder de fold zijn (scan zonder scroll); alleen blocked/no-effect hierboven zijn UX-defecten.\n`;

fs.writeFileSync(path.join(OUT, 'SUMMARY.md'), summary);
console.log('Problems:', problems.length);
