#!/usr/bin/env bash
# Publishes a handful of sample transaction-categorized events to Kafka via the running
# docker-compose stack, so you can watch fraud cases get raised without writing a producer.
set -euo pipefail

TOPIC="transaction-categorized-events"

read -r -d '' EVENTS <<'JSON' || true
event-type:transaction-categorized-event	{"transactionId":"txn-clean-1","accountId":"acc-1001","customerId":"cust-1","amount":250.00,"currency":"ZAR","category":"groceries","channel":"pos","merchantName":"Corner Store","merchantCategoryCode":"5411","isNewPayee":false,"countryCode":"ZA","occurredAt":"2026-01-01T10:00:00Z"}
event-type:transaction-categorized-event	{"transactionId":"txn-highvalue-1","accountId":"acc-1002","customerId":"cust-2","amount":75000.00,"currency":"ZAR","category":"transfer","channel":"eft","isNewPayee":false,"countryCode":"ZA","occurredAt":"2026-01-01T10:05:00Z"}
event-type:transaction-categorized-event	{"transactionId":"txn-newpayee-1","accountId":"acc-1003","customerId":"cust-3","amount":20000.00,"currency":"ZAR","category":"transfer","channel":"instantPayment","counterpartyAccountId":"payee-999","isNewPayee":true,"countryCode":"ZA","occurredAt":"2026-01-01T10:10:00Z"}
event-type:transaction-categorized-event	{"transactionId":"txn-blacklist-1","accountId":"acc-1004","customerId":"cust-4","amount":500.00,"currency":"ZAR","category":"gambling","channel":"cardNotPresent","merchantName":"Unlicensed Crypto Exchange","merchantCategoryCode":"6051","isNewPayee":false,"countryCode":"ZA","occurredAt":"2026-01-01T10:15:00Z"}
event-type:transaction-categorized-event	{"transactionId":"txn-unusualhour-1","accountId":"acc-1005","customerId":"cust-5","amount":8000.00,"currency":"ZAR","category":"transfer","channel":"online","isNewPayee":false,"countryCode":"ZA","occurredAt":"2026-01-01T02:00:00Z"}
JSON

# Defaults: headers.delimiter is a tab (separating the header block from the JSON payload),
# headers.key.separator is ':' (separating a header's key from its value) — both match the lines above.
echo "${EVENTS}" | docker compose exec -T kafka /opt/kafka/bin/kafka-console-producer.sh \
  --bootstrap-server localhost:9092 \
  --topic "${TOPIC}" \
  --property "parse.headers=true"

echo "Published sample events to '${TOPIC}'."
