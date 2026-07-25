const PptxGenJS = require('pptxgenjs');
const path = require('path');

const root = __dirname;
const shots = path.join(root, 'screenshots');
const outFile = path.join(root, 'Jobsy-Presentatie.pptx');

const pptx = new PptxGenJS();
pptx.defineLayout({ name: 'WIDE', width: 13.333, height: 7.5 });
pptx.layout = 'WIDE';
pptx.author = 'Jobsy';
pptx.title = 'Jobsy — Demo-overzicht';

const GREEN = '22784A';
const DARK = '1C2421';
const CREAM = 'F5F7F4';
const MUTED = '5A645F';
const WHITE = 'FFFFFF';
const SOFT = '8CC8A0';

function img(name) {
  return path.join(shots, name);
}

// 1. Title
{
  const s = pptx.addSlide();
  s.addShape(pptx.shapes.RECTANGLE, { x: 0, y: 0, w: 13.333, h: 7.5, fill: { color: DARK } });
  s.addText('Jobsy', {
    x: 0.7, y: 2.2, w: 12, h: 1,
    fontSize: 54, fontFace: 'Segoe UI', bold: true, color: WHITE,
  });
  s.addText('Hyper-lokaal matchen op reistijd', {
    x: 0.7, y: 3.2, w: 12, h: 0.5,
    fontSize: 24, fontFace: 'Segoe UI', color: SOFT,
  });
  s.addText('Demo-overzicht · Westland / Den Haag\nRollen, banenkaart en werkgeversflows', {
    x: 0.7, y: 4.0, w: 12, h: 0.8,
    fontSize: 16, fontFace: 'Segoe UI', color: 'B4BEB9',
  });
}

// 2. What is Jobsy
{
  const s = pptx.addSlide();
  s.addShape(pptx.shapes.RECTANGLE, { x: 0, y: 0, w: 13.333, h: 7.5, fill: { color: CREAM } });
  s.addText('Wat is Jobsy?', {
    x: 0.6, y: 0.4, w: 12, h: 0.6,
    fontSize: 32, fontFace: 'Segoe UI', bold: true, color: DARK,
  });
  s.addText('Job-matching op reistijd en vervoer — niet op keywords. Lijst + kaart zoals Funda.', {
    x: 0.6, y: 1.2, w: 12, h: 0.5,
    fontSize: 18, fontFace: 'Segoe UI', color: MUTED,
  });
  s.addText([
    { text: 'Kandidaat vindt werk binnen X minuten fiets / auto / OV', options: { breakLine: true } },
    { text: 'Werkgever publiceert met tokens (basis, highlight, PushBom)', options: { breakLine: true } },
    { text: 'Rollen van filiaal tot enterprise, intermediair en admin', options: { breakLine: true } },
    { text: 'Prototype met seed-data voor sales-demo’s', options: { breakLine: true } },
  ], {
    x: 0.6, y: 2.2, w: 12, h: 3.5,
    fontSize: 20, fontFace: 'Segoe UI', color: DARK, bullet: true, paraSpacingAfter: 14,
  });
}

// 3. Banenkaart
{
  const s = pptx.addSlide();
  s.addShape(pptx.shapes.RECTANGLE, { x: 0, y: 0, w: 13.333, h: 7.5, fill: { color: CREAM } });
  s.addText('Banenkaart — het hart van de app', {
    x: 0.5, y: 0.25, w: 12.3, h: 0.45,
    fontSize: 26, fontFace: 'Segoe UI', bold: true, color: DARK,
  });
  s.addImage({ path: img('00-banenkaart-publiek.png'), x: 0.5, y: 0.85, w: 12.3, h: 6.2, sizing: { type: 'contain', w: 12.3, h: 6.2 } });
}

// 4. Roles overview
{
  const s = pptx.addSlide();
  s.addShape(pptx.shapes.RECTANGLE, { x: 0, y: 0, w: 13.333, h: 7.5, fill: { color: CREAM } });
  s.addText('Zes rollen, één platform', {
    x: 0.6, y: 0.4, w: 12, h: 0.6,
    fontSize: 32, fontFace: 'Segoe UI', bold: true, color: DARK,
  });
  const roles = [
    ['1. Kandidaat', 'zoeken, solliciteren, likes & shares'],
    ['2. Filiaalmanager', 'vacatures + tokens voor één vestiging'],
    ['3. Regiomanager', 'overzicht over vestigingen in de regio'],
    ['4. Enterprise', 'org-breed: users, regio’s, CAO, tokens'],
    ['5. Intermediair', 'batch-hiring voor opdrachtgevers'],
    ['6. Admin', 'platform KPI’s, finance, settings'],
  ];
  roles.forEach(([title, desc], i) => {
    const y = 1.3 + i * 0.9;
    s.addShape(pptx.shapes.ROUNDED_RECTANGLE, {
      x: 0.6, y, w: 12.1, h: 0.75,
      fill: { color: WHITE },
      shadow: { type: 'outer', color: '000000', blur: 4, opacity: 0.06, offset: 1 },
      rectRadius: 0.08,
    });
    s.addText(title, {
      x: 0.85, y: y + 0.15, w: 3.5, h: 0.45,
      fontSize: 18, fontFace: 'Segoe UI', bold: true, color: GREEN,
    });
    s.addText(desc, {
      x: 4.4, y: y + 0.15, w: 8, h: 0.45,
      fontSize: 16, fontFace: 'Segoe UI', color: DARK,
    });
  });
}

// 5. Kandidaat
{
  const s = pptx.addSlide();
  s.addShape(pptx.shapes.RECTANGLE, { x: 0, y: 0, w: 13.333, h: 7.5, fill: { color: CREAM } });
  s.addText('Kandidaat — dashboard & banen', {
    x: 0.4, y: 0.2, w: 12.5, h: 0.4,
    fontSize: 24, fontFace: 'Segoe UI', bold: true, color: DARK,
  });
  s.addImage({ path: img('01-kandidaat-home.png'), x: 0.35, y: 0.7, w: 6.2, h: 3.5, sizing: { type: 'contain', w: 6.2, h: 3.5 } });
  s.addImage({ path: img('01-kandidaat-vacature-detail.png'), x: 6.8, y: 0.7, w: 6.2, h: 3.5, sizing: { type: 'contain', w: 6.2, h: 3.5 } });
  s.addText('Home met eigen KPI’s · vacaturedetail met solliciteren / like / share  ·  kandidaat@jobsy.local', {
    x: 0.4, y: 4.3, w: 12.5, h: 0.35,
    fontSize: 13, fontFace: 'Segoe UI', color: MUTED,
  });
  s.addImage({ path: img('01-kandidaat-banenkaart.png'), x: 2.2, y: 4.75, w: 8.9, h: 2.5, sizing: { type: 'contain', w: 8.9, h: 2.5 } });
}

// 6. Employers
{
  const s = pptx.addSlide();
  s.addShape(pptx.shapes.RECTANGLE, { x: 0, y: 0, w: 13.333, h: 7.5, fill: { color: CREAM } });
  s.addText('Werkgevers — filiaal → regio → enterprise', {
    x: 0.4, y: 0.2, w: 12.5, h: 0.4,
    fontSize: 22, fontFace: 'Segoe UI', bold: true, color: DARK,
  });
  const tops = [
    ['02-filiaal-home.png', 'Filiaalmanager'],
    ['03-regio-home.png', 'Regiomanager'],
    ['04-enterprise-home.png', 'Enterprise'],
  ];
  tops.forEach(([file, label], i) => {
    const x = 0.35 + i * 4.3;
    s.addImage({ path: img(file), x, y: 0.7, w: 4.1, h: 2.5, sizing: { type: 'contain', w: 4.1, h: 2.5 } });
    s.addText(label, {
      x, y: 3.25, w: 4.1, h: 0.35,
      fontSize: 14, fontFace: 'Segoe UI', bold: true, color: GREEN, align: 'center',
    });
  });
  s.addImage({ path: img('04-enterprise-tokens.png'), x: 0.8, y: 3.75, w: 5.6, h: 3.4, sizing: { type: 'contain', w: 5.6, h: 3.4 } });
  s.addImage({ path: img('04-enterprise-salaristabellen.png'), x: 6.9, y: 3.75, w: 5.6, h: 3.4, sizing: { type: 'contain', w: 5.6, h: 3.4 } });
}

// 7. Intermediary + Admin
{
  const s = pptx.addSlide();
  s.addShape(pptx.shapes.RECTANGLE, { x: 0, y: 0, w: 13.333, h: 7.5, fill: { color: CREAM } });
  s.addText('Intermediair & Admin', {
    x: 0.4, y: 0.2, w: 12.5, h: 0.4,
    fontSize: 24, fontFace: 'Segoe UI', bold: true, color: DARK,
  });
  s.addImage({ path: img('05-intermediair-batch.png'), x: 0.35, y: 0.7, w: 6.2, h: 3.2, sizing: { type: 'contain', w: 6.2, h: 3.2 } });
  s.addImage({ path: img('06-admin-home.png'), x: 6.8, y: 0.7, w: 6.2, h: 3.2, sizing: { type: 'contain', w: 6.2, h: 3.2 } });
  s.addText('Intermediair: batch publiceren voor opdrachtgevers', {
    x: 0.35, y: 3.95, w: 6.2, h: 0.35,
    fontSize: 12, fontFace: 'Segoe UI', color: MUTED,
  });
  s.addText('Admin: platform-KPI’s, bedrijven, finance, settings', {
    x: 6.8, y: 3.95, w: 6.2, h: 0.35,
    fontSize: 12, fontFace: 'Segoe UI', color: MUTED,
  });
  s.addImage({ path: img('05-intermediair-opdrachtgevers.png'), x: 0.8, y: 4.4, w: 5.6, h: 2.8, sizing: { type: 'contain', w: 5.6, h: 2.8 } });
  s.addImage({ path: img('06-admin-bedrijven.png'), x: 6.9, y: 4.4, w: 5.6, h: 2.8, sizing: { type: 'contain', w: 5.6, h: 2.8 } });
}

// 8. Try it
{
  const s = pptx.addSlide();
  s.addShape(pptx.shapes.RECTANGLE, { x: 0, y: 0, w: 13.333, h: 7.5, fill: { color: DARK } });
  s.addText('Zelf proberen', {
    x: 0.7, y: 1.0, w: 12, h: 0.7,
    fontSize: 36, fontFace: 'Segoe UI', bold: true, color: WHITE,
  });
  s.addText('http://localhost:5201   ·   wachtwoord: Jobsy123!', {
    x: 0.7, y: 1.9, w: 12, h: 0.5,
    fontSize: 18, fontFace: 'Segoe UI', color: SOFT,
  });
  const accounts = [
    ['kandidaat@jobsy.local', 'Kandidaat'],
    ['ondernemer@jobsy.local', 'Filiaalmanager'],
    ['regio@jobsy.local', 'Regiomanager'],
    ['enterprise@jobsy.local', 'Enterprise'],
    ['intermediair@jobsy.local', 'Intermediair'],
    ['admin@jobsy.local', 'Admin'],
  ];
  accounts.forEach(([email, role], i) => {
    const y = 2.7 + i * 0.55;
    s.addText(email, {
      x: 0.7, y, w: 5.5, h: 0.45,
      fontSize: 16, fontFace: 'Consolas', color: 'D2DED7',
    });
    s.addText(role, {
      x: 6.5, y, w: 5, h: 0.45,
      fontSize: 16, fontFace: 'Segoe UI', color: WHITE,
    });
  });
}

pptx.writeFile({ fileName: outFile }).then(() => {
  console.log('OK', outFile);
}).catch((err) => {
  console.error(err);
  process.exit(1);
});
