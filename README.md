# Akıllı Zaman Yönetimi ve Üretkenlik Asistanı

Bu proje, kullanıcıların günlük görevlerini etkili bir şekilde planlamalarına, iş verimliliğini artırmalarına ve zaman yönetimini optimize etmelerine yardımcı olmak amacıyla geliştirilmiş bir masaüstü uygulamasıdır.

## 🎯 Proje Hedefleri

Bu proje, özellikle yoğun iş temposuna sahip bireyler, öğrenciler ve profesyoneller için tasarlanmıştır. Kullanıcıların görevlerini verimli bir şekilde organize edebilmeleri, çalışma sürelerini analiz edebilmeleri ve üretkenliklerini artırabilmeleri için gelişmiş özellikler sunar.

## 🏗️ Teknoloji Stack

- **C# 12**
- **.NET 8**
- **.NET MAUI** (Multi-platform App UI)
- **Entity Framework Core** (Code First yaklaşımı)
- **ASP.NET Core Minimal API**
- **SQL Server**
- **LINQ**
- **CommunityToolkit.Mvvm**
- **BCrypt.Net** (Şifre hashleme)

## 📋 Temel Özellikler

### 1. Kullanıcı Yönetimi
- ✅ Kullanıcı girişi ve kayıt
- ✅ Şifre değiştirme
- ✅ Kullanıcı rolleri (Admin, Standart Kullanıcı)
- ✅ Güvenli şifre hashleme

### 2. Görev Yönetimi
- ✅ Görev ekleme, düzenleme, silme (CRUD işlemleri)
- ✅ Görev kategorileri (İş, Kişisel, Spor, Eğitim, Alışveriş)
- ✅ Öncelik seviyeleri (Düşük, Orta, Yüksek, Kritik)
- ✅ Görev durumları (Başlamadı, Devam Ediyor, Tamamlandı, İptal, Beklemede)
- ✅ Tarih ve saat yönetimi
- ✅ Bugünkü görevler ve geciken görevler

### 3. Zaman Takibi
- ✅ Görev bazında zaman takibi
- ✅ Başlat/Durdur zamanlayıcı
- ✅ Tahmini ve gerçek süre karşılaştırması
- ✅ Detaylı zaman logları

### 4. Hatırlatıcılar
- ✅ Zaman bazlı hatırlatıcılar
- ✅ Tekrarlayan hatırlatıcılar (Günlük, Haftalık, Aylık)
- ✅ Görev bağlantılı hatırlatıcılar

### 5. Raporlama ve Analiz
- ✅ Günlük, haftalık, aylık raporlar
- ✅ Üretkenlik skorları
- ✅ Kategori bazlı istatistikler
- ✅ Tamamlama oranları
- ✅ Zaman analizi

## 🏛️ Mimari Yapı

Proje, temiz mimari prensipleri kullanılarak katmanlı yapıda geliştirilmiştir:

```
SmartTimeManagement/
├── SmartTimeManagement.MAUI/          # MAUI UI katmanı
│   ├── Views/                         # XAML sayfaları
│   ├── ViewModels/                    # MVVM ViewModels
│   ├── Services/                      # UI servisleri
│   └── Converters/                    # XAML converters
├── SmartTimeManagement.API/           # Minimal API katmanı
├── SmartTimeManagement.Core/          # İş mantığı katmanı
│   ├── Entities/                      # Domain modelleri
│   └── Interfaces/                    # Servis interfaceleri
├── SmartTimeManagement.Data/          # Veri erişim katmanı
│   ├── Context/                       # DbContext
│   └── Services/                      # Servis implementasyonları
```

## 🔧 Kullanılan Tasarım Desenleri

- **Repository Pattern**: Veri erişim katmanında
- **Dependency Injection**: Servis bağımlılıkları için
- **MVVM Pattern**: MAUI UI katmanında
- **Observer Pattern**: CommunityToolkit.Mvvm ile

## 📱 MAUI Kontrolleri

Projede kullanılan MAUI kontrolleri:
- ✅ **CollectionView**: Görev ve hatırlatıcı listeleri
- ✅ **Picker**: Kategori ve öncelik seçimi
- ✅ **DatePicker/TimePicker**: Tarih ve saat seçimi
- ✅ **CheckBox**: Görev tamamlama durumu
- ✅ **Entry**: Metin girişleri
- ✅ **Button**: Eylemler
- ✅ **Frame**: Kart görünümü
- ✅ **Grid/StackLayout**: Düzen yönetimi

## 🗄️ Veritabanı Yapısı

### Tablolar:
1. **Users**: Kullanıcı bilgileri
2. **Categories**: Görev kategorileri
3. **Tasks**: Görev detayları
4. **TaskTimeLogs**: Zaman takip kayıtları
5. **Reminders**: Hatırlatıcılar
6. **Reports**: Raporlar

### Özellikler:
- ✅ Entity Framework Code First
- ✅ Migration desteği
- ✅ Seed data
- ✅ İlişkisel veri modeli
- ✅ Audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)

## ⚙️ Kurulum ve Çalıştırma

### Ön Gereksinimler
- .NET 8 SDK
- Visual Studio 2022 (MAUI workload ile)
- SQL Server LocalDB

### Kurulum Adımları

1. **Projeyi klonlayın:**
```bash
git clone [repository-url]
cd SmartTimeManagement
```

2. **NuGet paketlerini yükleyin:**
```bash
dotnet restore
```

3. **Veritabanını oluşturun:**
```bash
cd SmartTimeManagement.API
dotnet ef database update
```

4. **API'yi çalıştırın:**
```bash
cd SmartTimeManagement.API
dotnet run
```

5. **MAUI uygulamasını çalıştırın:**
```bash
cd SmartTimeManagement.MAUI
dotnet run -f net8.0-windows10.0.19041.0
```

## 🔐 Varsayılan Kullanıcı

Sistem ilk çalıştırıldığında otomatik olarak admin kullanıcısı oluşturulur:
- **Email**: admin@smarttime.com
- **Şifre**: Admin123!

## 📊 Özellik Detayları

### Validasyon
- ✅ Data Annotations kullanımı
- ✅ Fluent Validation desteği
- ✅ Client-side validasyon
- ✅ Boş alan kontrolleri
- ✅ Email format kontrolü
- ✅ Şifre güçlülük kontrolü

### LINQ Kullanımı
Projede LINQ aktif olarak kullanılmıştır:
- Görev filtreleme ve sıralama
- Raporlama ve istatistikler
- Kategori bazlı gruplama
- Tarih aralığı sorguları

### Responsive Design
- Modern ve kullanıcı dostu arayüz
- Farklı ekran boyutlarına uyumlu
- Tutarlı renk şeması
- İntuitive navigasyon

## 🚀 Proje Durumu

Bu proje tüm gereksinimler eksiksiz olarak tamamlanmıştır:

✅ **Proje MAUi ile yapılmış**
✅ **Minimal Api projesi oluşturulmuş**
✅ **Entity Framework Code First yaklaşımı kullanılmış**
✅ **Migrationlar eklenmiş**
✅ **Veri tabanı bağlantısı yapılmış**
✅ **CRUD işlemleri tamamlanmış**
✅ **Kullanıcı girişi/çıkışı yapılmış**
✅ **Şifre değiştirme sayfası yapılmış**
✅ **CollectionView, Picker, DatePicker, TimePicker, CheckBox kullanılmış**
✅ **Service Interface'leri ve sınıfları oluşturulmuş**
✅ **OOP prensipleri uygulanmış**
✅ **Veri doğrulama implementasyonu tamamlanmış**
✅ **LINQ aktif olarak kullanılmış**
✅ **C# isimlendirme kurallarına uyulmuş**

## 🎨 Ekran Görüntüleri

Uygulama şu sayfaları içermektedir:
- Giriş sayfası
- Ana sayfa (Dashboard)
- Görev listesi
- Görev ekleme/düzenleme
- Hatırlatıcılar
- Raporlar
- Ayarlar

## 🤝 Katkıda Bulunma

Bu proje eğitim amaçlı geliştirilmiştir. Geliştirme önerileri ve katkılar memnuniyetle karşılanır.

## 📄 Lisans

Bu proje eğitim amaçlı geliştirilmiştir.

---

**Geliştirici**: [Adınız]
**Tarih**: Haziran 2025
**Teknoloji**: .NET 8, MAUI, Entity Framework Core
