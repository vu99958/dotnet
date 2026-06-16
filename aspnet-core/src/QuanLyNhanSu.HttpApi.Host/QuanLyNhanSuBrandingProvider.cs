using Microsoft.Extensions.Localization;
using QuanLyNhanSu.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace QuanLyNhanSu;

[Dependency(ReplaceServices = true)]
public class QuanLyNhanSuBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<QuanLyNhanSuResource> _localizer;

    public QuanLyNhanSuBrandingProvider(IStringLocalizer<QuanLyNhanSuResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
