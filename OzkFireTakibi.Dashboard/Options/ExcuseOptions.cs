namespace OzkFireTakibi.Dashboard.Options;

/// <summary>
/// Otomatik mazeret oluşturma kuralları.
/// </summary>
public sealed class ExcuseOptions
{
    public const string SectionName = "Excuse";

    /// <summary>
    /// Aylık rapor genel fire büyüklüğüne uygulanacak çarpan. 1.50 değeri yüzde 50 fazlasıdır.
    /// </summary>
    public decimal ThresholdMultiplier { get; set; } = 1.50m;

    /// <summary>
    /// İlk kez görüldüğünde mazeret kapsamı dışında oluşturulacak merkez/yardımcı depo numaraları.
    /// Sonraki değişiklikler mağaza kapsam ekranından yönetilir.
    /// </summary>
    public int[] InitiallyExcludedStoreNumbers { get; set; } = [93, 94, 95, 103, 105];

    /// <summary>
    /// Mazeret listesinde bir sayfada gösterilecek kayıt sayısı.
    /// </summary>
    public int PageSize { get; set; } = 25;
}
