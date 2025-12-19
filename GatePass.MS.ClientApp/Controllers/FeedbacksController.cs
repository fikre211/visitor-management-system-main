using GatePass.MS.ClientApp.Data;
using GatePass.MS.ClientApp.Service;
using GatePass.MS.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace GatePass.MS.ClientApp.Controllers
{
    public class FeedbacksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserActivityService _userActivityService;
        private readonly ICurrentCompany _current;
        public FeedbacksController(ApplicationDbContext context, UserActivityService userActivityService, ICurrentCompany current,
           UserManager<ApplicationUser> userManager)
        {
            _current = current;
            _context = context;
            _userManager = userManager;
            _userActivityService = userActivityService;
        }
        // GET: Employees
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Feedback
                .Where(e => _current.Value.Id == 0 || e.CompanyId == _current.Value.Id);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.Feedback
                .FirstOrDefaultAsync(m => m.Id == id);
            if (feedback == null)
            {
                return NotFound();
            }

            return PartialView("_Details", feedback);
        }

        // POST: Feedbacks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Feedback feedback)
        {
            if (feedback == null)
                return BadRequest();

            // Ensure CompanyId is set from current company if not provided
            if (feedback.CompanyId == 0 && _current?.Value != null)
            {
                feedback.CompanyId = _current.Value.Id;
            }

            // If RequestId was posted via form, it'll be bound to feedback.RequestId already

            _context.Feedback.Add(feedback);
            await _context.SaveChangesAsync();

            // Return to index or partial as needed
            return RedirectToAction(nameof(Index));
        }

    }
}
