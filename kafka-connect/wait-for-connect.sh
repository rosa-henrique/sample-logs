#!/bin/bash

echo "Aguardando Kafka Connect estar pronto..."
until curl -s http://localhost:8083/connectors; do
  sleep 5
done

echo "Registrando conector..."
curl -X POST http://localhost:8083/connectors \
  -H "Content-Type: application/json" \
  -d @/etc/kafka-connect/connector.json