# Blazor Server Mimarisinde Race Condition (Yarış Durumları) ve Çözüm Rehberi

Bu belge, **ÖZK Fire Takibi** projesinde ve genel Blazor Server mimarisinde karşılaşılabilecek olası yarış durumlarını (Race Conditions), asenkron yaşam döngüsü çakışmalarını ve bunların önüne geçmek için uygulanması gereken en iyi pratikleri (Best Practices) özetlemektedir.

---

## 1. Blazor Server'da Race Condition Neden Oluşur?

Blazor Server, istemci (tarayıcı) ile sunucu arasında sürekli bir **SignalR WebSocket** bağlantısı üzerinden çalışır. 
* Arayüz olayları (tıklama, sayfa açılışı, input değişimi),
* Tarayıcı API'leri (LocalStorage, SessionStorage, URL okuma),
* Sunucu tarafı veritabanı sorguları ve servis çağrıları

birbirinden farklı hızlarda ve **asenkron (`async/await`)** olarak yürütülür. Zamanlama ve sıralama doğru yönetilmediğinde sistem "yarış durumu"na düşer.

---

## 2. Karşılaşılabilecek Kritik Senaryolar ve Çözüm Kalıpları

### Senaryo 1: Kimlik Doğrulama ve Sayfa Başlatma Çakışması (Auth vs. Initialization)

* **Problem:** 
  Sayfa açılırken `OnInitialized()` metodu senkron çalışırsa, `CustomStateProvider`'ın `ProtectedLocalStorage` üzerinden şifreli oturumu okuyup getirmesi beklenmeden `IsAuthenticated` kontrol edilir. Kullanıcı aslında giriş yapmış olsa bile o milisaniyede `false` göründüğü için yanlışlıkla `/giris` sayfasına yönlendirilir.

* **Hatalı Kod:**
  ```csharp
  // YANLIŞ: Senkron başlatma oturumun asenkron çözülmesini beklemez
  protected override void OnInitialized()
  {
      if (!StateProvider.IsAuthenticated)
      {
          NavigationManager.NavigateTo("/giris");
      }
  }
  ```

* **Doğru Çözüm (Projeye Uygulanan Yaklaşım):**
  `CascadingParameter` ile gelen `Task<AuthenticationState>` nesnesini `OnInitializedAsync()` içerisinde `await` ederek beklemek.
  ```csharp
  // DOĞRU: AuthStateTask tamamlanana kadar bekler
  [CascadingParameter]
  private Task<AuthenticationState>? AuthStateTask { get; set; }

  protected override async Task OnInitializedAsync()
  {
      if (AuthStateTask is not null)
      {
          var authState = await AuthStateTask;
          if (authState.User.Identity?.IsAuthenticated != true)
          {
              NavigationManager.NavigateTo("/giris", replace: true);
          }
      }
  }
  ```

---

### Senaryo 2: Butona Hızlı Çift Tıklama (Double Submit / Çoklu Kayıt)

* **Problem:** 
  Kullanıcı bir fire kaydederken veya form gönderirken "Kaydet" butonuna hızlıca iki veya üç kez tıklarsa, ilk istek veritabanına yazılmadan ikinci istek başlar ve mükerrer (çift) kayıt oluşur.

* **Çözüm Kalıbı:**
  Form ve buton işlemlerinde her zaman bir `isProcessing` (veya `isBusy`) bayrağı kullanmak ve butonun `disabled` durumunu bağlamak:
  ```razor
  <button type="submit" class="button is-primary" disabled="@isProcessing">
      @if (isProcessing)
      {
          <span class="icon"><i class="fas fa-spinner fa-spin"></i></span>
          <span>Kaydediliyor...</span>
      }
      else
      {
          <span>Kaydet</span>
      }
  </button>

  @code {
      private bool isProcessing;

      private async Task SaveAsync()
      {
          if (isProcessing) return; // İkinci tıklamayı anında engeller

          try
          {
              isProcessing = true;
              await FireService.CreateFireRecordAsync(model);
          }
          finally
          {
              isProcessing = false;
          }
      }
  }
  ```

---

### Senaryo 3: Entity Framework `DbContext` Eşzamanlılık Çakışması

* **Problem:** 
  Entity Framework Core'un `AppDbContext` nesnesi **Thread-Safe değildir**. Aynı Scoped `DbContext` üzerinde aynı anda iki farklı `async` sorgu (`ToListAsync()`, `SaveChangesAsync()` vb.) yürütülürse şu hata fırlatılır:
  > *"A second operation was started on this context instance before a previous operation completed."*

* **Hatalı Kod:**
  ```csharp
  // YANLIŞ: Task.WhenAll ile aynı DbContext'e eşzamanlı iki istek atmak
  var task1 = _dbContext.Users.ToListAsync();
  var task2 = _dbContext.FireRecords.ToListAsync();
  await Task.WhenAll(task1, task2); // HATA FIRLATIR!
  ```

* **Doğru Çözüm:**
  Sorguları sırayla `await` etmek:
  ```csharp
  // DOĞRU: Sıralı await kullanımı
  var users = await _dbContext.Users.ToListAsync();
  var records = await _dbContext.FireRecords.ToListAsync();
  ```

---

### Senaryo 4: Canlı Arama / Filtreleme (Debounce & İptal Yönetimi)

* **Problem:** 
  Kullanıcı arama kutusuna "Fıstık" yazarken her tuş vuruşunda (`oninput`) veritabanına istek atılırsa; "Fı" araması "Fıstık" aramasından daha geç sonuç dönebilir. Bu durumda ekranda en son yazılan değil, geç gelen eski sorgunun sonucu görüntülenir.

* **Doğru Çözüm (Debounce + CancellationToken):**
  ```csharp
  @code {
      private CancellationTokenSource? _searchCts;
      private string searchTerm = "";

      private async Task OnSearchInputChanged(ChangeEventArgs e)
      {
          searchTerm = e.Value?.ToString() ?? "";

          // Önceki arama isteğini iptal et
          _searchCts?.Cancel();
          _searchCts?.Dispose();
          _searchCts = new CancellationTokenSource();

          try
          {
              // Kullanıcı yazmayı bitirene kadar 300ms bekle (Debounce)
              await Task.Delay(300, _searchCts.Token);
              
              var results = await FireService.SearchAsync(searchTerm, _searchCts.Token);
              // Sonuçları listele
          }
          catch (TaskCanceledException)
          {
              // Yeni bir tuşa basıldığı için bu arama iptal edildi, normaldir
          }
      }
  }
  ```

---

### Senaryo 5: Sayfa Kapatıldığında Devam Eden Görevler (`ObjectDisposedException`)

* **Problem:** 
  Arka planda çalışan uzun süreli bir async işlem (örn. rapor hesaplama) sürerken kullanıcı başka bir sayfaya geçerse, bileşen bellekten silinir (`Disposed`). İşlem bittiğinde `StateHasChanged()` çağrılırsa `ObjectDisposedException` hatası oluşur.

* **Doğru Çözüm:**
  Bileşende `IDisposable` uygulayarak `CancellationTokenSource` iptalini tetiklemek:
  ```csharp
  @implements IDisposable

  @code {
      private readonly CancellationTokenSource _cts = new();

      protected override async Task OnInitializedAsync()
      {
          try
          {
              await LoadLongRunningReportAsync(_cts.Token);
          }
          catch (OperationCanceledException) { }
      }

      public void Dispose()
      {
          _cts.Cancel();
          _cts.Dispose();
      }
  }
  ```

---

## 3. Geliştirici Kontrol Listesi (Checklist)

Yeni bir sayfa veya servis geliştirirken şu 4 altın kuralı kontrol ediniz:

1. [ ] **Korumalı Sayfalar:** Korumalı sayfalarda doğrudan `@inherits AuthRequiredComponent` kullanıldı mı?
2. [ ] **Butonlar ve Formlar:** Kaydet/Güncelle/Sil işlemlerinde çift tıklamayı önleyen `isProcessing` bayrağı eklendi mi?
3. [ ] **Veritabanı Çağrıları:** Aynı `DbContext` üzerinde `Task.WhenAll` yerine sıralı `await` kullanıldı mı?
4. [ ] **Canlı Arama Alanları:** Canlı filtreleme ve input olaylarında gecikme (Debounce) ve `CancellationToken` uygulandı mı?
