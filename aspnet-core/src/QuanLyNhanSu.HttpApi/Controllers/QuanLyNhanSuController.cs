using QuanLyNhanSu.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace QuanLyNhanSu.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class QuanLyNhanSuController : AbpControllerBase
{
    protected QuanLyNhanSuController()
    {
        LocalizationResource = typeof(QuanLyNhanSuResource);
    }
}
