# OzkFireTakibi.Dashboard geliştirme kuralları

- Uygulama tek `.csproj` içinde kalmalıdır. Yeni class library veya yardımcı proje ancak açıkça istenirse eklenir.
- Basit ve doğrudan çözüm tercih edilmelidir; varsayımsal ihtiyaçlar için katman, servis veya abstraction oluşturulmamalıdır.
- Arayüzde öncelikle BulmaCSS 1.x sınıfları ve Bulma component desenleri kullanılmalıdır.
- Bulma ile karşılanabilen buton, kart, modal, sekme, form alanı, bildirim ve tablo yapıları için özel UI component veya özel CSS yazılmamalıdır.
- Özel Razor component yalnızca anlamlı bir davranışı izole ediyorsa veya tekrar kullanılıyorsa oluşturulmalıdır.
- Özel CSS yalnızca Bulma'nın karşılamadığı zorunlu davranışlar için kullanılmalıdır; mevcut Bulma teması ve değişkenleri korunmalıdır.
- `OzkFireTakibiClient` projesi açıkça istenmedikçe değiştirilmemelidir.
