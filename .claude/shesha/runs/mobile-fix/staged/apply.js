const fs = require('fs');
const R = process.env.R;
const load = n => JSON.parse(fs.readFileSync(`${R}/${n}.before.json`, 'utf8').replace(/^\uFEFF/, ''));
const walk = (o, fn) => { if (Array.isArray(o)) return o.forEach(x => walk(x, fn)); if (!o || typeof o !== 'object') return; fn(o); Object.values(o).forEach(v => walk(v, fn)); };
const log = [];
const plan = {
  'Vehicle-search-page': { anchor: true, hideButtons: ['Add'], viewName: 'Available cars' },
  'my-bookings':         { anchor: true, hideButtons: ['Add'], viewName: 'My bookings' },
  'booking-table':       { anchor: true, renameCols: { 'Cancel Boooking': 'Cancel' } },
  'my-profile':          { profileButton: true },
};
for (const [name, p] of Object.entries(plan)) {
  const m = load(name);
  walk(m.components, o => {
    if (p.anchor && o.columnType === 'action' && o.anchored !== 'right') { o.anchored = 'right'; log.push(`${name}: pinned "${o.caption}" column right`); }
    if (p.renameCols && o.columnType && p.renameCols[o.caption]) { log.push(`${name}: caption "${o.caption}" -> "${p.renameCols[o.caption]}"`); o.caption = p.renameCols[o.caption]; }
    if (p.hideButtons && o.itemType === 'item' && p.hideButtons.includes(o.label) && o.buttonType !== undefined) { o.hidden = true; o.visible = false; log.push(`${name}: hid "${o.label}" button`); }
    if (p.viewName && Array.isArray(o.filters)) o.filters.forEach(f => { if (f.name && f.name !== p.viewName) { log.push(`${name}: view "${f.name}" -> "${p.viewName}"`); f.name = p.viewName; } });
    if (p.profileButton && o.label === 'Update Profile') {
      if (o.type === 'button') { o.icon = 'SaveOutlined'; o.buttonType = 'primary'; log.push(`${name}: Update Profile button -> primary + save icon`); }
      else if (o.itemType === 'item') { o.icon = 'SaveOutlined'; o.buttonType = 'primary'; }
    }
  });
  const str = JSON.stringify(m); JSON.parse(str);
  fs.writeFileSync(`${R}/${name}.after.json`, JSON.stringify(m, null, 1));
}
console.log(log.join('\n'));
