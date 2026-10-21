using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThanhNgan.Web.Data;

namespace ThanhNgan.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.StaffPolicy)]
public abstract class AdminControllerBase : Controller
{
}
