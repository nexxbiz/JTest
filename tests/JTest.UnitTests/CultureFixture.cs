using System.Globalization;

namespace JTest.UnitTests;

public sealed class CultureFixture : IDisposable
{
    private readonly CultureInfo _oldCulture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _oldUICulture = CultureInfo.CurrentUICulture;

    public CultureFixture()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _oldCulture;
        CultureInfo.CurrentUICulture = _oldUICulture;
        CultureInfo.DefaultThreadCurrentCulture = null;
        CultureInfo.DefaultThreadCurrentUICulture = null;
    }
}