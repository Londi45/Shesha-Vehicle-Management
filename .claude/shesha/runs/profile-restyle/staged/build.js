const fs = require('fs'), crypto = require('crypto');
const R = process.env.R;
const read = f => JSON.parse(fs.readFileSync(`${R}/${f}`, 'utf8').replace(/^\uFEFF/, ''));
const profile = read('my-profile.current.json'), tpl = read('sign-up.template.json');
const clone = o => JSON.parse(JSON.stringify(o));
const find = (ns, p) => { for (const c of ns || []) { if (p(c)) return c; const r = find(c.components, p); if (r) return r; } };
const strip = c => { const x = clone(c); delete x.components; return x; };
const fresh = c => ({ ...strip(c), id: crypto.randomUUID() });
const T = n => find(tpl.components, c => c.componentName === n);
const P = n => find(profile.components, c => c.propertyName === n);

// Outer area: centre the card, but no full-page background (this page lives inside the app layout).
const outer = fresh(T('mainContainer'));
outer.componentName = 'profilePage'; outer.propertyName = 'profilePage';
const pageStyle = "return {\r\n    width: '100%',\r\n    padding: '24px 0'\r\n}";
outer.style = pageStyle; outer.backgroundUrl = ''; outer.backgroundColor = 'transparent';
for (const bp of ['desktop', 'tablet', 'mobile']) if (outer[bp]) {
  outer[bp] = { ...outer[bp], style: pageStyle, background: { type: 'color', color: 'transparent' } };
}

// The card (already phone-safe from the sign-up work), now with a white background, rounded corners and a soft shadow.
const card = fresh(T('formContainer')); card.componentName = 'profileCard'; card.propertyName = 'profileCard';
const cardBg = { type: 'color', color: '#ffffff' };
const cardShadow = { offsetX: 0, offsetY: 4, color: 'rgba(0, 0, 0, 0.08)', blurRadius: 16, spreadRadius: 0 };
for (const bp of ['desktop', 'tablet', 'mobile']) if (card[bp]) {
  card[bp] = { ...card[bp], background: cardBg, shadow: cardShadow };
  card[bp].stylingBox = JSON.stringify({ ...JSON.parse(card[bp].stylingBox || '{}'), paddingTop: '28', paddingBottom: '28' });
}
card.stylingBox = JSON.stringify({ ...JSON.parse(card.stylingBox || '{}'), paddingTop: '28', paddingBottom: '28' });

// Titles
const textBox = fresh(T('textContainer'));
const welcomeBox = fresh(T('welcomeContainer'));
const infoBox = fresh(T('infoContainer'));
const title = { ...fresh(T('txtTitle')), content: 'My Profile', componentName: 'txtProfileTitle', propertyName: 'txtProfileTitle' };
const subtitle = { ...fresh(T('txtSubtitle')), componentName: 'txtProfileSubtitle', propertyName: 'txtProfileSubtitle',
  content: { _mode: 'code', _code: 'return "Keep your contact details up to date so we can reach you about your bookings."', _value: '' } };
welcomeBox.components = [title]; infoBox.components = [subtitle]; textBox.components = [welcomeBox, infoBox];

// Inputs: keep the profile's own components (ids, editMode, names); borrow the sign-up's rounded input style.
const inputStyle = find(tpl.components, c => c.propertyName === 'emailAddress').desktop;
const field = (name, label, placeholder, width) => {
  const f = clone(P(name)); delete f.components;
  f.label = label; if (placeholder) f.placeholder = placeholder; f.hideLabel = false;
  if (f.editMode !== 'readOnly') f.desktop = { ...(f.desktop || {}), ...clone(inputStyle) };
  f.desktop = f.desktop || {};
  f.desktop.dimensions = { ...(f.desktop.dimensions || {}), width: width ? '100%' : '100%' };
  return f;
};
// Row + half-width slots, copied from the sign-up (they stack on phones).
const signupRow = find(tpl.components, c => /^row/.test(c.componentName || ''));
const signupSlot = find(tpl.components, c => /^slot_/.test(c.componentName || ''));
const row = (name, children) => { const r = strip(signupRow); r.id = crypto.randomUUID(); r.componentName = name; r.propertyName = undefined; r.components = children; return r; };
const slot = (f) => { const s = strip(signupSlot); s.id = crypto.randomUUID(); s.componentName = 'slot_' + f.propertyName; s.propertyName = undefined; s.components = [f]; return s; };

// Section heading for the read-only licence details
const licenceHeading = { ...fresh(T('txtSubtitle')), componentName: 'txtLicenceHeading', propertyName: 'txtLicenceHeading',
  content: { _mode: 'code', _code: 'return "Driver\'s licence - managed by our staff. Contact us if it needs to change."', _value: '' } };

// Save button: keep the profile's own id + script, borrow the sign-up's primary full-width styling.
const ours = P('button1') || find(profile.components, c => c.type === 'button');
const btnTpl = T('btnCreateAccount');
const save = { ...strip(btnTpl), id: ours.id, componentName: 'btnSaveProfile', propertyName: 'btnSaveProfile',
  label: 'Save changes', buttonType: 'primary', icon: 'SaveOutlined', actionConfiguration: clone(ours.actionConfiguration) };
save.actionConfiguration.handleSuccess = false; delete save.actionConfiguration.onSuccess;

card.components = [
  textBox,
  row('rowNames', [slot(field('firstName', 'First Name', 'First name', true)), slot(field('lastName', 'Last Name', 'Last name', true))]),
  field('emailAddress', 'Email Address', 'you@example.com'),
  field('mobileNumber', 'Mobile Number', 'e.g. 082 123 4567'),
  licenceHeading,
  row('rowLicence', [slot(field('driversLicenceNumber', "Driver's Licence", null, true)), slot(field('licenceExpiryDate', 'Licence Expiry', null, true))]),
  save
];
outer.components = [card];

const stamp = (ns, pid) => ns.map(n => { n.parentId = pid; if (n.components) n.components = stamp(n.components, n.id); return n; });
const markup = { components: stamp([outer], 'root'),
  formSettings: { ...profile.formSettings, layout: 'vertical', labelCol: { span: 24 }, wrapperCol: { span: 24 }, colon: false } };

// safety: unique ids, versions, valid JSON, loader script intact
const ids = new Set(); const noVer = [];
(function w(ns) { for (const n of ns) { if (ids.has(n.id)) throw new Error('dup ' + n.id); ids.add(n.id); if (typeof n.version !== 'number') noVer.push(n.componentName); w(n.components || []); } })(markup.components);
if (noVer.length) console.log('WARN no version:', noVer);
if (!/GetProfile/.test(markup.formSettings.onBeforeDataLoad)) throw new Error('loader script lost');
if (!/UpdateMyProfile/.test(save.actionConfiguration.actionArguments.expression)) throw new Error('save script lost');
const str = JSON.stringify(markup); JSON.parse(str);
fs.writeFileSync(`${R}/my-profile.edited.json`, JSON.stringify(markup, null, 1));
fs.writeFileSync(`${R}/push.json`, JSON.stringify({ id: '1493aebb-63b1-4ec7-851b-afcf24eddb75', markup: str }));
console.log('OK components:', ids.size);
