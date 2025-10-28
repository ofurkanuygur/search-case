# ✅ DataGrip Elasticsearch Bağlantı Çözümü

## 🔴 Problem: JDBC Lisans Hatası
```
DBMS: Elasticsearch (ver. 8.11.1)
current license is non-compliant for [jdbc]
```

## 🟢 Çözüm: HTTP Request Kullanımı

### Neden HTTP Request?
- **JDBC** → Ticari lisans gerektirir (Gold/Platinum/Enterprise)
- **SQL API** → Basic lisans ile çalışır ✅
- **HTTP Request** → Ücretsiz ve tam fonksiyonel ✅

## 📋 Adım Adım Kurulum

### 1. DataGrip'te HTTP Request Dosyası Oluşturma

#### Yöntem A: Scratch File
1. DataGrip'i açın
2. `Cmd+Shift+N` (Mac) veya `Ctrl+Shift+Alt+Insert` (Windows)
3. **HTTP Request** seçin

#### Yöntem B: Menüden
1. **File** → **New** → **Scratch File** → **HTTP Request**

#### Yöntem C: Mevcut Dosyayı Kullanma
1. `datagrip-http-queries.http` dosyasını açın
2. DataGrip otomatik olarak HTTP Request olarak tanıyacak

### 2. Sorguları Çalıştırma

Her sorgunun yanında yeşil **▶ Run** butonu görünecek:

```http
### Tabloları Göster
POST http://localhost:9200/_sql?format=txt
Content-Type: application/json

{
  "query": "SHOW TABLES"
}
```

**Çıktı:**
```
catalog      |     name      |     type      |     kind
------------------+---------------+---------------+---------------
searchcase-cluster|content-index  |TABLE          |INDEX
```

### 3. Veri Sorgulama

```http
### İlk 5 Kayıt
POST http://localhost:9200/_sql?format=txt
Content-Type: application/json

{
  "query": "SELECT id, title, score FROM \"content-index\" LIMIT 5"
}
```

## 🎯 Test Edilmiş ve Çalışan Örnekler

### ✅ Tablo Formatında Sonuçlar
```bash
curl -X POST "localhost:9200/_sql?format=txt" \
  -H 'Content-Type: application/json' \
  -d'{"query": "SELECT id, title, score FROM \"content-index\" LIMIT 5"}'
```

**Sonuç:**
```
id       |        title         |     score
---------------+----------------------+---------------
test-1         |DataGrip Test         |9.5
test-2         |Elasticsearch SQL Test|8.7
```

### ✅ JSON Formatında Sonuçlar
```bash
curl -X POST "localhost:9200/_sql?format=json" \
  -H 'Content-Type: application/json' \
  -d'{"query": "SELECT * FROM \"content-index\" LIMIT 2"}'
```

### ✅ CSV Export
```bash
curl -X POST "localhost:9200/_sql?format=csv" \
  -H 'Content-Type: application/json' \
  -d'{"query": "SELECT id, title, score FROM \"content-index\""}'
```

## 📊 DataGrip'te Kullanım İpuçları

### 1. Format Seçenekleri
- `format=txt` → **En okunabilir** (tablo görünümü)
- `format=json` → Programatik işlem için
- `format=csv` → Excel'e export için
- `format=tsv` → Tab-separated
- `format=yaml` → YAML formatı

### 2. Hızlı Kısayollar
- `Ctrl+Enter` → Sorguyu çalıştır
- `Ctrl+Alt+E` → Tüm sorguları çalıştır
- `Alt+X` → Execution sonucunu temizle

### 3. Response Görüntüleme
DataGrip otomatik olarak:
- JSON'ı formatlar
- Tabloları düzenler
- Syntax highlighting yapar

## 🔍 Lisans Durumu Kontrolü

### Mevcut Lisans:
```bash
curl -X GET "localhost:9200/_license" | jq '.license.type'
# Çıktı: "basic"
```

### SQL Desteği Kontrolü:
```bash
curl -X GET "localhost:9200/_xpack?filter_path=features.sql"
# SQL enabled: true ✅
```

## ⚡ Performans Karşılaştırması

| Özellik | JDBC Driver | HTTP Request |
|---------|-------------|--------------|
| **Lisans** | Gold+ gerekli ❌ | Basic yeterli ✅ |
| **Kurulum** | Driver indirme gerekli | Hazır ✅ |
| **Bağlantı** | Connection pooling | Stateless |
| **Format** | Sadece JDBC | txt/json/csv/yaml |
| **DataGrip Desteği** | Native | HTTP Client |

## 🚀 Önerilen Workflow

### Development İçin:
1. `datagrip-http-queries.http` dosyasını kullanın
2. HTTP Request Scratch file'ları oluşturun
3. Sonuçları JSON/CSV olarak export edin

### Production İçin:
1. Application'dan REST API kullanın
2. NEST client (C#) veya native client kullanın
3. SQL yerine native Elasticsearch Query DSL tercih edin

## 📝 Alternatif Araçlar

### 1. **Kibana** (Resmi)
```bash
# docker-compose.yml'e ekleyin
kibana:
  image: docker.elastic.co/kibana/kibana:8.11.1
  ports:
    - "5601:5601"
  environment:
    - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
```

### 2. **Elasticvue** (Hafif)
- Chrome Extension
- Web: https://elasticvue.com

### 3. **Postman**
- REST API testleri için
- Collection'lar oluşturabilirsiniz

## ✅ Özet

**Problem:** JDBC commercial license gerekiyor
**Çözüm:** HTTP Request + SQL API kullanımı
**Avantajlar:**
- ✅ Ücretsiz (Basic lisans yeterli)
- ✅ DataGrip'te çalışıyor
- ✅ Tüm SQL özellikleri mevcut
- ✅ Multiple format desteği
- ✅ Kurulum gerektirmiyor

## 🎉 Sonuç

DataGrip'te Elasticsearch'e başarıyla bağlandınız!

**Kullanım:**
1. `datagrip-http-queries.http` dosyasını açın
2. İstediğiniz sorguyu seçin
3. ▶ Run butonuna tıklayın
4. Sonuçları görüntüleyin

Artık JDBC lisans hatası almadan tüm Elasticsearch SQL özelliklerini kullanabilirsiniz!