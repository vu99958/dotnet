using System;
using System.Collections.Generic;
using System.Text;
using QuanLyNhanSu.Localization;
using Volo.Abp.Application.Services;

namespace QuanLyNhanSu;

/* Inherit your application services from this class.
 */
public abstract class QuanLyNhanSuAppService : ApplicationService
{
    protected QuanLyNhanSuAppService()
    {
        LocalizationResource = typeof(QuanLyNhanSuResource);
    }
}
