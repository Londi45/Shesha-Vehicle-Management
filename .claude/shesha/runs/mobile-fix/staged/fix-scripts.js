const fs = require('fs');
const R = process.env.R;
const read = f => JSON.parse(fs.readFileSync(`${R}/${f}`, 'utf8').replace(/^\uFEFF/, ''));

// 1. create-booking: permission check in try/catch, defaulting to the safe (customer) view
const cb = read('create-booking.live.json');
console.log('create-booking BEFORE:\n' + cb.formSettings.onBeforeDataLoad + '\n');
cb.formSettings.onBeforeDataLoad = [
  'form.setFieldsValue({...form.formArguments});',
  'try {',
  '  const canManage = await application.user.hasPermissionAsync("app:ManageBookings");',
  '  form.setFieldsValue({ canManageBookings: canManage });',
  '} catch (e) {',
  '  // If we cannot tell, treat the user as a customer: the Customer box stays hidden (the safe choice).',
  '  form.setFieldsValue({ canManageBookings: false });',
  '}'
].join('\r\n');

// 2. my-bookings: Cancel uses Shesha's http (current login, no hard-coded server address) instead of fetch + token hunting
const mb = read('my-bookings.live.json');
const newCancel = [
  'const row = data; // in a table row action, data is the clicked row',
  'if (!row?.id) {',
  '  message.error("No booking selected");',
  '  return;',
  '}',
  'if (!confirm("Cancel booking for " + (row.displayName || "this vehicle") + "?")) return;',
  '',
  'try {',
  '  await http.post("/api/services/app/Bookings/CancelBooking?bookingId=" + row.id);',
  '  message.success("Booking cancelled");',
  '} catch (e) {',
  '  message.error(e?.response?.data?.error?.message ?? "Could not cancel booking");',
  '  throw e; // tells Shesha the action failed, so "on success" steps (like refreshing) do not run',
  '}'
].join('\r\n');
let replaced = 0;
(function walk(o) {
  if (Array.isArray(o)) return o.forEach(walk);
  if (!o || typeof o !== 'object') return;
  if (o.actionArguments && typeof o.actionArguments.expression === 'string' && /CancelBooking/.test(o.actionArguments.expression) && /fetch\(/.test(o.actionArguments.expression)) {
    o.actionArguments.expression = newCancel; replaced++;
  }
  Object.values(o).forEach(walk);
})(mb.components);
console.log('my-bookings Cancel scripts replaced:', replaced);

for (const [file, form] of [['push-create-booking.json', cb], ['push-my-bookings.json', mb]]) {
  const str = JSON.stringify(form); JSON.parse(str);
  fs.writeFileSync(`${R}/${file}`, JSON.stringify({ id: file.includes('create') ? process.env.CB : '8ebc6666-4e9d-4bf8-8954-90de6c0dd66a', markup: str }));
}
