# Fire raporu parse stratejisi

İki dosyayı da satır satır inceleyip test ettim. Aşağıdaki kurallar gerçek veriye dayanıyor (varsayım değil).

## 1. Satır tipi → hedef tablo eşleştirmesi

Her satırdaki `rpr_tip` (ve `rpr_id`) alanı, satırın hangi seviyeyi temsil ettiğini söylüyor. Bunu **birincil sınıflandırma anahtarı** olarak kullan — `Depo No == 0` veya `Kategori Kodu == "Tümü"` gibi alan içeriğine bakarak seviye tahmin etmeye çalışma, çünkü bu değerler her seviyede farklı anlam taşıyabiliyor (aşağıda 3. bölümde bir istisnasını göreceksin).

| rpr_id | rpr_tip | Anlamı | Hedef tablo |
|---|---|---|---|
| 1 | `Genel Durum <tarih aralığı>` | Şirket geneli tek satır, dönem bilgisi de bu metnin içinde | `Donem` (özet kolonları) |
| 2 | `Genel Durum Grup Detaylı` | Şirket geneli, kategori kırılımlı | `KategoriFireOzeti` |
| 3 | `Şubeler Genel Durum` | **Mağaza bazlı toplam — mazeret motorunun asıl girdisi** | `SubeFireOzeti` |
| 4 | `Şubeler Grup Detaylı` | Mağaza × kategori | `SubeKategoriFire` |
| 5 | `Stoklar Genel` | Şirket geneli, ürün kırılımlı | `UrunFireOzeti` |
| 7 | `Şubeler Stok Detaylı` | Mağaza × kategori × ürün (en granüler) | `SubeUrunFire` |

`rpr_id = 1` satırının `rpr_tip` metni dinamik ("Genel Durum 28.03.2026-28.04.2026" gibi), o yüzden bu satırı `rpr_id == 1` ile yakala, diğerlerini `rpr_tip` tam eşleşmesiyle. Beklenmeyen bir `rpr_tip` gelirse **exception fırlat, satırı sessizce atlama** — ERP rapor formatı değişirse bunu importta anında görmen lazım.

## 2. Boyut (dimension) tabloları ve doğal anahtarları

Dosyaları karşılaştırınca şunu doğruladım: bu üç alan **dönemler ve dosyalar arası stabil**, yani ayrı master tablolarda tutulup dönem bazlı fact tablolarından FK ile referans alınmalı.

| Varlık | Doğal anahtar | Doğrulama |
|---|---|---|
| `Sube` | `DepoNo` (int) | 2 dosyada ortak 42 depo no test edildi, hepsinde `DepoAdı` birebir örtüştü. Aynı dosya içinde de tek depo no'ya birden fazla isim bağlanmıyor. |
| `Kategori` | `KategoriKodu` (string, örn. `15.001.0001`) | Her iki dosyada da tam olarak aynı 6 kategori kodu var, fark yok. |
| `Urun` | `StokKodu` (string, örn. `02.007.001.00150`) | Bkz. bölüm 3 — bir istisna var. |

`DepoNo`, `KategoriKodu`, `StokKodu` değeri `"Tümü"` veya `0` olan satırlar bir üst seviyenin özet/rollup satırlarıdır — bunlar dimension tablosuna değil, kendi seviyesinin fact tablosuna gider (örn. `Depo No = 0, Depo Adı = "Tümü"` olan satır bir mağaza değil, şirket geneli özetidir).

## 3. Tuzak: `Stoklar Genel` seviyesinde `Stok Kodu` hep `"Tümü"`

Test ettim: **204 satırın 204'ünde de** `Stoklar Genel` (rpr_id=5) satırlarında `Stok Kodu = "Tümü"`, ama `Stok İsmi` doğru dolu geliyor. Yani bu seviyede ürünü koduyla değil, ismiyle ayırt edebiliyorsun — ama fact tablosunda hâlâ `StokKodu` FK'sini kullanmak istiyorsun (isim üzerinden join kırılgan olur).

Çözüm — aynı dosya içinde daha granüler olan `Şubeler Stok Detaylı` (rpr_id=7) satırları hem isim hem kod içeriyor. Test ettim: bu seviyede **204 benzersiz ürün ismi var ve hiçbiri birden fazla koda eşlenmiyor** (isim ↔ kod tam 1:1). Yani:

1. Dosyayı işlerken önce `rpr_id=7` satırlarını tara, `Dictionary<string StokIsmi, string StokKodu>` oluştur.
2. Sonra `rpr_id=5` satırlarını işlerken `StokKodu`'nu bu sözlükten çöz.
3. Sözlükte bulunamayan bir isim çıkarsa (yeni ürün, henüz hiçbir mağazada satılmamış vs.) satırı `StokKodu = null` ile "eşleşmedi" kuyruğuna at, importu durdurma ama logla — manuel gözden geçirme gerektirir.

Bu iki geçişli (two-pass) parse mantığı zorunlu: önce detay seviyesi okunmalı, özet seviyesi ondan sonra çözümlenmeli.

## 4. Fact tablolarının benzersizlik anahtarları

Her fact tablosu `DonemId` ile birlikte composite unique index almalı:

| Tablo | Composite unique key | Test sonucu |
|---|---|---|
| `SubeFireOzeti` | `(DonemId, DepoNo)` | — |
| `SubeKategoriFire` | `(DonemId, DepoNo, KategoriKodu)` | — |
| `SubeUrunFire` | `(DonemId, DepoNo, KategoriKodu, StokKodu)` | 4895 satırın hepsi bu üçlüde benzersiz çıktı, çakışma yok |
| `KategoriFireOzeti` | `(DonemId, KategoriKodu)` | — |
| `UrunFireOzeti` | `(DonemId, StokKodu)` | Kod bölüm 3'teki eşleştirmeden sonra doldurulur |
| `Donem` | `(BaslangicTarihi, BitisTarihi, DonemTipi)` | — |

## 5. Dönem anahtarı ve "kesinleşen" yeniden yükleme

`Donem` satırının tarih aralığını `rpr_id=1` satırının `rpr_tip` metninden regex ile çıkar: `Genel Durum (\d{2}\.\d{2}\.\d{4})-(\d{2}\.\d{2}\.\d{4})`.

`DonemTipi` (Aylık / Kümülatif) için dosya adına güvenme — iki dosyanda da "AYLIK" kelimesi geçiyor ama biri tek aylık, diğeri 4 aylık kümülatif. Bunun yerine **tarih aralığının uzunluğuna göre hesapla**: `(Bitis - Baslangic).Days <= 31` ise Aylık, değilse Kümülatif. Bu veri odaklı ve dile/isimlendirmeye bağımlı değil.

Bir dönem daha önce "taslak" olarak yüklenmiş, sonra "kesinleşen" versiyonu geldiyse (senin ikinci dosyan tam olarak bu senaryo — Nisan ayının kesinleşmiş hali): aynı `(BaslangicTarihi, BitisTarihi, DonemTipi)` eşleşirse **yeni `Donem` satırı açma**, mevcut `DonemId`'yi bul, o `DonemId`'ye bağlı tüm fact satırlarını sil, yeni parse edilen satırları tek transaction içinde yeniden ekle. ~5000 satırlık hacimde per-row upsert yerine delete+reinsert hem daha basit hem daha güvenli (kaynak dosyayla veritabanı birebir örtüşür, kısmi güncelleme riski olmaz).

```csharp
public enum DonemTipi { Aylik, Kumulatif }

public enum SatirTipi
{
    GenelDurum, KategoriGenel, SubeGenel, SubeKategori, UrunGenel, SubeUrun
}

static SatirTipi TipiCoz(double rprId, string rprTip) => rprId switch
{
    1 => SatirTipi.GenelDurum,
    _ => rprTip switch
    {
        "Genel Durum Grup Detaylı" => SatirTipi.KategoriGenel,
        "Şubeler Genel Durum" => SatirTipi.SubeGenel,
        "Şubeler Grup Detaylı" => SatirTipi.SubeKategori,
        "Stoklar Genel" => SatirTipi.UrunGenel,
        "Şubeler Stok Detaylı" => SatirTipi.SubeUrun,
        _ => throw new InvalidOperationException($"Bilinmeyen rpr_tip: '{rprTip}' (rpr_id={rprId})")
    }
};

// NPOI ile ham satırları oku (hem .xls hem .xlsx için çalışır)
static IEnumerable<RawRow> OkuHamSatirlar(Stream dosya)
{
    IWorkbook wb = WorkbookFactory.Create(dosya); // format otomatik algılanır
    ISheet sh = wb.GetSheetAt(0);
    var basliklar = sh.GetRow(0).Cells.Select(c => c.StringCellValue).ToList();

    for (int r = 1; r <= sh.LastRowNum; r++)
    {
        IRow row = sh.GetRow(r);
        if (row == null) continue;
        yield return RawRow.FromExcelRow(row, basliklar); // kolon adı -> değer sözlüğü
    }
}

public async Task ImportEt(Stream dosya, string dosyaAdi)
{
    var hamSatirlar = OkuHamSatirlar(dosya).ToList();

    // --- Geçiş 1: satırları tipe göre ayır ---
    var gruplar = hamSatirlar
        .GroupBy(r => TipiCoz(r.GetDouble("rpr_id"), r.GetString("rpr_tip")))
        .ToDictionary(g => g.Key, g => g.ToList());

    // --- Geçiş 2: SubeUrun satırlarından isim -> kod sözlüğü kur ---
    var isim2kod = gruplar[SatirTipi.SubeUrun]
        .GroupBy(r => r.GetString("Stok İsmi"))
        .ToDictionary(g => g.Key, g => g.First().GetString("Stok Kodu"));

    // --- Dönemi çöz (regex ile tarih aralığı, gün farkına göre tip) ---
    var genelSatir = gruplar[SatirTipi.GenelDurum].Single();
    var (baslangic, bitis) = DonemTarihiCoz(genelSatir.GetString("rpr_tip"));
    var donemTipi = (bitis - baslangic).Days <= 31 ? DonemTipi.Aylik : DonemTipi.Kumulatif;

    using var tx = await _db.Database.BeginTransactionAsync();

    var donem = await _db.Donemler
        .SingleOrDefaultAsync(d => d.Baslangic == baslangic && d.Bitis == bitis && d.Tip == donemTipi)
        ?? _db.Donemler.Add(new Donem { Baslangic = baslangic, Bitis = bitis, Tip = donemTipi }).Entity;

    donem.KaynakDosya = dosyaAdi;
    donem.ImportTarihi = DateTime.UtcNow;
    await _db.SaveChangesAsync(); // DonemId üretilir

    // Var olan dönemse önce eski fact satırlarını temizle (kesinleşen versiyon senaryosu)
    await _db.SubeFireOzetleri.Where(x => x.DonemId == donem.Id).ExecuteDeleteAsync();
    await _db.SubeUrunFireleri.Where(x => x.DonemId == donem.Id).ExecuteDeleteAsync();
    // ... diğer fact tabloları için aynısı

    // --- Boyut tablolarını önbelleğe al, eksikleri toplu ekle ---
    var subeCache = await _db.Subeler.ToDictionaryAsync(s => s.DepoNo);
    var kategoriCache = await _db.Kategoriler.ToDictionaryAsync(k => k.KategoriKodu);
    var urunCache = await _db.Urunler.ToDictionaryAsync(u => u.StokKodu);

    // ör: Şubeler Genel Durum -> SubeFireOzeti
    foreach (var r in gruplar[SatirTipi.SubeGenel])
    {
        int depoNo = (int)r.GetDouble("Depo No");
        if (!subeCache.ContainsKey(depoNo))
            subeCache[depoNo] = (_db.Subeler.Add(new Sube { DepoNo = depoNo, DepoAdi = r.GetString("Depo Adı") })).Entity;

        _db.SubeFireOzetleri.Add(new SubeFireOzeti
        {
            DonemId = donem.Id,
            DepoNo = depoNo,
            FireOrani = r.GetDouble("Fire Oranı"),
            FireTutari = r.GetDouble("Fire Tutarı"),
            KarOrani = r.GetDouble("Kar Oran")
        });
    }

    // Stoklar Genel -> UrunFireOzeti (isim -> kod çözümlemesi burada devreye giriyor)
    foreach (var r in gruplar[SatirTipi.UrunGenel])
    {
        var isim = r.GetString("Stok İsmi");
        if (!isim2kod.TryGetValue(isim, out var stokKodu))
        {
            _logger.LogWarning("Stok Kodu eşleşmedi: {Isim} (dönem {DonemId})", isim, donem.Id);
            continue; // ya da EslesmeyenUrunler kuyruğuna at
        }
        // ... UrunFireOzeti kaydı ekle
    }

    // (SubeKategori, KategoriGenel, SubeUrun için aynı desen tekrarlanır)

    await _db.SaveChangesAsync();
    await tx.CommitAsync();
}
```

## 6. Sıra bilgisiyle yapısal doğrulama

Dosyayı satır sırasına göre test ettim, iki seviyede kaba→ince (büyük parça→küçük parça) deseni var:

**Makro (tüm sayfa):** `rpr_id` kesin artan sırada, her tip tek kesintisiz blok — `1(1 satır) → 2(6) → 3(42) → 4(201) → 5(199) → 7(4259)`. En üstte şirket geneli (en büyük özet), en altta mağaza×kategori×ürün (en küçük atomik kayıt).

**Mikro (detay bloklarının içi):** `Şubeler Grup Detaylı` ve `Şubeler Stok Detaylı` blokları kendi içinde `Kategori Kodu` (dış, 6 kesintisiz blok) → `Depo No` (orta, numerik artan, kesintisiz) → `Stok Kodu` (iç, sırasız) şeklinde iç içe sıralı. Ayrıca doğruladım: bir `Stok Kodu`'nun `Kategori Kodu`'su hep sabit (199 üründen hiçbiri iki kategoriye düşmüyor) — yani Kategori, Ürünün gerçek üst kümesi, tesadüfi bir sıralama değil.

**Kullanım alanları:**
- **Yapısal doğrulama:** Import'ta dosyayı bir kez tarayıp şunu assert et: `rpr_id` blokları kesin artıyor mu, her detay bloğunda `Kategori Kodu` kesintisiz mi, kategori içinde `Depo No` azalmıyor mu. İhlal varsa ERP rapor formatı değişmiş demektir — importu bozuk veriyle sessizce yürütme, hata fırlat.
- **İki geçişli parse'ın nedeni budur:** Bu formatta bir özet satırı her zaman kendi detayından önce (yukarıda) görünüyor — `Stoklar Genel` (rpr_id=5), `Şubeler Stok Detaylı`'dan (rpr_id=7) önce geldiği için Stok Kodu'nu henüz bilmeden karşımıza çıkıyor. İleride yeni bir özet seviyesi eklenirse aynı sorunu bekleyebiliriz.
- **Şema kararı:** `Urun` tablosuna `KategoriKodu` FK'sini doğrudan kolon olarak koy — bu ilişki sabit olduğu doğrulandı, fact satırları üzerinden dolaylı join'e gerek yok.

## 7. Sıradaki adım

Bu iskelet üzerine üç şeyi netleştirmemiz lazım:
- `RawRow` / `DonemTarihiCoz` gibi yardımcı fonksiyonların tam implementasyonu
- Eşleşmeyen ürünler (`isim2kod`'da bulunamayan) için ayrı bir "gözden geçirme" tablosu mu istersin, yoksa import'u o satır için sessizce mi atlayalım?
- Entity sınıflarının tam hâli (nullable alanlar, `Fire Oranı` gibi negatif de olabilen alanlar için doğru tip seçimi — örneklerde `-1.62` gibi negatif fire oranı gördüm, yani `decimal`/`double` olmalı, `uint` gibi bir şey değil)

Hazır olduğunda mazeret motoruna (ortalama hesaplama + eşik tespiti) geçebiliriz.
