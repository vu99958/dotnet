using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyNhanSu.Application;
using QuanLyNhanSu.Application.Contracts;

namespace QuanLyNhanSu.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserKeyController : ControllerBase
{
    private readonly UserKeyAppService _userKeyAppService;

    public UserKeyController(UserKeyAppService userKeyAppService)
    {
        _userKeyAppService = userKeyAppService;
    }

    /// <summary>
    /// Tạo user key mới
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateUserKey([FromBody] CreateUserKeyDto input)
    {
        try
        {
            var result = await _userKeyAppService.CreateUserKeyAsync(input);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách user keys của user hiện tại
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetUserKeys()
    {
        try
        {
            var result = await _userKeyAppService.GetUserKeysAsync();
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Xác minh key hợp lệ
    /// </summary>
    [HttpPost("verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyKey([FromBody] VerifyKeyDto input)
    {
        try
        {
            var result = await _userKeyAppService.VerifyKeyAsync(input.Key);
            if (result == null)
                return BadRequest(new { success = false, error = "Key không hợp lệ hoặc đã hết hạn" });

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Xóa user key
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUserKey(Guid id)
    {
        try
        {
            await _userKeyAppService.DeleteUserKeyAsync(id);
            return Ok(new { success = true, message = "Xóa key thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}

public class VerifyKeyDto
{
    public string Key { get; set; } = null!;
}
