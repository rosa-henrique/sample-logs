docker compose -f docker-compose.base.yml -f .\logstash\docker-compose.logstash.yml up -d 
docker exec -it kafka kafka-console-producer --bootstrap-server localhost:9092 --topic logs_logstash

docker compose -f docker-compose.kafka-connector.yml up -d
ocker exec kafka kafka-topics --create --bootstrap-server localhost:9092 --partitions 1 --replication-factor 1 --topic logs_connector
docker exec -it kafka kafka-console-producer --bootstrap-server localhost:9092 --topic logs_connector

{"level":"info","message":"Log direto","timestamp":"2025-06-07T18:10:00Z"}