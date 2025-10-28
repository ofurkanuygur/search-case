# SearchWorker Test Senaryoları

## 🚀 Başlatma ve Kurulum

### 1. Tüm Servisleri Başlatma

```bash
# Tüm servisleri başlat
docker-compose up -d elasticsearch search-db rabbitmq

# Elasticsearch'ün hazır olmasını bekle (60 saniye)
sleep 60

# Elasticsearch sağlık kontrolü
curl -X GET "localhost:9200/_cluster/health?pretty"

# SearchWorker'ı başlat
cd src/SearchWorker
dotnet run
```

### 2. Index Oluşturma

```bash
# Index var mı kontrol et
curl -X GET "localhost:8006/api/search/index/exists"

# Index yoksa oluştur
curl -X POST "localhost:8006/api/search/index"

# Index mapping'lerini kontrol et
curl -X GET "localhost:9200/content-index/_mapping?pretty"
```

## 📊 Event-Driven Test Senaryoları

### Senaryo 1: Content Batch Update Event İşleme

```bash
# 1. WriteService'i çalıştır (content sync yapar)
cd ../WriteService
dotnet run

# 2. WriteService sync job'ını tetikle
curl -X POST "localhost:8003/hangfire/jobs/enqueue/content-sync"

# 3. RabbitMQ'da mesajları kontrol et
# RabbitMQ Management UI: http://localhost:15672
# Username: guest, Password: guest
# Queues -> search-worker-queue kontrol et

# 4. SearchWorker loglarını kontrol et
# Beklenen: "Processing ContentBatchUpdatedEvent with X content IDs"

# 5. Elasticsearch'te index'lenen içerikleri kontrol et
curl -X GET "localhost:9200/content-index/_search?pretty&size=5"
```

### Senaryo 2: Retry ve Error Handling

```bash
# 1. Elasticsearch'ü durdur
docker-compose stop elasticsearch

# 2. WriteService'ten event gönder
curl -X POST "localhost:8003/hangfire/jobs/enqueue/content-sync"

# 3. SearchWorker loglarını kontrol et
# Beklenen:
# - "Failed to index batch"
# - "Retry attempt 1/3"
# - "Retry attempt 2/3"
# - "Retry attempt 3/3"
# - "Circuit breaker opened"

# 4. Elasticsearch'ü başlat
docker-compose start elasticsearch
sleep 30

# 5. Circuit breaker reset'i bekle (30 saniye)
# Beklenen: "Circuit breaker closed, resuming normal operation"
```

## 🔍 Search API Test Senaryoları

### Senaryo 3: Full-Text Search

```bash
# 1. Arama yap
curl -X GET "localhost:8006/api/search?query=technology&page=1&pageSize=10"

# Beklenen Response:
{
  "items": [
    {
      "id": "...",
      "title": "Technology Trends",
      "score": 8.5,
      "highlights": {
        "title": ["<em>Technology</em> Trends"]
      }
    }
  ],
  "totalCount": 25,
  "page": 1,
  "pageSize": 10
}

# 2. Fuzzy search (yazım hataları ile)
curl -X GET "localhost:8006/api/search?query=tecnology&page=1&pageSize=10"
# Beklenen: "technology" ile ilgili sonuçlar (fuzzy matching)

# 3. Multi-field search
curl -X GET "localhost:8006/api/search?query=AI%20machine%20learning&page=1&pageSize=5"
```

### Senaryo 4: Get Document by ID

```bash
# 1. Mevcut bir ID ile
curl -X GET "localhost:8006/api/search/content-001"

# 2. Olmayan bir ID ile
curl -X GET "localhost:8006/api/search/non-existent-id"
# Beklenen: 404 Not Found
```

### Senaryo 5: Index Statistics

```bash
# Index istatistiklerini al
curl -X GET "localhost:8006/api/search/stats"

# Beklenen Response:
{
  "documentCount": 150,
  "indexSizeMb": 2.5,
  "shards": {
    "total": 1,
    "successful": 1,
    "failed": 0
  },
  "health": "green"
}
```

## 🔄 Integration Test Senaryoları

### Senaryo 6: End-to-End Content Flow

```bash
# 1. PostgreSQL'de yeni content ekle
docker exec search-db psql -U postgres -d searchcase -c "
INSERT INTO contents (id, title, content_type, source_provider, score, content_hash, published_at)
VALUES ('test-001', 'Test Content for Search', 'article', 'manual', 9.5, 'hash123', NOW())
RETURNING id;"

# 2. WriteService ile sync tetikle
curl -X POST "localhost:8003/hangfire/jobs/enqueue/content-sync"

# 3. SearchWorker'ın event'i işlemesini bekle (5-10 saniye)
sleep 10

# 4. Elasticsearch'te ara
curl -X GET "localhost:8006/api/search?query=Test%20Content%20for%20Search"

# 5. Content'i ID ile getir
curl -X GET "localhost:8006/api/search/test-001"
```

### Senaryo 7: Batch Processing Performance

```bash
# 1. PostgreSQL'e 100 content ekle
for i in {1..100}; do
  docker exec search-db psql -U postgres -d searchcase -c "
  INSERT INTO contents (id, title, content_type, source_provider, score, content_hash, published_at)
  VALUES ('perf-test-$i', 'Performance Test Content $i', 'article', 'batch-test', RANDOM() * 10, 'hash-$i', NOW());"
done

# 2. Sync başlat ve zamanla
time curl -X POST "localhost:8003/hangfire/jobs/enqueue/content-sync"

# 3. SearchWorker loglarını kontrol et
# Beklenen: "Indexed 100 documents in batch"
# Süre: < 5 saniye

# 4. Elasticsearch'te kontrol et
curl -X GET "localhost:9200/content-index/_count?q=source_provider:batch-test"
```

## 🏥 Health Check Senaryoları

### Senaryo 8: Service Health Monitoring

```bash
# 1. Overall health
curl -X GET "localhost:8006/health"

# 2. Readiness probe
curl -X GET "localhost:8006/health/ready"

# 3. Liveness probe
curl -X GET "localhost:8006/health/live"

# 4. Dependency health details
curl -X GET "localhost:8006/health" -H "Accept: application/json" | jq .
```

### Senaryo 9: Degraded Service

```bash
# 1. PostgreSQL'i durdur
docker-compose stop search-db

# 2. Health check
curl -X GET "localhost:8006/health"
# Beklenen: "Unhealthy" - PostgreSQL down

# 3. Search hala çalışmalı (Elasticsearch up)
curl -X GET "localhost:8006/api/search?query=test"
# Beklenen: Başarılı response (degraded mode)

# 4. PostgreSQL'i başlat
docker-compose start search-db
sleep 10

# 5. Health check tekrar
curl -X GET "localhost:8006/health"
# Beklenen: "Healthy"
```

## 🔧 Maintenance Senaryoları

### Senaryo 10: Index Rebuild

```bash
# 1. Mevcut index'i sil
curl -X DELETE "localhost:8006/api/search/index"

# 2. Yeni index oluştur
curl -X POST "localhost:8006/api/search/index"

# 3. Full re-index tetikle
# PostgreSQL'den tüm content'leri çek ve index'le
curl -X POST "localhost:8003/hangfire/jobs/enqueue/content-sync"

# 4. Progress kontrol
watch -n 2 'curl -s localhost:9200/content-index/_count | jq .count'
```

## 📈 Load Testing

### Senaryo 11: Concurrent Search Requests

```bash
# Apache Bench kullanarak load test
# 100 concurrent request, toplam 1000 request
ab -n 1000 -c 100 "http://localhost:8006/api/search?query=test"

# Beklenen:
# - Response time: < 100ms (p95)
# - Error rate: < 1%
# - Throughput: > 100 req/sec
```

### Senaryo 12: Memory ve CPU Monitoring

```bash
# Container resource kullanımı
docker stats searchcase-search-worker --no-stream

# Elasticsearch memory kullanımı
curl -X GET "localhost:9200/_nodes/stats/jvm?pretty" | grep heap_used_percent

# SearchWorker .NET memory
# Loglar: "Memory usage", "GC collection"
```

## 🐛 Debugging Senaryoları

### Senaryo 13: Logging Levels

```bash
# 1. Debug logging aktif et
export Logging__LogLevel__Default=Debug
dotnet run

# 2. Elasticsearch slow query log
curl -X PUT "localhost:9200/content-index/_settings" -H 'Content-Type: application/json' -d'
{
  "index.search.slowlog.threshold.query.debug": "1ms"
}'

# 3. RabbitMQ message tracing
docker exec searchcase-rabbitmq rabbitmqctl trace_on
```

## ✅ Test Validation Checklist

- [ ] Index başarıyla oluşturuluyor
- [ ] RabbitMQ event'leri consume ediliyor
- [ ] Batch indexing çalışıyor (< 1000ms for 100 docs)
- [ ] Search API doğru sonuç döndürüyor
- [ ] Fuzzy search çalışıyor
- [ ] Pagination doğru çalışıyor
- [ ] Health checks doğru status döndürüyor
- [ ] Retry mechanism çalışıyor
- [ ] Circuit breaker çalışıyor
- [ ] Memory limitleri aşılmıyor (< 512MB)
- [ ] Concurrent request'ler handle ediliyor
- [ ] Index statistics doğru

## 🔗 İlgili Komutlar

```bash
# Docker logs
docker logs -f searchcase-search-worker

# Elasticsearch cluster info
curl -X GET "localhost:9200/_cluster/stats?human&pretty"

# RabbitMQ queue info
curl -u guest:guest "localhost:15672/api/queues"

# Index mapping
curl -X GET "localhost:9200/content-index/_mapping?pretty"

# All indexed documents
curl -X GET "localhost:9200/content-index/_search?pretty&size=100"

# Delete test data
curl -X POST "localhost:9200/content-index/_delete_by_query" -H 'Content-Type: application/json' -d'
{
  "query": {
    "match": {
      "source_provider": "batch-test"
    }
  }
}'
```

## 📝 Expected Log Patterns

### Başarılı İşlem
```
[INF] SearchWorker started successfully
[INF] Connected to Elasticsearch at http://elasticsearch:9200
[INF] Index 'content-index' initialized
[INF] Consuming ContentBatchUpdatedEvent from queue 'search-worker-queue'
[INF] Processing ContentBatchUpdatedEvent with 50 content IDs
[INF] Fetched 50 documents from database
[INF] Indexed 50 documents to Elasticsearch in 823ms
```

### Hata Durumu
```
[WRN] Failed to index batch: Elasticsearch cluster is unavailable
[INF] Retry attempt 1/3 after 2 seconds
[ERR] All retry attempts exhausted for batch
[WRN] Circuit breaker opened due to consecutive failures
```

### Recovery
```
[INF] Circuit breaker closed after timeout
[INF] Successfully indexed previously failed batch
[INF] System recovered to healthy state
```