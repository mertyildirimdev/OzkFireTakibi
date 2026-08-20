# Case study: bir Excel dosyasının uçtan uca işlenişi

Senaryo: `AÇIK_KURUYEMİŞ...NİSAN_2026...KESİNLEŞEN_ŞEKLİ.xls` dosyası sisteme yükleniyor. Dosyanın gerçek içeriğini kullanarak, tek bir zinciri (KAYISDAGI mağazası → KURUYEMISLER ACIK kategorisi → kabuklu ceviz ürünü) baştan sona takip ediyoruz. Tüm rakamlar dosyadan — uydurma örnek yok.

## Aşama 1 — Ham okuma

NPOI dosyayı açıyor, ilk satırı başlık olarak alıyor, kalan **4709 satırı** belleğe okuyor (33 kolon × 4709 satır, ~2MB — bu hacimde stream'e gerek yok, tamamını `List<RawRow>`'a almak sorun değil). Her satır kolon adı → değer sözlüğüne çevriliyor.

## Aşama 2 — Satır sınıflandırma

Her satır `rpr_id`/`rpr_tip`'e göre 6 gruba ayrılıyor:

| Grup | Satır sayısı | Örnek satırımız burada mı var? |
|---|---|---|
| Genel Durum | 1 | ✓ (şirket geneli) |
| Genel Durum Grup Detaylı | 6 | ✓ (KURUYEMISLER ACIK satırı) |
| Şubeler Genel Durum | 42 | ✓ (KAYISDAGI satırı) |
| Şubeler Grup Detaylı | 201 | ✓ (KAYISDAGI × KURUYEMISLER ACIK) |
| Stoklar Genel | 199 | ✓ (kabuklu ceviz, şirket geneli) |
| Şubeler Stok Detaylı | 4259 | ✓ (KAYISDAGI × KURUYEMISLER ACIK × kabuklu ceviz) |

Bu altı grubun her birinde, seçtiğimiz zincirle ilgili tam olarak bir satır var — birazdan hepsini tek tek göreceğiz.

## Aşama 3 — Dönem çözümleme

`Genel Durum` grubundaki tek satırın `rpr_tip` metni: `"Genel Durum 28.03.2026-28.04.2026"`. Regex ile ayrıştırılıyor:

```
Baslangic = 28.03.2026
Bitis     = 28.04.2026
Fark      = 31 gün  →  DonemTipi = Aylık
```

(Karşılaştırma için 4 aylık dosyada bu metin `"07.01.2026-28.04.2026"` idi, fark 111 gün → Kümülatif. Aynı regex, aynı eşik kuralı, iki dosyada da doğru çalışıyor.)

`Donem` tablosunda `(28.03.2026, 28.04.2026, Aylık)` anahtarıyla kayıt aranıyor. Bu dosya adında "KESİNLEŞEN" geçtiği için muhtemelen bu dönem daha önce bir taslak olarak yüklenmişti — eşleşme bulunuyor, mevcut `DonemId` kullanılıyor (yeni satır açılmıyor).

## Aşama 4 — İlk geçiş: isim → kod sözlüğü

`Şubeler Stok Detaylı` grubundaki 4259 satır taranıyor, `Stok İsmi → Stok Kodu` sözlüğü kuruluyor. Bizim ürünümüz için:

```
"K.YEMIS KABUKLU CEVIZ KG"  →  "02.007.001.00150"
```

Bu sözlük 5. aşamada `Stoklar Genel` satırlarındaki eksik kodu çözmek için kullanılacak.

## Aşama 5 — Eski verinin temizlenmesi

Transaction açılıyor. `DonemId` daha önce var olduğu için (kesinleşen versiyon senaryosu), o `DonemId`'ye bağlı **tüm fact tabloları** (`SubeFireOzeti`, `SubeKategoriFire`, `SubeUrunFire`, `KategoriFireOzeti`, `UrunFireOzeti`) siliniyor. Taslak dönemin eski, kesinleşmemiş rakamları artık yok — yerine az sonra kesinleşen rakamlar yazılacak.

## Aşama 6 — Master data upsert

Zincirimizdeki üç boyut (dimension) kontrol ediliyor:

| Varlık | Anahtar | Zaten var mı? |
|---|---|---|
| `Sube` | DepoNo=1 | Var — "KAYISDAGI" (isim değişmemiş) |
| `Kategori` | 15.001.0001 | Var — "KURUYEMISLER ACIK" |
| `Urun` | 02.007.001.00150 | Var — "K.YEMIS KABUKLU CEVIZ KG", KategoriKodu=15.001.0001 |

Üçü de daha önceki bir importtan zaten mevcut, yeni insert gerekmiyor — sadece cache'den okunuyor. (İlk kez görülen bir mağaza/kategori/ürün olsaydı burada insert edilirdi.)

## Aşama 7 — Fact satırlarının yazılması: aynı zincir, altı farklı bakış açısı

Şimdi asıl ilginç kısım. Aynı "KAYISDAGI mağazasındaki kabuklu ceviz" gerçeği, agregasyon seviyesine göre **çok farklı bir Fire Oranı gösteriyor**:

| Seviye | Kapsam | Fire Oranı | Fire Tutarı |
|---|---|---|---|
| `GenelDurum` | Tüm şirket, tüm ürünler | **-0.72** | -377.247,31 ₺ |
| `KategoriFireOzeti` | Tüm şirket, KURUYEMISLER ACIK | **+0.40** | +145.178,17 ₺ |
| `SubeFireOzeti` | KAYISDAGI, tüm ürünler | **-3.10** | -12.070,96 ₺ |
| `SubeKategoriFire` | KAYISDAGI, KURUYEMISLER ACIK | **-0.31** | -620,71 ₺ |
| `UrunFireOzeti` | Tüm şirket, sadece kabuklu ceviz | **+0.03** | +553,66 ₺ |
| `SubeUrunFire` | KAYISDAGI, kabuklu ceviz | **-1.62** | -439,62 ₺ |

Bunun neden önemli olduğu şu: şirket geneli KURUYEMISLER ACIK kategorisi pozitif (+0.40, yani kayıp yok, fazlalık var), kabuklu ceviz ürünü de şirket genelinde neredeyse nötr (+0.03). Ama **KAYISDAGI mağazasının genel fire oranı -3.10 ile şirket ortalamasının (-0.72) belirgin şekilde altında** — ve bu mağazadaki kabuklu ceviz özelinde -1.62. Sadece `SubeFireOzeti` seviyesine bakan bir mazeret motoru "KAYISDAGI kötü" der ama *neden* kötü olduğunu göstermez; `SubeUrunFire` seviyesine indiğinde bu mağazadaki hangi ürünlerin ortalamayı aşağı çektiğini görebilirsin. Mazeret talebini otomatik oluştururken muhtemelen mağaza müdürüne "genel fire oranın düşük" demek yerine "bu ürünlerde özellikle düşüksün" diye somut liste vermek istersin — bunun için `SubeUrunFire` tablosu lazım.

*(Not: `Fire Oranı`'nın negatif olması "kayıp", pozitifin ne anlama geldiği — muhtemelen sayım fazlası veya ölçüm sapması — ortalama/eşik motorunu tasarlarken netleştirmemiz gereken bir işaret kuralı. 42 mağazanın Fire Oranı ortalaması -2.00, standart sapması 2.51 — yani mağazalar arası fark oldukça büyük, "ortalamanın üstü/altı" tanımını dikkatli kurmamız lazım. Bu, bir sonraki aşamanın konusu.)*

## Aşama 8 — Commit

Tüm satırlar (bizim zincirimiz dahil, geri kalan ~4700 satır da aynı akıştan geçerek) fact tablolarına ekleniyor, `SaveChangesAsync()` + `CommitAsync()` çağrılıyor. Transaction başarılıysa dönem artık "kesinleşen" veriyle güncel; başarısızsa hiçbir şey yazılmamış olur (ya hep ya hiç — yarım kalmış bir import veritabanında tutarsızlık bırakmaz).

## Özet: uçtan uca akış

```
Ham .xls (4709 satır)
   → NPOI okuma
   → rpr_id'ye göre 6 gruba ayırma
   → Genel Durum satırından dönem çözümleme (regex + gün farkı)
   → Şubeler Stok Detaylı'dan isim→kod sözlüğü (Stoklar Genel'i çözmek için)
   → Donem upsert, varsa eski fact satırlarını sil
   → Sube/Kategori/Urun master data cache + eksikleri insert
   → 6 fact tablosuna satırları yaz (aynı zincir, 6 farklı agregasyon seviyesi)
   → transaction commit
```

Bir sonraki adım: `SubeFireOzeti` ve `SubeUrunFire` tablolarındaki bu rakamlardan ortalama/eşik hesaplayıp otomatik mazeret talebi üretecek motor.
