const fs = require('fs');
const R = process.env.R;
const res = JSON.parse(fs.readFileSync(`${R}/menu.before.json`, 'utf8').replace(/^\uFEFF/, ''));
let menu = res.result; if (typeof menu === 'string') menu = JSON.parse(menu);
const items = menu.items;
const byTitle = t => items.find(i => i.title === t);
const log = [];
const CUSTOMER = ['app:CustomerArea'];

const bookings = byTitle('Bookings'), manage = byTitle('Manage'), profile = byTitle('Profile');
const child = (g, t) => (g?.childItems || []).find(c => c.title === t);

// Icons (Ant Design icon names)
const setIcon = (o, icon) => { if (o && !o.icon) { o.icon = icon; log.push(`icon ${icon} -> ${o.title}`); } };
setIcon(bookings, 'CarOutlined');
setIcon(child(bookings, 'Find A Vehicle'), 'SearchOutlined');
setIcon(child(bookings, 'My Bookings'), 'CalendarOutlined');
setIcon(manage, 'SettingOutlined');
setIcon(child(manage, 'All Bookings'), 'ScheduleOutlined');
setIcon(child(manage, 'All Vehicles'), 'CarOutlined');
setIcon(child(manage, 'Customers'), 'TeamOutlined');
setIcon(child(manage, 'Branches'), 'BankOutlined');
setIcon(profile, 'UserOutlined');
setIcon(child(profile, 'My Profile'), 'IdcardOutlined');

// Customer-only items: hidden for anyone without the customer key (e.g. admin)
for (const o of [child(bookings, 'My Bookings'), profile, child(profile, 'My Profile')]) {
  if (o) { o.requiredPermissions = CUSTOMER; log.push(`customer-only -> ${o.title}`); }
}

// Order: Home, Bookings, Profile, Manage, then Shesha's own groups
const order = ['Home Page', 'Bookings', 'Profile', 'Manage'];
items.sort((a, b) => {
  const ia = order.indexOf(a.title), ib = order.indexOf(b.title);
  return (ia === -1 ? 99 : ia) - (ib === -1 ? 99 : ib);
});
log.push('order: ' + items.map(i => i.title).join(' > '));

fs.writeFileSync(`${R}/menu.after.json`, JSON.stringify(menu, null, 1));
fs.writeFileSync(`${R}/menu.push.json`, JSON.stringify({ name: 'Shesha.MainMenuSettings', module: 'Shesha', appKey: 'default-app', value: menu }));
console.log(log.join('\n'));
