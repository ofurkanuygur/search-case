# 📊 Dashboard ve Monitoring Rehberi

## 🐰 RabbitMQ Management Dashboard

### Erişim
```
URL: http://localhost:15672
Username: guest
Password: guest
```

### Önemli Sekmeler ve Ne Gösterirler:

#### 1. **Overview (Genel Bakış)**
- **Message rates**: Mesaj akış grafiği (publish/deliver/acknowledge)
- **Queued messages**: Toplam bekleyen mesajlar
- **Node status**: RabbitMQ node durumu
- **Connections**: Aktif bağlantı sayısı
- **Channels**: Açık kanal sayısı

#### 2. **Queues** ⭐ (En Önemli)
Bu sekmede görecekleriniz:
- **cache-worker-queue**
- **search-worker-queue**

Her queue için:
- **Ready**: İşlenmeyi bekleyen mesaj sayısı
- **Unacked**: İşleniyor olan mesaj sayısı
- **Total**: Toplam mesaj
- **Incoming**: Gelen mesaj/saniye
- **Deliver/Get**: İşlenen mesaj/saniye
- **Consumers**: Bağlı consumer sayısı

**Nasıl Analiz Edilir:**
- Ready > 0 ise: Mesajlar birikiyor, consumer yavaş
- Unacked > 0 ise: Mesajlar işleniyor
- Consumers = 0 ise: Worker down olmuş
- Message rates grafiği: Performans analizi

#### 3. **Exchanges**
MassTransit'in otomatik oluşturduğu exchange'ler:
- `ContentBatchUpdatedEvent` exchange'i
- Fanout type exchange'ler

#### 4. **Connections**
Aktif bağlantılar:
- WriteService
- EventBusService
- CacheWorker
- SearchWorker

#### 5. **Admin**
- Virtual hosts
- Users
- Permissions

### 🔍 RabbitMQ'da Event Flow İzleme:

```bash
# 1. Queues sekmesine gidin
# 2. "cache-worker-queue" veya "search-worker-queue" tıklayın
# 3. "Get Messages" bölümünde mesajları görebilirsiniz
# 4. "Message rates" grafiğinde akışı izleyin
```

---

## 🔍 Elasticsearch Dashboards

### 1. **SearchWorker Swagger UI** ✅
```
URL: http://localhost:8006/swagger
```
API endpoints:
- `GET /api/search` - Arama
- `GET /api/search/{id}` - ID ile getir
- `GET /api/search/stats` - İstatistikler
- `POST /api/search/index` - Index oluştur
- `DELETE /api/search/index` - Index sil

### 2. **Kibana** (Opsiyonel - Kurulu değil)
Profesyonel Elasticsearch dashboard'u. Kurulum:

```yaml
# docker-compose.yml'e ekleyin:
kibana:
  image: docker.elastic.co/kibana/kibana:8.11.1
  container_name: searchcase-kibana
  environment:
    - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
    - ELASTICSEARCH_USERNAME=elastic
    - ELASTICSEARCH_PASSWORD=changeme
    - SERVER_NAME=kibana
    - SERVER_HOST=0.0.0.0
  ports:
    - "5601:5601"
  depends_on:
    - elasticsearch
  networks:
    - searchcase-network
```

**Kibana Özellikleri:**
- Discover: Log ve doküman arama
- Visualize: Grafik ve görselleştirmeler
- Dashboard: Custom dashboard'lar
- Dev Tools: Elasticsearch query console
- Stack Monitoring: Performans monitoring

### 3. **Elasticvue** (Hafif Alternatif)
Chrome Extension veya standalone uygulama:
- Chrome Extension: "Elasticvue" araması
- Web: https://elasticvue.com/

### 4. **Dejavu** (Web Tabanlı)
```bash
docker run -p 1358:1358 -d appbaseio/dejavu
# Erişim: http://localhost:1358
# Connect: http://localhost:9200
```

---

## 📊 Mevcut Sistemde Monitoring

### API Üzerinden Manuel Kontrol:

#### Elasticsearch İstatistikleri:
```bash
# Cluster sağlığı
curl -X GET "localhost:9200/_cluster/health?pretty"

# Node bilgileri
curl -X GET "localhost:9200/_nodes/stats?pretty"

# Index istatistikleri
curl -X GET "localhost:9200/content-index/_stats?pretty"

# Tüm dokümanları görme
curl -X GET "localhost:9200/content-index/_search?pretty&size=100"
```

#### Redis Monitoring:
```bash
# Redis Commander kurulumu (Web UI)
npm install -g redis-commander
redis-commander --redis-host localhost --redis-port 6379

# Erişim: http://localhost:8081
```

#### RabbitMQ API:
```bash
# Queue detayları
curl -u guest:guest http://localhost:15672/api/queues/%2F/cache-worker-queue

# Message rates
curl -u guest:guest http://localhost:15672/api/queues/%2F/cache-worker-queue/message-stats
```

---

## 🚀 Hızlı Test Senaryosu

### Event Flow'u Canlı İzleme:
```bash
# Terminal 1: RabbitMQ Management açık
# http://localhost:15672 -> Queues sekmesi

# Terminal 2: Test event gönder
curl -X POST "http://localhost:8005/api/cache/refresh" \
  -H "Content-Type: application/json" \
  -d '["provider1_v1"]'

# RabbitMQ'da görecekleriniz:
# 1. Message rate spike'ı
# 2. Ready -> Unacked -> Acknowledged flow
# 3. Consumer processing
```

---

## 📈 Performans Metrikleri İzleme

### Önemli Metrikler:

1. **RabbitMQ**:
   - Message rate (msg/sec)
   - Queue depth (bekleyen mesaj)
   - Consumer utilization
   - Connection count

2. **Elasticsearch**:
   - Index size
   - Document count
   - Search latency
   - Indexing rate

3. **Redis**:
   - Memory usage
   - Key count
   - Hit/Miss ratio
   - Commands/sec

### Health Check Endpoints:
```bash
# Tüm servisler
curl http://localhost:8003/health  # WriteService
curl http://localhost:8004/health  # EventBusService
curl http://localhost:8005/health  # CacheWorker
curl http://localhost:8006/health  # SearchWorker
```

---

## 🎯 Dashboard Gerekliliği

### ✅ **Mevcut Yeterli mi?**
Development için yeterli:
- RabbitMQ Management UI ✅
- Swagger UI ✅
- API endpoints ✅

### 🔄 **Production için Öneriler:**

1. **Kibana**: Elasticsearch için profesyonel dashboard
2. **Grafana + Prometheus**: Tüm sistem metrikleri
3. **Redis Commander**: Redis monitoring
4. **APM (Application Performance Monitoring)**:
   - Elastic APM
   - New Relic
   - DataDog

### 📊 Basit Dashboard Örneği:
```bash
# Tek komutla tüm durumu görme
watch -n 2 'echo "=== SYSTEM STATUS ===" && \
echo "PostgreSQL:" && docker exec search-db psql -U postgres -d searchcase -t -c "SELECT COUNT(*) FROM contents" && \
echo "Redis Keys:" && docker exec searchcase-redis redis-cli DBSIZE && \
echo "Elasticsearch Docs:" && curl -s localhost:9200/content-index/_count | jq .count && \
echo "RabbitMQ Queues:" && curl -s -u guest:guest http://localhost:15672/api/queues | jq ".[].messages"'
```

---

## 🔗 Hızlı Erişim Linkleri

| Servis | Dashboard | URL |
|--------|-----------|-----|
| RabbitMQ | Management UI | http://localhost:15672 |
| Elasticsearch | Direct API | http://localhost:9200 |
| SearchWorker | Swagger UI | http://localhost:8006/swagger |
| CacheWorker | Swagger UI | http://localhost:8005/swagger |
| WriteService | Hangfire | http://localhost:8003/hangfire |
| EventBusService | Swagger UI | http://localhost:8004/swagger |
| PostgreSQL | pgAdmin | http://localhost:5050 |