# 🚀 EventBus Test Rehberi

## Sistem Mimarisi
```
WriteService → EventBusService → RabbitMQ → Workers (CacheWorker, SearchWorker)
```

## 📊 Test Senaryoları

### 1️⃣ RabbitMQ Management UI Kontrolü

```bash
# RabbitMQ Management UI'a erişin
http://localhost:15672
Username: guest
Password: guest
```

**Kontrol Edilecekler:**
- **Queues** sekmesinde `cache-worker-queue` ve `search-worker-queue` görünmeli
- **Exchanges** sekmesinde MassTransit exchange'leri görünmeli
- **Connections** sekmesinde aktif bağlantılar görünmeli

### 2️⃣ EventBusService API Kontrolü

```bash
# EventBusService sağlık kontrolü
curl -X GET "http://localhost:8004/health"

# EventBusService Swagger UI
http://localhost:8004/swagger
```

### 3️⃣ Test Event Gönderme

#### A. WriteService Üzerinden (Önerilen)

```bash
# WriteService content sync tetikle
curl -X POST "http://localhost:8003/hangfire/jobs/enqueue/content-sync"
```

Bu komut:
1. WriteService provider'lardan veri çeker
2. PostgreSQL'e kaydeder
3. **ContentBatchUpdatedEvent** publish eder
4. CacheWorker ve SearchWorker bu event'i consume eder

#### B. Manual Test Event

```bash
# PostgreSQL'e test data ekle
docker exec search-db psql -U postgres -d searchcase -c "
INSERT INTO contents (id, title, content_type, source_provider, score, content_hash, published_at, created_at, updated_at)
VALUES
  ('test-001', 'Test Content 1', 'article', 'test', 8.5, 'hash001', NOW(), NOW(), NOW()),
  ('test-002', 'Test Content 2', 'video', 'test', 9.2, 'hash002', NOW(), NOW(), NOW())
RETURNING id, title;"

# WriteService sync'i tetikle
curl -X POST "http://localhost:8003/hangfire/jobs/enqueue/content-sync"
```

### 4️⃣ Worker'ların Consume Ettiğini Doğrulama

#### CacheWorker Kontrolü

```bash
# CacheWorker loglarını izle
docker logs -f searchcase-cache-worker --tail 50

# Beklenen loglar:
# [INF] Consuming ContentBatchUpdatedEvent from queue 'cache-worker-queue'
# [INF] Processing ContentBatchUpdatedEvent with X content IDs
# [INF] Successfully cached X contents in Redis
```

#### Redis'te Doğrulama

```bash
# Redis CLI ile kontrol
docker exec -it searchcase-redis redis-cli

# Redis komutları
KEYS content:*
GET content:test-001
ZRANGE content:by_score 0 -1 WITHSCORES
```

#### SearchWorker Kontrolü (Local çalıştırın)

```bash
# SearchWorker'ı local başlat
cd src/SearchWorker
dotnet run

# Logları izle - Beklenen:
# [INF] Consuming ContentBatchUpdatedEvent from queue 'search-worker-queue'
# [INF] Processing ContentBatchUpdatedEvent with X content IDs
# [INF] Indexed X documents to Elasticsearch
```

#### Elasticsearch'te Doğrulama

```bash
# Index kontrolü
curl -X GET "localhost:9200/content-index/_search?pretty&q=test"

# Tüm dokümanlar
curl -X GET "localhost:9200/content-index/_search?pretty&size=100"
```

### 5️⃣ RabbitMQ Queue İstatistikleri

```bash
# Queue detayları
curl -s -u guest:guest http://localhost:15672/api/queues | jq '.[] | {name: .name, messages: .messages, consumers: .consumers}'

# Exchange'ler
curl -s -u guest:guest http://localhost:15672/api/exchanges | jq '.[] | select(.name | contains("ContentBatchUpdated"))'

# Bağlantılar
curl -s -u guest:guest http://localhost:15672/api/connections | jq '.[] | {name: .name, state: .state}'
```

### 6️⃣ End-to-End Test Senaryosu

```bash
# 1. Başlangıç durumu - Queue'ları temizle (opsiyonel)
docker exec searchcase-rabbitmq rabbitmqctl purge_queue cache-worker-queue
docker exec searchcase-rabbitmq rabbitmqctl purge_queue search-worker-queue

# 2. Test verisi ekle
docker exec search-db psql -U postgres -d searchcase -c "
INSERT INTO contents (id, title, content_type, source_provider, score, content_hash, published_at, created_at, updated_at)
VALUES ('e2e-test-$(date +%s)', 'E2E Test Content', 'article', 'e2e-test', 9.9, 'hash-$(date +%s)', NOW(), NOW(), NOW());"

# 3. Sync tetikle
curl -X POST "http://localhost:8003/hangfire/jobs/enqueue/content-sync"

# 4. Queue'larda mesaj sayısı kontrol (5 saniye bekle)
sleep 5
curl -s -u guest:guest http://localhost:15672/api/queues | jq '.[] | {queue: .name, messages: .messages_ready, consumers: .consumers}'

# 5. CacheWorker loglarını kontrol
docker logs searchcase-cache-worker --tail 20 | grep "Processing ContentBatchUpdatedEvent"

# 6. Redis'te veriyi kontrol
docker exec searchcase-redis redis-cli KEYS "content:e2e-test*"

# 7. Elasticsearch'te veriyi kontrol (SearchWorker çalışıyorsa)
curl -X GET "localhost:9200/content-index/_search?q=e2e-test&pretty"
```

### 7️⃣ Monitoring Dashboard

RabbitMQ Management UI'da izlenecekler:
1. **Overview** → Message rates grafiği
2. **Queues** → Her queue için:
   - Ready messages
   - Unacked messages
   - Total messages
   - Message rates
   - Consumer count
3. **Connections** → Active consumers
4. **Channels** → Message flow

### 8️⃣ Troubleshooting

#### Queue'da mesaj birikmesi
```bash
# Queue durumu
docker exec searchcase-rabbitmq rabbitmqctl list_queues name messages consumers

# Consumer yoksa worker'ı restart et
docker restart searchcase-cache-worker
```

#### Connection sorunları
```bash
# RabbitMQ bağlantıları
docker exec searchcase-rabbitmq rabbitmqctl list_connections

# Worker logları
docker logs searchcase-cache-worker --tail 50 | grep -E "ERR|WARN|Failed"
```

#### Event gönderilmiyor
```bash
# EventBusService logları
docker logs searchcase-eventbus --tail 50

# WriteService logları
docker logs searchcase-write-service --tail 50 | grep "Publishing"
```

## ✅ Başarılı Test Kriterleri

1. ✅ RabbitMQ'da queue'lar oluşmuş
2. ✅ Consumer'lar queue'lara bağlı
3. ✅ WriteService event publish ediyor
4. ✅ CacheWorker event consume ediyor ve Redis'e yazıyor
5. ✅ SearchWorker event consume ediyor ve Elasticsearch'e indexliyor
6. ✅ Queue'larda mesaj birikimi yok
7. ✅ Error log yok

## 📝 Özet Komutlar

```bash
# Tüm servislerin durumu
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# RabbitMQ queue durumu
curl -s -u guest:guest http://localhost:15672/api/queues | jq '.[] | {name: .name, messages: .messages}'

# Test event gönder
curl -X POST "http://localhost:8003/hangfire/jobs/enqueue/content-sync"

# Worker loglarını izle
docker logs -f searchcase-cache-worker --tail 20

# Redis kontrolü
docker exec searchcase-redis redis-cli DBSIZE

# Elasticsearch kontrolü
curl -X GET "localhost:9200/_cat/indices?v"
```

## 🔗 Erişim Linkleri

- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **WriteService Hangfire**: http://localhost:8003/hangfire
- **EventBusService Swagger**: http://localhost:8004/swagger
- **Elasticsearch**: http://localhost:9200
- **Redis Commander** (opsiyonel): `npm install -g redis-commander && redis-commander`