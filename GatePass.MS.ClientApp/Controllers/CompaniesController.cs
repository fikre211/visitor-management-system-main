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
    public class CompaniesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserActivityService _userActivityService;

        public CompaniesController(ApplicationDbContext context, UserActivityService userActivityService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userActivityService = userActivityService;
            _userManager = userManager;
        }

        // GET: Designations
        public async Task<IActionResult> Index()
        {
            return View(await _context.Company.ToListAsync());
        }

        // GET: Designations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Company
                .FirstOrDefaultAsync(m => m.Id == id);
            if (company == null)
            {
                return NotFound();
            }

            return View(company);
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
        public async Task<IActionResult> Create([Bind("Id,Name,Slug,Email,Phone")] Company company,IFormFile file)
        {
            if (ModelState.IsValid)
            {
                _context.Add(company);
                await _context.SaveChangesAsync();
                if (file != null)
                {
                    // Implement logic to save the uploaded file
                    // This example uses a folder named "uploads"
                    var uploadsFolder = $"{Directory.GetCurrentDirectory()}\\wwwroot\\img\\";
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    string FileName = file.FileName;
                    string filePath = Path.Combine(uploadsFolder, FileName);
                    try
                    {
                        await using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // Update user profile picture path
                        company.LogoPath = "/img/" + FileName;
                        await _context.SaveChangesAsync();
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        // Handle the exception (log it, show a message to the user, etc.)
                        return StatusCode(StatusCodes.Status500InternalServerError, "Access to the file path is denied.");
                    }
                    catch (Exception ex)
                    {
                        // Handle other potential exceptions
                        return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while uploading the file.");
                    }

                }

                // Log the activity asynchronously
                var userId = _userManager.GetUserId(User);
                await _userActivityService.LogActivityAsync(userId, "Create", $"Company '{company.Name}' created.");

                return RedirectToAction(nameof(Index));

            }
            return View(company);
        }

        // GET: Designations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Company.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }
            return View(company);
        }

        // POST: Designations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Slug,Email,Phone")] Company company, IFormFile file)
        {
            if (id != company.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(company);
                    await _context.SaveChangesAsync();
                    if (file != null)
                    {
                        // Implement logic to save the uploaded file
                        // This example uses a folder named "uploads"
                        var uploadsFolder = $"{Directory.GetCurrentDirectory()}\\wwwroot\\img\\";
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
                        string FileName = file.FileName;
                        string filePath = Path.Combine(uploadsFolder, FileName);
                        try
                        {
                            await using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            // Update user profile picture path
                            company.LogoPath = "/img/" + FileName;
                            await _context.SaveChangesAsync();
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            // Handle the exception (log it, show a message to the user, etc.)
                            return StatusCode(StatusCodes.Status500InternalServerError, "Access to the file path is denied.");
                        }
                        catch (Exception ex)
                        {
                            // Handle other potential exceptions
                            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while uploading the file.");
                        }

                    }

                    // Log the activity asynchronously
                    var userId = _userManager.GetUserId(User);
                    await _userActivityService.LogActivityAsync(userId, "Edit", $"Company '{company.Name}' edited.");

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CompanyExists(company.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        // GET: Designations/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Company
                .FirstOrDefaultAsync(m => m.Id == id);
            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        // POST: Designations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var company = await _context.Company.FindAsync(id);
            if (company != null)
            {
                _context.Company.Remove(company);
            }

            await _context.SaveChangesAsync();
            // Log the activity asynchronously
            var userId = _userManager.GetUserId(User);
            await _userActivityService.LogActivityAsync(userId, "Delete", $"Designation '{company.Name}' deleted.");

            return RedirectToAction(nameof(Index));
        }

        private bool CompanyExists(int id)
        {
            return _context.Company.Any(e => e.Id == id);
        }
    }
}
