using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GatePass.MS.ClientApp.Data;
using GatePass.MS.Domain;
using Microsoft.AspNetCore.Authorization;
using GatePass.MS.ClientApp.Service;
using Microsoft.AspNetCore.Identity;

namespace GatePass.MS.ClientApp.Controllers
{
    [Authorize]
    public class DesignationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserActivityService _userActivityService;
        private readonly ICurrentCompany _current;

        public DesignationsController(ApplicationDbContext context,UserActivityService userActivityService,UserManager<ApplicationUser> userManager, ICurrentCompany current)
        {
            _context = context;
            _current = current;
            _userActivityService = userActivityService;
            _userManager= userManager;
        }

        // GET: Designations
        public async Task<IActionResult> Index()
        {
            return View(await _context.Designation.Where(d=>d.CompanyId==_current.Value.Id).ToListAsync());
        }

        // GET: Designations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var designation = await _context.Designation
                .FirstOrDefaultAsync(m => m.Id == id);
            if (designation == null)
            {
                return NotFound();
            }

            return View(designation);
        }

        // GET: Designations/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Designations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Designation designation)
        {
            designation.CompanyId = _current.Value.Id;
           
                _context.Add(designation);
                await _context.SaveChangesAsync();
                // Log the activity asynchronously
                var userId = _userManager.GetUserId(User);
                await _userActivityService.LogActivityAsync(userId, "Create", $"Designation '{designation.Name}' created.");

                return RedirectToAction(nameof(Index));

        }

        // GET: Designations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var designation = await _context.Designation.FindAsync(id);
            if (designation == null)
            {
                return NotFound();
            }
            return View(designation);
        }

        // POST: Designations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            var companyId = _current.Value.Id;

            var existing = await _context.Designation
                .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId);

            if (existing == null) return NotFound();

            // Only allow updating specific fields
            if (await TryUpdateModelAsync(existing, prefix: "",
                d => d.Name)) // add more properties here if needed
            {
                // CompanyId remains intact (server-owned)
                await _context.SaveChangesAsync();

                var userId = _userManager.GetUserId(User);
                await _userActivityService.LogActivityAsync(userId, "Edit", $"Designation '{existing.Name}' edited.");

                return RedirectToAction(nameof(Index));
            }

            // If we get here, ModelState had errors; show them
            return View(existing);
        }

        // GET: Designations/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var designation = await _context.Designation
                .FirstOrDefaultAsync(m => m.Id == id);
            if (designation == null)
            {
                return NotFound();
            }

            return View(designation);
        }

        // POST: Designations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var designation = await _context.Designation.FindAsync(id);
            if (designation != null)
            {
                _context.Designation.Remove(designation);
            }

            await _context.SaveChangesAsync();
            // Log the activity asynchronously
            var userId = _userManager.GetUserId(User);
            await _userActivityService.LogActivityAsync(userId, "Delete", $"Designation '{designation.Name}' deleted.");

            return RedirectToAction(nameof(Index));
        }

        private bool DesignationExists(int id)
        {
            return _context.Designation.Any(e => e.Id == id);
        }
    }
}
