namespace OzkFireTakibi.Dashboard.Data.Entities;

/// <summary>
/// Excel raporlarında bulunan mağaza/depo tanımını ve mazeret kapsamını temsil eder.
/// </summary>
public class StoreEntity : BaseEntity<int>
{
    /// <summary>
    /// Excel'deki Depo Adı alanının güncel değeri.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Bu mağaza için otomatik veya manuel mazeret talebi oluşturulup oluşturulamayacağını belirtir.
    /// </summary>
    public bool IsExcuseEligible { get; set; } = true;
}
