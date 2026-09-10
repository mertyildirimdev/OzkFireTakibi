# Dashboard doğrulaması

Depo kökünden PowerShell ile çalıştırın:

```powershell
./dokumanlar/dogrulama/Verify-Dashboard.ps1
```

.NET 10 SDK ve NuGet önbelleğinde `Microsoft.EntityFrameworkCore.InMemory` 10.0.11 gerektirir. Uygulamayı derler; doğrulama kodunu geçici dizinde çalıştırır. Yeni proje veya uygulama paket bağımlılığı eklemez. Gerçek veritabanına bağlanmaz, şema ve verileri değiştirmez.

35 kontrol; mağaza erişimi ve önbellek ayrımını, eşik sınırını, mazeret sayfalamasını/aramasını, aylık genel görünümü ve Razor bileşenlerinin örnek verilerle HTML çıktısını doğrular. Ana sayfanın örnek çıktısı çalıştırma sonunda yazılan geçici dizindeki `home.html` dosyasıdır. Bu çıktı statiktir; uygulamanın canlı oturumu değildir. Derleme ayrı bir geçici dizine yapılır; açık uygulamanın dosyalarını kilitlemez.

İş kuralları:

- `User` rolü yalnızca eşlenmiş mağazasının rapor satırlarını görür. Mağaza numarası önceliklidir; eski isim eşlemesi desteklenir. Eşleşme yoksa erişim verilmez. Merkez rollerinin rapor kapsamı korunur.
- Genel fire oranı, mağazanın eşik karşılaştırması için referans olarak gösterilir; diğer mağazaların satırları ve genel parasal toplamlar mağaza kullanıcısına verilmez.
- Ana sayfa, en son raporlama ayındaki tüm bilinen kapsamları yan yana gösterir. Kapsamlar ilk görüldükleri aydan itibaren takip edilir; gelecekte eklenen gruplar geçmişte eksik sayılmaz. Bu bir kapsam tanım ekranı değildir; artık kullanılmayan kapsamlar geçmişte varsa eksik listesinde kalabilir.
- Aylık değişimler, aynı kategori imzasına sahip önceki takvim ayının tam aylık raporuyla hesaplanır. Kısmi aylar karşılaştırılmaz. Eksik aylık rapor yerine eski veya kümülatif veri kullanılmaz.
- Çakışabilecek grupların tutarları toplanmaz, oranlarının ortalaması alınmaz. Eşik üzerindeki mağazalar mağaza numarasıyla tekilleştirilir. Öncelik, etkilenen grup sayısına ve ardından tek gruptaki en büyük kayba göre belirlenir.
- Ana sayfadaki mazeret bağlantıları seçili ayın tüm aktif aylık raporlarını kapsar. Mağaza erişim sınırı her sorguda korunur.
- Eşiğe eşit değerler otomatik mazeret servisindeki kuralla aynı şekilde değerlendirilir. Mazeret kapsamı dışındaki mağazalar öncelik listesine girmez.
- Mazeretler varsayılan 25, en fazla 100 kayıtla sayfalanır. Analiz kırılımları sayfa başına 50 satır gösterir. Arama ve filtreler sayfalama öncesinde uygulanır.

Bu kontroller SQL Server bağlantısını, üretim verisinin bütünlüğünü veya gerçek kullanıcı oturumuyla uçtan uca tarayıcı akışını doğrulamaz.
