const B = 'http://localhost:21021';
const T = require('fs').readFileSync(process.env.R + '/access-token', 'utf8').trim();
const MODULE = '6caa5009-a024-46e4-b899-a9d776528520';
const call = async (method, path, body) => {
  const r = await fetch(B + path, { method, headers: { Authorization: 'Bearer ' + T, 'Content-Type': 'application/json; charset=utf-8' }, body: body && JSON.stringify(body) });
  const j = await r.json().catch(() => ({}));
  if (!r.ok || j.success === false) throw new Error(method + ' ' + path + ' -> ' + r.status + ' ' + JSON.stringify(j.error || j).slice(0, 400));
  return j.result;
};
const defs = [
  { name: 'BookingConfirmed', label: 'Booking Confirmed', description: 'Sent to the customer when a booking is created.',
    subject: 'Your booking for {{VehicleName}} is confirmed',
    body: 'Hi {{CustomerName}},<br/><br/>Your booking is confirmed:<br/><ul><li><strong>Vehicle:</strong> {{VehicleName}}</li><li><strong>From:</strong> {{StartDate}}</li><li><strong>To:</strong> {{EndDate}}</li><li><strong>Total:</strong> {{TotalCost}}</li></ul>Thank you for booking with us!' },
  { name: 'BookingCancelled', label: 'Booking Cancelled', description: 'Sent to the customer when a booking is cancelled.',
    subject: 'Your booking for {{VehicleName}} has been cancelled',
    body: "Hi {{CustomerName}},<br/><br/>Your booking for <strong>{{VehicleName}}</strong> from {{StartDate}} to {{EndDate}} has been cancelled.<br/><br/>If this wasn't you, please contact us." },
  { name: 'BookingReminder', label: 'Booking Reminder', description: 'Sent to the customer the day before a booking starts.',
    subject: 'Reminder: your {{VehicleName}} booking starts soon',
    body: 'Hi {{CustomerName}},<br/><br/>Just a reminder that your booking for <strong>{{VehicleName}}</strong> starts on <strong>{{StartDate}}</strong>.<br/><br/>See you soon!' },
];
(async () => {
  for (const d of defs) {
    const type = await call('POST', '/api/dynamic/Shesha/NotificationTypeConfig/Crud/Create', {
      name: d.name, label: d.label, description: d.description, module: { id: MODULE },
      disable: false, canOptOut: false, isTimeSensitive: false, allowAttachments: false });
    const tpl = await call('POST', '/api/dynamic/Shesha/NotificationTemplate/Crud/Create', {
      titleTemplate: d.subject, bodyTemplate: d.body, messageFormat: 2, partOf: { id: type.id } });
    console.log('created', d.name, 'type', type.id, 'email template', tpl.id);
  }
})().catch(e => { console.error('FAILED:', e.message); process.exit(1); });
