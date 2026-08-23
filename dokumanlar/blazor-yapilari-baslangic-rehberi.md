# ÖZK Fire Takibi Projesinde Kullanılan Blazor Yapıları

## Başlangıç Rehberi

Bu doküman, Blazor hakkında daha önce hiç bilgi sahibi olmayan bir geliştiriciye **ÖZK Fire Takibi** projesindeki Blazor yapılarını tanıtmak amacıyla hazırlanmıştır.

Dokümanın hedefi yalnızca Blazor kavramlarını teorik olarak açıklamak değil, her kavramın bu projede nerede ve nasıl kullanıldığını göstermektir.

---

## 1. Blazor Nedir?

Geleneksel bir web uygulamasında genellikle şu teknolojiler birlikte kullanılır:

- **HTML:** Sayfanın yapısını oluşturur.
- **CSS:** Sayfanın görünümünü belirler.
- **JavaScript:** Buton tıklaması, form gönderme ve dinamik ekran değişiklikleri gibi işlemleri yönetir.
- **Backend:** Veritabanı erişimini ve iş kurallarını yürütür.

Blazor, ekranın dinamik davranışlarının büyük bölümünü JavaScript yerine **C# ile** geliştirmemizi sağlar.

Basit bir Blazor bileşeni şu şekilde görünebilir:

```razor
<h1>Merhaba @UserName</h1>

<button @onclick="SayaciArtir">Artır</button>

<p>Sayaç: @sayac</p>

@code {
    private int sayac;

    private void SayaciArtir()
    {
        sayac++;
    }
}
```

Bu örnekte:

- HTML etiketleri ekranın görünümünü oluşturur.
- `@UserName` ve `@sayac`, C# değerlerini HTML içinde gösterir.
- `@onclick`, buton tıklamasını bir C# metoduna bağlar.
- `@code` bölümü bileşenin C# alanlarını ve metotlarını içerir.

Blazor, `.razor` dosyasını derlerken arka planda bir C# sınıfına dönüştürür. Bu nedenle her `.razor` dosyası aslında bir **Blazor bileşenidir**.

---

## 2. Bu Proje Hangi Blazor Modelini Kullanıyor?

Bu proje modern bir **Blazor Web App** projesidir ve arayüzde **Interactive Server** render modunu kullanır.

İlgili kayıtlar [`Program.cs`](../OzkFireTakibiClient/Program.cs) içinde bulunur:

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
```

Uygulamanın kök bileşeni de Interactive Server moduyla eşlenir:

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
```

Interactive Server modelinde:

- Kullanıcının gördüğü HTML tarayıcıda bulunur.
- Bileşenlerin C# kodu ağırlıklı olarak sunucuda çalışır.
- Tarayıcı ile sunucu arasında Blazor bağlantısı kurulur.
- Kullanıcının tıklama ve input olayları sunucuya iletilir.
- C# kodu çalıştıktan sonra yalnızca değişen ekran parçaları tarayıcıya gönderilir.

Bağlantıda genellikle SignalR ve mümkün olduğunda WebSocket kullanılır. WebSocket kullanılamayan ortamlarda uygun bağlantı alternatifleri devreye girebilir.

### Basit etkileşim akışı

```text
Kullanıcı butona tıklar
        ↓
Tarayıcı olayı Blazor bağlantısıyla sunucuya gönderir
        ↓
Razor bileşenindeki C# metodu çalışır
        ↓
Gerekirse iş servisi ve veritabanı çağrılır
        ↓
Bileşenin C# alanları güncellenir
        ↓
Blazor görünümü yeniden hesaplar
        ↓
Yalnızca değişen HTML bölümü tarayıcıya gönderilir
```

Bu projede klasik MVC Controller veya ayrıca hazırlanmış bir Web API katmanı kullanılmıyor. Blazor bileşenleri, uygulamanın C# servislerini doğrudan çağırıyor.

---

## 3. Projenin Genel Yapısı

Blazor tarafındaki temel akış şöyledir:

```text
Program.cs
   ↓
App.razor
   ↓
Routes.razor
   ↓
MainLayout veya EmptyLayout
   ↓
Sayfa bileşeni
   ↓
LoginService / ReportImportService / ExcuseService
   ↓
Entity Framework Core
   ↓
SQL Server
```

Bu katmanların görevleri:

| Katman | Görevi |
|---|---|
| `Program.cs` | Blazor, veritabanı, servis, kimlik doğrulama ve HTTP altyapısını kurar. |
| `App.razor` | Ana HTML belgesini ve uygulamanın kök bileşenlerini oluşturur. |
| `Routes.razor` | URL ile açılacak sayfayı eşleştirir. |
| Layout bileşenleri | Sayfaların çevresindeki ortak görünümü oluşturur. |
| Page bileşenleri | Kullanıcının gördüğü ekranları ve ekran durumunu yönetir. |
| İş servisleri | Rapor, mazeret ve giriş işlemlerini gerçekleştirir. |
| Entity Framework Core | SQL Server ile iletişim kurar. |

---

## 4. Uygulamanın Başlangıç Noktası: Program.cs

[`Program.cs`](../OzkFireTakibiClient/Program.cs), uygulamanın başlangıç ve yapılandırma dosyasıdır.

Dosyada iki temel işlem yapılır:

1. Uygulamanın kullanacağı servisler Dependency Injection sistemine kaydedilir.
2. HTTP istek zinciri ve Blazor giriş noktası yapılandırılır.

### 4.1. Razor bileşen servisleri

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
```

`AddRazorComponents()` uygulamaya Razor bileşen altyapısını ekler.

`AddInteractiveServerComponents()` ise `@onclick`, `@bind`, form gönderme ve benzeri etkileşimlerin sunucuda çalışmasını sağlar.

### 4.2. Blazor kök bileşeni

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
```

Bu tanım uygulamanın kök bileşeninin `App` olduğunu belirtir. Blazor bileşen ağacı [`App.razor`](../OzkFireTakibiClient/Src/Components/App.razor) dosyasından başlar.

### 4.3. Dependency Injection

Projede kullanılan servisler `Program.cs` ve [`ServiceCollectionExtensions.cs`](../OzkFireTakibiClient/Src/Services/ServiceCollectionExtensions.cs) içinde kaydedilir.

Örneğin:

```csharp
builder.Services.AddBaseServices();
builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomStateProvider>();
```

Bu kayıt sayesinde bir Razor bileşeni ihtiyaç duyduğu servisi şu şekilde isteyebilir:

```razor
@inject LoginService LoginService
```

Blazor ve ASP.NET Core, `LoginService` nesnesinin oluşturulmasından ve bileşene verilmesinden sorumludur. Bileşenin elle `new LoginService(...)` yazması gerekmez.

### 4.4. Servis yaşam süreleri

Projede başlıca iki servis yaşam süresi kullanılır:

- **Singleton:** Uygulama boyunca tek örnek yaşar. Durum tutmayan `ReportImportParser` bu şekilde kaydedilmiştir.
- **Scoped:** Belirli bir kullanıcı bağlantısı/kapsam boyunca yaşar. Rapor, mazeret ve giriş servisleri bu şekilde kaydedilmiştir.

Interactive Server modelinde uzun süre aynı `DbContext` örneğini kullanmak eşzamanlılık sorunlarına yol açabilir. Proje bu nedenle `IDbContextFactory<AppDbContext>` kullanır. Örneğin [`ReportImportService`](../OzkFireTakibiClient/Src/Services/ReportImportService.cs), her işlem için yeni bir `AppDbContext` oluşturur.

### 4.5. Middleware ve yardımcı ASP.NET Core yapıları

`Program.cs` içinde aşağıdaki yapılar da bulunur:

- Üretim ortamı hata sayfası
- HSTS
- HTTPS yönlendirmesi
- Antiforgery koruması
- Statik dosya eşleme
- 404 sayfası yönlendirmesi

Bunlar doğrudan Blazor bileşeni değildir; Blazor uygulamasını barındıran ASP.NET Core altyapısının parçalarıdır.

---

## 5. Ana HTML Belgesi: App.razor

[`App.razor`](../OzkFireTakibiClient/Src/Components/App.razor), uygulamanın ana HTML kabuğudur.

Bu dosyada gerçek `<html>`, `<head>` ve `<body>` etiketleri bulunur.

### 5.1. CSS ve ikon kaynakları

```razor
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bulma@1.0.4/css/bulma.min.css">
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css" />
<link rel="stylesheet" href="@Assets["app.css"]" />
<link rel="stylesheet" href="@Assets["OzkFireTakibiClient.styles.css"]" />
```

Projede:

- Arayüz tasarımı için **Bulma CSS**,
- İkonlar için **Font Awesome**,
- Genel uygulama stilleri için `wwwroot/app.css`,
- Bileşenlere özel `.razor.css` dosyaları için oluşturulan birleşik CSS

kullanılır.

`@Assets[...]`, statik dosyanın doğru adresini çözmek için kullanılan modern Blazor yapısıdır.

### 5.2. HeadOutlet ve PageTitle

```razor
<HeadOutlet @rendermode="new InteractiveServerRenderMode(prerender: false)" />
```

Sayfalardaki şu kullanım tarayıcı sekmesinin başlığını belirler:

```razor
<PageTitle>Raporlar</PageTitle>
```

`PageTitle` başlığı değiştirmek ister, `HeadOutlet` ise bu değişikliği gerçek `<head>` bölümüne uygular.

### 5.3. Routes bileşeni

```razor
<Routes @rendermode="new InteractiveServerRenderMode(prerender: false)" />
```

Uygulamanın sayfa yönlendirme sistemi burada başlatılır.

`prerender: false`, bileşenlerin önce statik HTML olarak oluşturulup ardından yeniden interaktif hale getirilmesi yerine doğrudan interaktif bağlantıyla başlamasını sağlar. Proje tarayıcı depolamasına bağlı bir oturum yapısı kullandığı için bu tercih özellikle önemlidir.

### 5.4. Blazor JavaScript altyapısı

```razor
<script src="@Assets["_framework/blazor.web.js"]"></script>
```

Bu dosya:

- Tarayıcı ile sunucu arasındaki Blazor bağlantısını kurar.
- Tıklama ve input olaylarını sunucuya iletir.
- Sunucudan gelen görünüm değişikliklerini tarayıcıya uygular.
- Bağlantı kopması ve yeniden bağlanma süreçlerini destekler.

Uygulamanın rapor veya mazeret iş mantığı bu JavaScript dosyasında değildir. Dosya, Blazor’un çalışma altyapısıdır.

---

## 6. Razor Bileşeninin Anatomisi

Bu projedeki bir sayfa genel olarak aşağıdaki bölümlerden oluşur:

```razor
@page "/ornek"
@layout MainLayout
@inherits AuthRequiredComponent
@implements IAsyncDisposable
@inject OrnekService OrnekService

<PageTitle>Örnek Sayfa</PageTitle>

<h1>@baslik</h1>
<button @onclick="IslemAsync">İşlem yap</button>

@code {
    private string baslik = "Örnek";

    private async Task IslemAsync()
    {
        await OrnekService.IslemYapAsync();
    }
}
```

Direktiflerin anlamları:

| Direktif | Anlamı |
|---|---|
| `@page` | Bileşeni bir URL’ye bağlar. |
| `@layout` | Bileşenin hangi ortak sayfa çerçevesinde gösterileceğini belirler. |
| `@inherits` | Bileşenin hangi temel C# sınıfından türeyeceğini belirler. |
| `@implements` | Bileşenin uyguladığı C# arayüzünü belirtir. |
| `@inject` | Dependency Injection üzerinden servis alır. |
| `@using` | C# namespace’ini dosyaya dahil eder. |
| `@code` | Bileşenin C# alanlarını, özelliklerini ve metotlarını içerir. |

Proje genelinde ortak kullanılan `@using` ifadeleri [`_Imports.razor`](../OzkFireTakibiClient/Src/Components/_Imports.razor) içinde toplanmıştır. Böylece her `.razor` dosyasında aynı namespace tanımlarını tekrar yazmak gerekmez.

---

## 7. Routing: URL Hangi Sayfayı Açacak?

Routing işlemleri [`Routes.razor`](../OzkFireTakibiClient/Src/Components/Routes.razor) içinde yönetilir.

```razor
<Router AppAssembly="typeof(Program).Assembly"
        NotFoundPage="typeof(Pages.NotFound)">
```

`Router`, uygulamadaki `@page` ifadelerini bulur ve mevcut URL’ye uygun bileşeni açar.

### 7.1. Sabit rotalar

[`Raporlar.razor`](../OzkFireTakibiClient/Src/Components/Pages/Raporlar.razor) içinde:

```razor
@page "/raporlar"
```

Kullanıcı `/raporlar` adresine gittiğinde `Raporlar.razor` bileşeni açılır.

### 7.2. Parametreli rotalar

[`MazeretDetay.razor`](../OzkFireTakibiClient/Src/Components/Pages/MazeretDetay.razor) içinde:

```razor
@page "/mazeretler/{ExcuseId:long}"
```

URL şu şekildeyse:

```text
/mazeretler/42
```

Blazor `42` değerini aşağıdaki özelliğe aktarır:

```csharp
[Parameter]
public long ExcuseId { get; set; }
```

`:long`, rota değerinin tam sayı olması gerektiğini belirtir.

Rapor detayında da aynı yapı kullanılır:

```razor
@page "/raporlar/{ImportId:long}"
```

```csharp
[Parameter]
public long ImportId { get; set; }
```

### 7.3. AuthorizeRouteView

`Routes.razor` içinde normal `RouteView` yerine şu yapı kullanılır:

```razor
<AuthorizeRouteView RouteData="routeData"
                    DefaultLayout="typeof(Layout.MainLayout)">
```

Bu yapı hem sayfayı açar hem de sayfada tanımlanmış yetkilendirme kurallarını değerlendirir.

Yetkisiz kullanıcının iki olası durumu vardır:

- Kullanıcı giriş yapmış fakat rolü yeterli değilse yetkisizlik mesajı gösterilir.
- Kullanıcı giriş yapmamışsa [`RedirectToLogin.razor`](../OzkFireTakibiClient/Src/Components/Controls/RedirectToLogin.razor) çalışır.

Mevcut korumalı sayfaların çoğunda giriş kontrolü ayrıca `AuthRequiredComponent` mirası üzerinden yapılmaktadır. `AuthorizeRouteView` özellikle `[Authorize]` veya policy niteliği verilmiş sayfalar için genel yönlendirme altyapısıdır.

### 7.4. FocusOnNavigate

```razor
<FocusOnNavigate RouteData="routeData" Selector="h1" />
```

Sayfa değiştiğinde ilk `<h1>` etiketine odaklanır. Bu, klavye ve ekran okuyucu kullanan kişiler için erişilebilirlik desteğidir.

### 7.5. NavigationManager

Blazor içinde sayfa yönlendirmek için `NavigationManager` kullanılır:

```csharp
NavigationManager.NavigateTo("/giris", replace: true);
```

`replace: true`, mevcut adresin tarayıcı geçmişinde yeni bir kayıt oluşturmak yerine değiştirilmesini sağlar. Kullanıcının geri tuşuyla tekrar yetkisiz sayfaya dönmesini önlemek için giriş yönlendirmelerinde yararlıdır.

---

## 8. Layout Sistemi

Layout, sayfaların çevresindeki ortak görünüm çerçevesidir.

Projede iki layout bulunur:

- [`MainLayout.razor`](../OzkFireTakibiClient/Src/Components/Layout/MainLayout.razor)
- [`EmptyLayout.razor`](../OzkFireTakibiClient/Src/Components/Layout/EmptyLayout.razor)

### 8.1. MainLayout

```razor
@inherits LayoutComponentBase
```

Bu ifade bileşenin bir layout olduğunu belirtir.

`MainLayout` aşağıdaki ortak bölümleri içerir:

- Uygulama logosu
- Navbar
- Menü bağlantıları
- Kullanıcı adı ve rolü
- Giriş/çıkış butonu
- Sayfa içerik alanı
- Genel Blazor hata bildirimi

Layout içindeki en önemli ifade şudur:

```razor
@Body
```

Örneğin `Raporlar.razor` açıldığında sayfanın ürettiği içerik `@Body` konumuna yerleştirilir. Navbar ve dış çerçeve ise `MainLayout` tarafından oluşturulur.

### 8.2. EmptyLayout

Giriş sayfasında navbar görünmemesi için şu tanım kullanılır:

```razor
@layout EmptyLayout
```

`EmptyLayout` yalnızca `@Body` gösterir. Bu nedenle giriş ekranı bağımsız, tam ekran bir tasarıma sahip olabilir.

---

## 9. Tekrar Kullanılabilir Bileşenler

Blazor’da her `.razor` dosyası bir bileşendir. Bileşen tam ekran bir sayfa olmak zorunda değildir.

Örneğin [`NavbarItems.razor`](../OzkFireTakibiClient/Src/Components/Controls/NavbarItems.razor), navbar bağlantılarını oluşturan küçük bir bileşendir.

`MainLayout` içinde şu şekilde çağrılır:

```razor
<NavbarItems OnNavigate="CloseNavbarMenu" />
```

Buradaki `OnNavigate`, alt bileşene gönderilen bir parametredir:

```csharp
[Parameter]
public EventCallback OnNavigate { get; set; }
```

Bağlantıya tıklanınca alt bileşen şunu çağırır:

```csharp
await OnNavigate.InvokeAsync();
```

Akış şöyledir:

1. `MainLayout`, `NavbarItems` bileşenini oluşturur.
2. Kendi `CloseNavbarMenu` metodunu alt bileşene verir.
3. Alt bileşendeki bağlantıya tıklanır.
4. Alt bileşen `EventCallback` üzerinden üst bileşene haber verir.
5. Ana bileşen mobil navbar menüsünü kapatır.

Bu yapı, bileşenler arasında kontrollü iletişim kurulmasını sağlar.

---

## 10. Razor Sözdizimi

### 10.1. C# değerini ekrana yazma

```razor
<h1>Hoş Geldiniz, @UserName</h1>
```

`@` işareti HTML dünyasından C# dünyasına geçiş yapar.

### 10.2. Koşullu görünüm

```razor
@if (isLoading)
{
    <progress class="progress">Yükleniyor</progress>
}
else
{
    <table>...</table>
}
```

`isLoading` değiştiğinde Blazor bu bölümü yeniden değerlendirir.

Projede koşullu görünüm şu işler için sık kullanılır:

- Yükleniyor göstergesi
- Başarı veya hata mesajı
- Boş liste mesajı
- Yetkili kullanıcı işlemleri
- Modal pencere
- Formların gösterilmesi veya gizlenmesi

### 10.3. Liste oluşturma

```razor
@foreach (var item in history)
{
    <tr>
        <td>@item.OriginalFileName</td>
    </tr>
}
```

Servisten gelen her rapor için bir tablo satırı oluşturulur.

### 10.4. Dinamik HTML özelliği

```razor
<button disabled="@isProcessing">
```

`isProcessing` değeri `true` ise buton devre dışı kalır.

### 10.5. Dinamik CSS sınıfı

```razor
class="button @(isProcessing ? "is-loading" : "")"
```

İşlem devam ederken butona Bulma’nın `is-loading` sınıfı eklenir.

---

## 11. Event Handling: Kullanıcı Etkileşimleri

Normal JavaScript’te event listener kullanılan yerlerde Blazor Razor event ifadeleri kullanılabilir.

### 11.1. Buton tıklaması

```razor
<button @onclick="Logout">Çıkış Yap</button>
```

Kullanıcı tıkladığında C# metodu çalışır:

```csharp
private async Task Logout()
{
    await LoginService.LogoutAsync();
}
```

### 11.2. Parametreli event

```razor
<button @onclick="() => RequestDelete(item)">Sil</button>
```

Lambda ifadesi sayesinde mevcut `item` metoda parametre olarak gönderilir.

### 11.3. Değer değişimi

```razor
<select @onchange="ChangeStatusAsync">
```

Event metodu `ChangeEventArgs` üzerinden yeni değeri okuyabilir.

### 11.4. Klavye olayı

[`Mazeretler.razor`](../OzkFireTakibiClient/Src/Components/Pages/Mazeretler.razor) içinde:

```razor
<input @onkeydown="HandleSearchKeyAsync" />
```

Metot Enter tuşunu kontrol eder:

```csharp
private async Task HandleSearchKeyAsync(KeyboardEventArgs args)
{
    if (args.Key == "Enter")
    {
        await ApplyFiltersAsync();
    }
}
```

Blazor event metodu tamamlandıktan sonra bileşeni otomatik olarak yeniden render eder. Bu nedenle projede çoğu yerde elle `StateHasChanged()` çağrılmaması normaldir.

---

## 12. Veri Bağlama: @bind

`@bind`, ekrandaki form elemanı ile C# alanını birbirine bağlar.

```razor
<input @bind="searchText" />
```

Kullanıcı input değerini değiştirince `searchText` güncellenir. C# kodu `searchText` değerini değiştirirse input da güncellenir. Buna **iki yönlü veri bağlama** denir.

### 12.1. Blazor input bileşenleri

```razor
<InputText @bind-Value="loginModel.Email" />
<InputCheckbox @bind-Value="loginModel.RememberMe" />
```

`InputText` ve `InputCheckbox`, Blazor form ve doğrulama sistemiyle birlikte çalışan hazır bileşenlerdir.

### 12.2. onchange ve oninput farkı

```razor
<input @bind="searchText" @bind:event="oninput" />
```

Varsayılan bağlama çoğunlukla `onchange` olayını kullanır. Değer, kullanıcı inputtan çıktığında güncellenir.

`oninput` kullanılırsa her karakter yazıldığında C# alanı güncellenir.

[`MazeretMagazalari.razor`](../OzkFireTakibiClient/Src/Components/Pages/MazeretMagazalari.razor) içindeki arama alanı bu şekilde çalışır. Her tuşta veritabanına sorgu gönderilmez; `FilteredStores` özelliği bellekteki mağaza listesini filtreler.

---

## 13. Formlar ve Doğrulama

[`Giris.razor`](../OzkFireTakibiClient/Src/Components/Pages/Giris.razor), Blazor form sisteminin en açık örneğidir.

```razor
<EditForm Model="@loginModel"
          OnValidSubmit="@HandleValidSubmit"
          FormName="LoginForm">
```

Buradaki özellikler:

- `Model`: Formun bağlı olduğu C# nesnesidir.
- `OnValidSubmit`: Form doğrulamayı geçerse çalışacak metottur.
- `FormName`: Formun ayırt edici adıdır.

Doğrulama altyapısı:

```razor
<DataAnnotationsValidator />
<ValidationMessage For="@(() => loginModel.Email)" />
```

Kurallar C# modelinde tanımlanır:

```csharp
[Required(ErrorMessage = "E-posta adresi gereklidir.")]
[EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
public string Email { get; set; } = string.Empty;
```

Kullanıcı geçersiz e-posta girerse:

- Hata mesajı gösterilir.
- `HandleValidSubmit` çalıştırılmaz.
- Servise gereksiz giriş isteği gönderilmez.

`[SupplyParameterFromForm]`, form verisinin `loginModel` özelliğine sağlanabilmesini destekleyen Blazor form yapısıdır.

---

## 14. Kimlik Doğrulama ve Oturum Yönetimi

Projede özel bir [`CustomStateProvider`](../OzkFireTakibiClient/Src/CustomStateProvider.cs) kullanılır.

Bu sınıf `AuthenticationStateProvider` sınıfından türemiştir ve Blazor’a o anda hangi kullanıcının giriş yaptığını bildirir.

### 14.1. Giriş akışı

```text
Kullanıcı giriş formunu gönderir
        ↓
Giris.razor → HandleValidSubmit()
        ↓
LoginService.LoginAsync()
        ↓
UserService kullanıcıyı doğrular
        ↓
CustomStateProvider.MarkUserAsAuthenticatedAsync()
        ↓
Kullanıcı için ClaimsPrincipal oluşturulur
        ↓
AuthenticationState değişikliği bütün bileşenlere bildirilir
        ↓
Routes.razor kullanıcıyı hedef sayfaya yönlendirir
```

### 14.2. Claims

Giriş yapan kullanıcı için aşağıdaki bilgiler claim olarak hazırlanır:

- Kullanıcı ID’si
- Adı
- E-posta adresi
- Rolü
- Varsa mağaza/depo adı
- Varsa mağaza/depo numarası

Rol kontrolü şu şekilde yapılabilir:

```csharp
currentUser.IsInRole("Admin")
```

### 14.3. Beni hatırla

Kullanıcı “Beni Hatırla” seçeneğini işaretlerse kullanıcı ID’si ve oturum son kullanma tarihi `ProtectedLocalStorage` içinde saklanır.

`ProtectedLocalStorage`, tarayıcıdaki `localStorage` verisini ASP.NET Core Data Protection altyapısıyla korur.

Tarayıcı yeniden açıldığında `CustomStateProvider`, saklanan oturumu okumaya ve kullanıcıyı veritabanından yeniden yüklemeye çalışır.

### 14.4. CascadingAuthenticationState

`Program.cs` içinde:

```csharp
builder.Services.AddCascadingAuthenticationState();
```

Bu yapı kimlik bilgisinin bileşen ağacının aşağısına otomatik aktarılmasını sağlar. Her alt bileşene kullanıcıyı tek tek parametre olarak göndermek gerekmez.

[`AuthRequiredComponent`](../OzkFireTakibiClient/Src/Components/Controls/AuthRequiredComponent.cs) bu bilgiyi şu şekilde alır:

```csharp
[CascadingParameter]
private Task<AuthenticationState>? AuthStateTask { get; set; }
```

`CascadingParameter`, bir değerin doğrudan ebeveynden değil, bileşen ağacının üst kısmından bütün alt bileşenlere aktarılmasını sağlar.

### 14.5. AuthRequiredComponent

Korumalı sayfalarda şu kullanım vardır:

```razor
@inherits AuthRequiredComponent
```

Bu temel sınıf:

- Kullanıcının giriş durumunun yüklenmesini bekler.
- Kullanıcı giriş yapmamışsa `/giris` sayfasına yönlendirir.
- `CurrentUser`, `UserName`, `UserRole` ve `IsAuthenticated` gibi ortak özellikler sağlar.
- Giriş sayfasına yönlendirirken mevcut adresi `returnUrl` olarak ekler.

Bu nedenle [`Anasayfa.razor`](../OzkFireTakibiClient/Src/Components/Pages/Anasayfa.razor) içinde doğrudan şunlar kullanılabilir:

```razor
@UserName
@UserRole
```

### 14.6. AuthorizeView

`AuthorizeView`, içeriği kullanıcının giriş veya yetki durumuna göre gösterir.

```razor
<AuthorizeView Policy="@ReportPolicies.CanImportReports">
    <Authorized>
        <!-- Rapor yükleme alanı -->
    </Authorized>
    <NotAuthorized>
        <!-- Yetki açıklaması -->
    </NotAuthorized>
</AuthorizeView>
```

`Program.cs` içindeki politikalar:

| Politika | İzin verilen roller |
|---|---|
| Rapor yükleme | Admin, Moderator |
| Rapor silme | Admin |
| Mazeret değerlendirme | Admin, Moderator |
| Mazeret mağaza yönetimi | Admin |

`AuthorizeView` butonu veya içeriği gizler. Ancak gerçek güvenlik yalnızca arayüzde buton gizlemeye dayanmamalıdır. Projedeki iş servisleri de `IAuthorizationService` veya kullanıcı rolü üzerinden yetki kontrolü yapar.

---

## 15. Bileşen Yaşam Döngüsü

Blazor bileşenlerinin belirli yaşam döngüsü metotları vardır.

### 15.1. OnInitialized

```csharp
protected override void OnInitialized()
{
    loginModel ??= new();
}
```

Bileşen ilk oluşturulduğunda senkron olarak çalışır. Servis veya veritabanı beklemek gerekmiyorsa kullanılabilir.

### 15.2. OnInitializedAsync

```csharp
protected override async Task OnInitializedAsync()
{
    await base.OnInitializedAsync();
    currentUser = (await StateProvider.GetAuthenticationStateAsync()).User;

    if (currentUser.Identity?.IsAuthenticated == true)
    {
        await LoadAsync();
    }
}
```

Bileşen açılırken asenkron bir işlem yapılacaksa kullanılır.

`await base.OnInitializedAsync()` önemlidir. Önce temel sınıftaki `AuthRequiredComponent` giriş kontrolünü yapar, sonra sayfanın kendi veri yükleme işlemi devam eder.

### 15.3. IDisposable ve IAsyncDisposable

Rapor ve mazeret sayfaları şu direktifi kullanır:

```razor
@implements IAsyncDisposable
```

Sayfa kapatılırken:

```csharp
public ValueTask DisposeAsync()
{
    cancellationTokenSource.Cancel();
    cancellationTokenSource.Dispose();
    return ValueTask.CompletedTask;
}
```

çalışır.

Böylece kullanıcı başka bir sayfaya geçtiğinde devam eden veritabanı veya dosya işlemleri iptal edilebilir.

`Routes.razor` ise event aboneliklerini kaldırmak için `IDisposable` kullanır:

```csharp
AuthenticationStateProvider.AuthenticationStateChanged -= HandleAuthenticationStateChanged;
NavigationManager.LocationChanged -= HandleLocationChanged;
```

Event aboneliklerini kaldırmak, eski bileşenin bellekte gereksiz yere tutulmasını önler.

### 15.4. InvokeAsync

`Routes.razor` içinde bazı yönlendirmeler `InvokeAsync` ile yapılır:

```csharp
await InvokeAsync(() => NavigateIfNeeded(targetUrl));
```

Kimlik doğrulama veya konum değişikliği gibi olaylar bileşenin normal render akışının dışından gelebilir. `InvokeAsync`, işlemi Blazor bileşeninin doğru senkronizasyon bağlamında çalıştırır.

---

## 16. Bileşen Durumu ve Yeniden Render

Bir Razor bileşenindeki aşağıdaki alanlar bileşenin durumunu temsil eder:

```csharp
private bool isLoading;
private string? errorMessage;
private IReadOnlyList<ReportImportHistoryItem> history = [];
```

Örneğin:

```csharp
isLoading = true;
history = await ReportImportService.GetHistoryAsync(...);
isLoading = false;
```

Blazor bu değişikliklerden sonra görünümü tekrar değerlendirir:

- `isLoading == true` iken progress göstergesi görünür.
- İşlem bitince progress kaybolur.
- `history` dolduğunda tablo oluşturulur.

Event metodu veya yaşam döngüsü metodu tamamlandığında Blazor genellikle otomatik render yapar. Bu nedenle çoğu işlemde açıkça `StateHasChanged()` çağırmak gerekmez.

Interactive Server modelinde bu durum alanları tarayıcının JavaScript belleğinde değil, kullanıcının sunucudaki Blazor circuit belleğinde tutulur.

---

## 17. Bağlantı ve ReconnectModal

Interactive Server uygulamasında kullanıcının tarayıcısı ile sunucu arasında canlı bir bağlantı bulunur. İnternet veya sunucu bağlantısı kesilirse buton tıklamaları sunucuya ulaşamaz.

[`ReconnectModal.razor`](../OzkFireTakibiClient/Src/Components/Layout/ReconnectModal.razor), bağlantı durumunu kullanıcıya gösterir.

İlgili JavaScript dosyası [`ReconnectModal.razor.js`](../OzkFireTakibiClient/Src/Components/Layout/ReconnectModal.razor.js) şu Blazor API’lerini kullanır:

```javascript
Blazor.reconnect();
Blazor.resumeCircuit();
```

Bağlantı yeniden kurulamazsa sayfa yenilenir. Bu JavaScript uygulamanın iş mantığını değil, Interactive Server bağlantısının yeniden kurulmasını yönetir.

---

## 18. Dosya Yükleme

[`Raporlar.razor`](../OzkFireTakibiClient/Src/Components/Pages/Raporlar.razor), Blazor’un `InputFile` bileşenini kullanır:

```razor
<InputFile class="file-input"
           accept=".xls,.xlsx"
           disabled="@isProcessing"
           OnChange="HandleMonthlyFileSelectedAsync" />
```

Dosya yükleme akışı:

1. Kullanıcı Excel dosyasını seçer.
2. Blazor `InputFileChangeEventArgs` üretir.
3. `eventArgs.File` ile seçilen dosyaya erişilir.
4. `OpenReadStream(maxSize)` dosya boyutunu sınırlar.
5. Dosya sunucudaki geçici dizine kopyalanır.
6. İki dosya da seçildiyse `ReportImportService.PreviewPairAsync()` çalışır.
7. Önizleme sonucu `pairPreview` alanına atanır.
8. Blazor önizleme tablosunu ekranda gösterir.
9. Kullanıcı onaylarsa raporlar tek transaction içinde kaydedilir.
10. Sayfa kapanınca veya kullanıcı vazgeçince geçici dosyalar silinir.

Blazor burada dosya seçim ve aktarım köprüsüdür. Excel ayrıştırma `ReportImportParser`, veritabanı işlemleri ise `ReportImportService` tarafından gerçekleştirilir.

---

## 19. Filtreleme ve Sayfalama

Projede hem tarayıcı belleğinde hem de veritabanında filtreleme örnekleri bulunur.

### 19.1. Bellekte filtreleme

`MazeretMagazalari.razor` önce mağazaları servisten alır, sonra kullanıcının yazdığı metne göre bellekte filtreler:

```csharp
private IEnumerable<ExcuseStoreItem> FilteredStores =>
    string.IsNullOrWhiteSpace(searchText)
        ? stores
        : stores.Where(...);
```

Bu işlem her tuşta veritabanına gitmez.

### 19.2. Sunucuda filtreleme

`Mazeretler.razor` ve `RaporDetay.razor`, arama veya filtre uygulanınca servisi yeniden çağırır:

```csharp
private Task ApplyFiltersAsync() => LoadAsync(1);
```

### 19.3. Sayfalama

```razor
<button @onclick="() => LoadAsync(result.PageNumber - 1)">
    Önceki
</button>
```

Yeni sayfa numarası servise gönderilir ve yalnızca ilgili kayıtlar yüklenir. Bu yaklaşım büyük veri kümelerinin tamamının tek seferde tarayıcıya gönderilmesini önler.

---

## 20. Çift Tıklama ve Eşzamanlılık Korumaları

Projede uzun veya kayıt oluşturan işlemlerde aşağıdaki kalıp sık kullanılır:

```csharp
if (isProcessing)
{
    return;
}

isProcessing = true;

try
{
    await IslemAsync();
}
finally
{
    isProcessing = false;
}
```

Butonda da aynı durum kullanılır:

```razor
<button disabled="@isProcessing">
```

Bu iki koruma birlikte çalışır:

- Arayüzde buton devre dışı kalır.
- Çok hızlı ikinci event yine de gelirse C# tarafındaki `if` işlemi engeller.
- Hata oluşsa bile `finally` bloğu bayrağı sıfırlar.

Projede kullanılan başlıca işlem bayrakları:

- `isProcessing`
- `isLoading`
- `isSaving`
- `isDeleting`
- `isHistoryLoading`

Bu bayraklar hem kullanıcı arayüzünü yönetir hem de mükerrer işlemleri azaltır.

### CancellationTokenSource

Sayfa bileşenleri uzun süren servis çağrılarına bir `CancellationToken` gönderir:

```csharp
private readonly CancellationTokenSource cancellationTokenSource = new();
```

Sayfa kapatıldığında token iptal edilir. Servisler ve Entity Framework sorguları bu iptal bilgisini dikkate alabilir.

Bu konunun ayrıntılı açıklaması için ayrıca [`blazor-race-conditions-rehberi.md`](blazor-race-conditions-rehberi.md) dokümanına bakılabilir.

---

## 21. CSS Isolation

Bir bileşenin yanında aynı isimde `.razor.css` dosyası bulunursa Blazor bu stilleri bileşene özel olarak işler.

Örneğin:

```text
MazeretDetay.razor
MazeretDetay.razor.css
```

[`MazeretDetay.razor.css`](../OzkFireTakibiClient/Src/Components/Pages/MazeretDetay.razor.css) içindeki `.is-pre-wrap` kuralı ilgili bileşende kullanılır.

Benzer şekilde:

```text
ReconnectModal.razor
ReconnectModal.razor.css
ReconnectModal.razor.js
```

dosyaları aynı bileşenin görünüm, stil ve JavaScript parçalarını yan yana tutar.

Bileşene özel CSS dosyaları derleme sırasında `OzkFireTakibiClient.styles.css` içinde birleştirilir.

---

## 22. Projedeki Sayfaların Blazor Açısından Görevleri

### Anasayfa.razor

[`Anasayfa.razor`](../OzkFireTakibiClient/Src/Components/Pages/Anasayfa.razor):

- En basit korumalı sayfa örneğidir.
- `AuthRequiredComponent` sınıfından miras alır.
- Kullanıcı adı ve rolünü temel sınıftan alır.
- `PageTitle` kullanımını gösterir.

### Giris.razor

[`Giris.razor`](../OzkFireTakibiClient/Src/Components/Pages/Giris.razor):

- `EmptyLayout` kullanır.
- `EditForm` içerir.
- Data Annotations doğrulaması yapar.
- `InputText`, `InputCheckbox` ve `ValidationMessage` kullanır.
- `LoginService` servisini inject eder.
- İşlem durumunu `isProcessing` ile yönetir.

### Raporlar.razor

[`Raporlar.razor`](../OzkFireTakibiClient/Src/Components/Pages/Raporlar.razor):

- `InputFile` ile dosya seçer.
- Rol bazlı `AuthorizeView` kullanır.
- Yükleniyor, hata ve başarı durumlarını yönetir.
- Koşullu önizleme oluşturur.
- `foreach` ile rapor geçmişi tablosu üretir.
- Modal pencereyi koşullu olarak gösterir.
- Uzun işlemleri `CancellationToken` ile iptal edebilir.
- Sayfa kapanırken geçici dosyaları temizler.

### RaporDetay.razor

[`RaporDetay.razor`](../OzkFireTakibiClient/Src/Components/Pages/RaporDetay.razor):

- URL’den `ImportId` parametresi alır.
- Arama ve sayfalama yapar.
- Seçilen rapor satırı tipine göre dinamik kolonlar oluşturur.
- Checkbox ve select değişikliklerini işler.
- Aylık ve kümülatif verileri karşılaştırır.
- Admin kullanıcılara koşullu mazeret oluşturma işlemi sunar.

### Mazeretler.razor

[`Mazeretler.razor`](../OzkFireTakibiClient/Src/Components/Pages/Mazeretler.razor):

- Mazeret listesini servis üzerinden yükler.
- Arama, durum filtresi ve sayfalama yapar.
- Enter tuşunu `KeyboardEventArgs` ile yakalar.
- Admin kullanıcılara mağaza kapsamı bağlantısını gösterir.

### MazeretDetay.razor

[`MazeretDetay.razor`](../OzkFireTakibiClient/Src/Components/Pages/MazeretDetay.razor):

- URL’den `ExcuseId` parametresi alır.
- Mazeret geçmişini `foreach` ile gösterir.
- Servisten gelen `CanRespond` ve `CanReview` durumlarına göre form alanları açar.
- Mağaza cevabı, yönetici onayı ve revizyon isteği işlemlerini yürütür.
- Kayıt sonrasında detay verisini yeniden yükler.

### MazeretMagazalari.razor

[`MazeretMagazalari.razor`](../OzkFireTakibiClient/Src/Components/Pages/MazeretMagazalari.razor):

- Mağaza listesini servis üzerinden yükler.
- Arama metnini `oninput` ile anında günceller.
- Listeyi veritabanına yeniden gitmeden bellekte filtreler.
- Checkbox değişikliğini servise kaydeder.
- Başarılı işlem sonrasında yerel listeyi günceller.

### Error.razor ve NotFound.razor

- [`Error.razor`](../OzkFireTakibiClient/Src/Components/Pages/Error.razor), sunucu tarafı hata ekranını ve istek kimliğini gösterir.
- [`NotFound.razor`](../OzkFireTakibiClient/Src/Components/Pages/NotFound.razor), hiçbir rota ile eşleşmeyen URL’lerde gösterilir.

---

## 23. Blazor ile Diğer Teknolojilerin Ayrımı

Projeyi öğrenirken hangi yapının Blazor’a, hangisinin başka bir teknolojiye ait olduğunu ayırmak önemlidir.

| Yapı | Teknoloji |
|---|---|
| `.razor` bileşenleri | Blazor |
| `@bind`, `@onclick`, `@page`, `Router` | Blazor |
| `EditForm`, `InputText`, `InputFile` | Blazor |
| `Program.cs`, middleware ve DI | ASP.NET Core |
| `AppDbContext`, entity ve migration dosyaları | Entity Framework Core |
| `ReportImportService`, `ExcuseService` | Uygulamanın C# iş katmanı |
| Bulma | CSS/UI kütüphanesi |
| Font Awesome | İkon kütüphanesi |
| ExcelDataReader | Excel okuma kütüphanesi |
| SQL Server | Veritabanı |
| `ReconnectModal.razor.js` | Blazor bağlantısını destekleyen JavaScript |

---

## 24. Yeni Bir Blazor Sayfası Eklerken İzlenebilecek Kalıp

Bu projeye giriş gerektiren yeni bir liste sayfası eklemek için aşağıdaki başlangıç kalıbı kullanılabilir:

```razor
@page "/ornekler"
@inherits AuthRequiredComponent
@implements IAsyncDisposable
@inject OrnekService OrnekService

<PageTitle>Örnekler</PageTitle>

<h1 class="title">Örnekler</h1>

@if (!string.IsNullOrWhiteSpace(errorMessage))
{
    <div class="notification is-danger">@errorMessage</div>
}

@if (isLoading)
{
    <progress class="progress is-primary" max="100">Yükleniyor</progress>
}
else if (items.Count == 0)
{
    <div class="notification is-light">Kayıt bulunamadı.</div>
}
else
{
    <ul>
        @foreach (var item in items)
        {
            <li>@item.Name</li>
        }
    </ul>
}

@code {
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private IReadOnlyList<OrnekItem> items = [];
    private string? errorMessage;
    private bool isLoading;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (IsAuthenticated)
        {
            await LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;
        errorMessage = null;

        try
        {
            items = await OrnekService.GetListAsync(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
        }
        finally
        {
            isLoading = false;
        }
    }

    public ValueTask DisposeAsync()
    {
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        return ValueTask.CompletedTask;
    }
}
```

Bu kalıp projedeki mevcut yaklaşımı takip eder:

- URL tanımı
- Giriş kontrolü
- Servis injection
- İlk yükleme
- Yükleniyor ve hata durumları
- Koşullu liste görünümü
- Tekrarlanan işlem koruması
- Sayfa kapanırken iptal yönetimi

---

## 25. Kısa Özet

Bu projede Blazor aşağıdaki görevler için kullanılır:

- Sayfa ve tekrar kullanılabilir bileşen oluşturma
- URL yönlendirmesi
- Layout yönetimi
- Buton, input ve klavye olayları
- İki yönlü veri bağlama
- Form doğrulama
- Dosya yükleme
- Kullanıcı oturum durumunu bileşenlere yayma
- Rol bazlı görünürlük
- Yükleniyor, hata ve başarı durumlarını yönetme
- Bileşen yaşam döngüsü ve işlem iptali
- Bağlantı kopması durumunda yeniden bağlanma

Veritabanı erişimi, Excel ayrıştırma ve temel iş kuralları ise Razor bileşenlerinin içinde tutulmak yerine C# servislerine ayrılmıştır.

Bu ayrım projenin temel tasarım yaklaşımını özetler:

> Razor bileşenleri ekranı ve kullanıcı etkileşimini yönetir; servisler iş kurallarını, Entity Framework Core ise kalıcı veriyi yönetir.
