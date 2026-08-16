using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StackUnderflow.Controllers;

[Authorize(Policy = "IsModerator")]
public class ModerationController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
