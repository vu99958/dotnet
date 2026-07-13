using Volo.Abp.Settings;

namespace QuanLyNhanSu.Settings;

public class QuanLyNhanSuSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(QuanLyNhanSuSettings.Payroll.LatePenaltyPerMinute, "2000"),
            new SettingDefinition(QuanLyNhanSuSettings.Payroll.NetSalaryRate, "0.895")
        );
    }
}
