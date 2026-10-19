T=$(cat "$R/access-token"); B=http://localhost:21021
show() { sqlcmd -S . -d VehicleBooking -E -C -W -s"|" -Q "SET NOCOUNT ON; SELECT n.Name type, m.RecipientText [to], m.Subject, m.StatusLkp status, LEFT(ISNULL(m.ErrorMessage,''),90) error, CONVERT(varchar(8),n.CreationTime,108) at FROM dbo.Core_Notifications n LEFT JOIN dbo.Core_NotificationMessages m ON m.PartOfId=n.Id WHERE n.TriggeringEntityId='$1' ORDER BY n.CreationTime"; }
START=$(date -d '+12 hours' '+%Y-%m-%dT%H:00:00'); END=$(date -d '+60 hours' '+%Y-%m-%dT%H:00:00')
echo "== 1. Create booking for Thabo: Toyota Fortuner, $START -> $END"
RES=$(curl -s -X POST "$B/api/services/app/Bookings/CreateBooking" -H "Authorization: Bearer $T" -H "Content-Type: application/json" \
  -d "{\"vehicleId\":\"9bac9a9f-47c4-42bc-9e41-8a2a9d423ff4\",\"customerId\":\"3558cf30-e44f-4a4c-80eb-15441f934987\",\"startDate\":\"$START\",\"endDate\":\"$END\",\"notes\":\"curl notification test - delete me\"}")
echo "$RES" | head -c 300; echo
ID=$(echo "$RES" | node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{try{console.log(JSON.parse(s).result||'')}catch{}})")
[ -z "$ID" ] && { echo "no booking id - stopping"; exit 1; }
echo "$ID" > "$R/test-booking-id"; sleep 5; show "$ID"
echo; echo "== 2. Run reminder job"
curl -s -X POST "$B/api/services/Scheduler/ScheduledJob/StartJob" -H "Authorization: Bearer $T" -H "Content-Type: application/json" -d '{"jobId":"7c1e5b2a-4d3f-4a8e-9b6c-2f0d8e1a3b45"}' | head -c 200; echo
sleep 10; show "$ID"; sqlcmd -S . -d VehicleBooking -E -C -W -h -1 -Q "SET NOCOUNT ON; SELECT 'ReminderSentDate = '+ISNULL(CONVERT(varchar(19),ReminderSentDate,120),'NULL') FROM dbo.VehicleBooking_Bookings WHERE Id='$ID'"
echo; echo "== 3. Run reminder job again (expect no second reminder)"
curl -s -X POST "$B/api/services/Scheduler/ScheduledJob/StartJob" -H "Authorization: Bearer $T" -H "Content-Type: application/json" -d '{"jobId":"7c1e5b2a-4d3f-4a8e-9b6c-2f0d8e1a3b45"}' > /dev/null
sleep 10; show "$ID"
echo; echo "== 4. Cancel the booking"
curl -s -X POST "$B/api/services/app/Bookings/CancelBooking?bookingId=$ID" -H "Authorization: Bearer $T" -H "Content-Type: application/json" | head -c 200; echo
sleep 5; show "$ID"
