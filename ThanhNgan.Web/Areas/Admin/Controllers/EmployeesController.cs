using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Areas.Admin.Models;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Identity;

namespace ThanhNgan.Web.Areas.Admin.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class EmployeesController : AdminControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private static readonly string[] StaffRoles = { RoleNames.WarehouseStaff, RoleNames.SalesStaff, RoleNames.Manager, RoleNames.Admin };

    public EmployeesController(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<IActionResult> Index()
    {
        var rows = new List<EmployeeRow>();
        foreach (var role in StaffRoles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role);
            foreach (var u in usersInRole)
            {
                var existing = rows.FirstOrDefault(r => r.Id == u.Id);
                if (existing is null)
                    rows.Add(new EmployeeRow { Id = u.Id, FullName = u.FullName, Email = u.Email ?? "", PhoneNumber = u.PhoneNumber, Roles = { role }, IsLocked = await _userManager.IsLockedOutAsync(u) });
                else
                    existing.Roles.Add(role);
            }
        }
        ViewBag.CurrentUserId = _userManager.GetUserId(User);
        return View(rows);
    }

    public IActionResult Create() => View(new CreateEmployeeForm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateEmployeeForm form)
    {
        if (!StaffRoles.Contains(form.Role)) ModelState.AddModelError(nameof(form.Role), "Vai trò không hợp lệ.");
        if (!ModelState.IsValid) return View(form);

        var user = new ApplicationUser { UserName = form.Email, Email = form.Email, EmailConfirmed = true, FullName = form.FullName };
        var result = await _userManager.CreateAsync(user, form.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(form);
        }
        await _userManager.AddToRoleAsync(user, form.Role);
        TempData["AdminMessage"] = "Đã tạo tài khoản nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        ViewBag.CurrentRoles = await _userManager.GetRolesAsync(user);
        ViewBag.AllRoles = StaffRoles;
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, string role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (!StaffRoles.Contains(role)) return BadRequest();

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);

        TempData["AdminMessage"] = "Đã cập nhật vai trò nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        if (id == _userManager.GetUserId(User))
        {
            TempData["AdminMessage"] = "Không thể tự khóa/mở khóa tài khoản của chính mình.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var isLocked = await _userManager.IsLockedOutAsync(user);
        user.LockoutEnabled = true;
        user.LockoutEnd = isLocked ? null : DateTimeOffset.MaxValue;
        await _userManager.UpdateAsync(user);

        TempData["AdminMessage"] = isLocked ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.";
        return RedirectToAction(nameof(Index));
    }
}
