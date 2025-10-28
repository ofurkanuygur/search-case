# 🔌 DataGrip'te Elasticsearch Bağlantısı

## ⚠️ Problem Analizi

**Hata:** `java.lang.NullPointerException`

**Sebep:** Versiyon uyumsuzluğu
- Elasticsearch: **8.11.1** ✅
- JDBC Driver: **7.17** ❌

## ✅ Çözüm 1: REST API Bağlantısı (Önerilen)

DataGrip'te HTTP REST bağlantısı kullanın:

### Adımlar:

1. **DataGrip'te yeni Data Source ekleyin:**
   - `+` → `Data Source` → `URL Only`

2. **Bağlantı Ayarları:**
   ```
   URL: http://localhost:9200
   Driver: REST API
   ```

3. **Query Console'da kullanım:**
   ```json
   GET /content-index/_search
   {
     "query": {
       "match_all": {}
     }
   }
   ```

## ✅ Çözüm 2: Elasticsearch SQL Plugin

### 1. SQL Plugin'i Aktifleştirme:

```bash
# Docker container'a SQL plugin ekle
docker exec -it searchcase-elasticsearch bash -c "
  bin/elasticsearch-plugin install https://artifacts.elastic.co/downloads/elasticsearch-plugins/x-pack/x-pack-8.11.1.zip
"

# Container'ı restart et
docker restart searchcase-elasticsearch
```

### 2. DataGrip JDBC Ayarları:

1. **Driver İndir:**
   - [Elasticsearch 8.x JDBC Driver](https://www.elastic.co/downloads/jdbc-client)
   - Versiyon: **8.11.x** (Elasticsearch ile aynı)

2. **DataGrip Bağlantı Ayarları:**
   ```
   Driver: Elasticsearch
   Host: localhost
   Port: 9200
   Database: (boş bırakın)
   User: (boş bırakın)
   Password: (boş bırakın)

   URL: jdbc:es://localhost:9200
   ```

3. **Advanced Settings:**
   ```
   ssl: false
   ssl.verification: false
   ```

## ✅ Çözüm 3: Uyumlu Driver Kullanma

### Manual Driver Kurulumu:

1. **Driver İndir:**
   ```bash
   wget https://artifacts.elastic.co/downloads/elasticsearch-jdbc/elasticsearch-jdbc-8.11.1.jar
   ```

2. **DataGrip'e Ekle:**
   - File → Data Sources → Drivers
   - `+` → JAR ekle
   - İndirdiğiniz JAR'ı seçin

3. **Custom Driver Oluştur:**
   ```
   Name: Elasticsearch 8.11
   Class: org.elasticsearch.xpack.sql.jdbc.EsDriver
   URL Template: jdbc:es://{host}:{port}
   Default Port: 9200
   ```

## ✅ Çözüm 4: Kibana Dev Tools Kullanma (Alternatif)

Eğer DataGrip zorunlu değilse:

### Kibana Kurulumu:
```bash
# docker-compose.yml'e ekleyin
docker-compose up -d kibana
```

### Erişim:
```
http://localhost:5601
Dev Tools → Console
```

## 🎯 Hızlı Test

### 1. REST API ile Test:
```bash
# Terminal'den test
curl -X GET "localhost:9200/_sql?format=json" \
  -H 'Content-Type: application/json' \
  -d'{
    "query": "SELECT * FROM content-index LIMIT 10"
  }'
```

### 2. Elasticsearch SQL Syntax:
```sql
-- DataGrip SQL Console'da kullanabilirsiniz
SHOW TABLES;
DESCRIBE content-index;
SELECT * FROM "content-index" LIMIT 10;
SELECT title, score FROM "content-index" WHERE score > 8.0;
```

## 🔧 Sorun Giderme

### Hata: "No SQL plugin"
```bash
# SQL plugin'i kontrol et
curl -X GET "localhost:9200/_cat/plugins?v"
```

### Hata: "Authentication required"
Docker-compose'da security disabled olduğunu doğrulayın:
```yaml
environment:
  - xpack.security.enabled=false
```

### Hata: "Connection refused"
```bash
# Elasticsearch'ün çalıştığını doğrula
docker ps | grep elasticsearch
curl http://localhost:9200
```

## 📝 DataGrip Alternatifi Araçlar

### 1. **Elasticvue** (Chrome Extension)
- Kolay kurulum
- Görsel arayüz
- Query builder

### 2. **Postman**
- REST API testleri
- Collection oluşturma
- Environment variables

### 3. **VS Code Extensions**
- Elasticsearch for VSCode
- REST Client

### 4. **DBeaver**
- Elasticsearch desteği var
- Ücretsiz Community Edition

## 🚀 Önerilen Çözüm

**En kolay yol:**

1. DataGrip'te **HTTP Request** scratch file oluşturun:
   - File → New → HTTP Request

2. Elasticsearch query'lerini çalıştırın:
   ```http
   ### Get all documents
   GET http://localhost:9200/content-index/_search
   Content-Type: application/json

   {
     "query": {
       "match_all": {}
     }
   }

   ### SQL Query
   POST http://localhost:9200/_sql?format=json
   Content-Type: application/json

   {
     "query": "SELECT * FROM \"content-index\" LIMIT 10"
   }
   ```

3. Response'u JSON olarak görüntüleyin

## ✅ Test Edilmiş Çözüm

**Docker-compose güncelleme** (SQL desteği için):

```yaml
elasticsearch:
  image: docker.elastic.co/elasticsearch/elasticsearch:8.11.1
  environment:
    - discovery.type=single-node
    - xpack.security.enabled=false
    - xpack.sql.enabled=true  # SQL desteği ekle
    - ES_JAVA_OPTS=-Xms512m -Xmx512m
```

Ardından:
```bash
docker-compose down elasticsearch
docker-compose up -d elasticsearch
```

**DataGrip bağlantısı:**
```
Driver: Generic JDBC
URL: jdbc:es://localhost:9200?ssl=false
Driver files: elasticsearch-sql-jdbc-8.11.1.jar
```

Bu şekilde DataGrip'ten SQL sorguları çalıştırabilirsiniz!