const fs = require('fs'), crypto = require('crypto');
const R = process.env.R;
const read = f => JSON.parse(fs.readFileSync(R + '/' + f, 'utf8').replace(/^\uFEFF/, ''));
const login = read('login.json'), signup = read('Sign-up.json');
fs.writeFileSync(R + '/Sign-up.current.json', JSON.stringify(signup, null, 1));   // backup

const clone = o => JSON.parse(JSON.stringify(o));
const find = (nodes, pred) => { for (const c of nodes || []) { if (pred(c)) return c; const r = find(c.components, pred); if (r) return r; } };
const newId = () => crypto.randomUUID();
const strip = c => { const x = clone(c); delete x.components; return x; };
const fresh = c => ({ ...strip(c), id: newId() });

const L = n => find(login.components, c => c.componentName === n);
const S = n => find(signup.components, c => c.propertyName === n || c.componentName === n);

// --- page + card (from login) ---
const main = fresh(L('mainContainer'));
const card = fresh(L('formContainer'));
const logo = fresh(L('imgLogo'));
const textBox = fresh(L('textContainer'));
const welcomeBox = fresh(L('welcomeContainer'));
const title = { ...fresh(L('textWelcome')), content: 'Create your account', componentName: 'txtTitle', propertyName: 'txtTitle' };
const infoBox = fresh(L('infoContainer'));
const info = { ...fresh(L('txtInfo')), componentName: 'txtSubtitle', propertyName: 'txtSubtitle',
  content: { _mode: 'code', _code: 'return "Book a car in minutes. It only takes a moment."', _value: '' } };
const validation = fresh(L('validationErrors1'));
welcomeBox.components = [title]; infoBox.components = [info]; textBox.components = [welcomeBox, infoBox];

// --- inputs: keep our components, give them the login's rounded input style, labels on top ---
const inputStyle = L('password').desktop;
const field = (name, placeholder, width) => {
  const f = clone(S(name)); delete f.components;
  f.placeholder = placeholder; f.hideLabel = false;
  f.desktop = { ...(f.desktop || {}), ...clone(inputStyle) };
  if (f.type === 'dateField') f.desktop = { ...(f.desktop || {}), dimensions: { ...(inputStyle.dimensions || {}) } };
  if (width) f.desktop.dimensions = { ...(f.desktop.dimensions || {}), width };
  return f;
};
const rowTemplate = strip(L('container9'));
const row = children => ({ ...clone(rowTemplate), id: newId(), componentName: 'row' + Math.random().toString(36).slice(2, 7),
  propertyName: undefined, gap: '12px', justifyContent: 'space-between', flexWrap: 'nowrap',
  desktop: { ...clone(rowTemplate.desktop), flexDirection: 'row', display: 'flex', gap: '12px',
             dimensions: { ...(rowTemplate.desktop.dimensions || {}), width: '100%' } },
  components: children });
const half = 'calc(50% - 6px)';

// --- primary button: login's styling, our script ---
const ours = S('button1');
const btn = { ...strip(L('btnSignIn')), id: ours.id, componentName: 'btnCreateAccount', propertyName: 'btnCreateAccount',
  label: 'Create account', buttonType: 'primary', actionConfiguration: clone(ours.actionConfiguration) };
btn.actionConfiguration.handleSuccess = false; delete btn.actionConfiguration.onSuccess;

// --- footer link: "Already have an account? Log in" ---
const footer = fresh(L('container9'));
const footerText = { ...fresh(L('text4')), content: 'Already have an account?', componentName: 'txtHaveAccount', propertyName: 'txtHaveAccount' };
const loginLink = { ...fresh(L('button2')), label: 'Log in', componentName: 'btnGoToLogin', propertyName: 'btnGoToLogin',
  actionConfiguration: { _type: 'action-config', actionName: 'Navigate', actionOwner: 'shesha.common', handleFail: false, handleSuccess: false,
    actionArguments: { version: 0, navigationType: 'url', url: '/login' } } };
footer.components = [footerText, loginLink];

card.components = [
  logo, textBox, validation,
  row([field('firstName', 'First name', half), field('lastName', 'Last name', half)]),
  field('emailAddress', 'you@example.com'),
  field('mobileNumber', 'e.g. 082 123 4567'),
  row([field('driversLicenceNumber', 'Licence number', half), field('licenceExpiryDate', 'Expiry date', half)]),
  field('password', 'Choose a password'),
  field('confirmPassword', 'Repeat your password'),
  btn, footer
];
main.components = [card];

// --- stamp parentIds ---
const stamp = (nodes, pid) => nodes.map(n => { n.parentId = pid; if (n.components) n.components = stamp(n.components, n.id); return n; });
const markup = { components: stamp([main], 'root'),
  formSettings: { ...signup.formSettings, layout: 'vertical', labelCol: { span: 24 }, wrapperCol: { span: 24 }, colon: false, access: 5 } };

// --- safety checks ---
const ids = new Set(); let missingVersion = [];
(function walk(ns) { for (const n of ns) { if (ids.has(n.id)) throw new Error('dup id ' + n.id); ids.add(n.id);
  if (typeof n.version !== 'number') missingVersion.push(n.componentName); walk(n.components || []); } })(markup.components);
if (missingVersion.length) console.log('WARN missing version:', missingVersion);
const str = JSON.stringify(markup); JSON.parse(str); JSON.parse(JSON.stringify({ markup: str }));
fs.writeFileSync(R + '/Sign-up.edited.json', JSON.stringify(markup, null, 1));
console.log('OK components:', ids.size, 'length:', str.length);
