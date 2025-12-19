using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GatePass.MS.ClientApp.Data;
using GatePass.MS.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using GatePass.MS.ClientApp.Service;

namespace GatePass.MS.ClientApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserActivityService _userActivityService;
        private readonly ICurrentCompany _current;

        public DepartmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ICurrentCompany current, UserActivityService userActivityService)
        {
            _context = context;
            _current = current;
            _userManager = userManager;
            _userActivityService = userActivityService;
        }

        // GET: Departments
        public async Task<IActionResult> Index()
        {
            List<Department> departments = await _context.Department.Where(d=>d.CompanyId==_current.Value.Id).ToListAsync();
            return View(departments);
        }

        // GET: Departments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Department
                .Include(d => d.ParentDepartment)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (department == null)
            {
                return NotFound();
            }
            return View(department);
        }

        // GET: Department/Create?parentId=5
        public IActionResult Create(int? parentId)
        {
            var model = new Department();
            if (parentId.HasValue)
            {
                model.ParentDepartmentId = parentId.Value;
            }
            return View(model);
        }

        // POST: Department/Create
        [Authorize(Roles = "Admin")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([Bind("Name,ParentDepartmentId")] Department department)
        {
            department.CompanyId = _current.Value.Id;
            _context.Add(department);
                await _context.SaveChangesAsync();

                // Log the activity asynchronously
                var userId = _userManager.GetUserId(User);
                await _userActivityService.LogActivityAsync(userId, "Create", $"Department '{department.Name}' created.");

                return RedirectToAction(nameof(Index));
           
        }

        // GET: Department/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var department = await _context.Department.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            return View(department);
        }

        // POST: Department/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ParentDepartmentId")] Department department)
        {
            if (id != department.Id)
            {
                return NotFound();
            }

              try
                {
                department.CompanyId = _current.Value.Id;
                _context.Update(department);
                    await _context.SaveChangesAsync();

                    // Log the activity asynchronously
                    var userId = _userManager.GetUserId(User);
                    await _userActivityService.LogActivityAsync(userId, "Edit", $"Department '{department.Name}' edited.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.Id))
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

        // GET: Department/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var department = await _context.Department
                .FirstOrDefaultAsync(m => m.Id == id);
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // POST: Department/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Department.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }

            // Step 1: Delete RequestInformation records where DepartmentId matches the department being deleted
            var departmentRequests = await _context.RequestInformation.Where(r => r.DepartmentId == department.Id).ToListAsync();
            _context.RequestInformation.RemoveRange(departmentRequests);

            // Step 2: Get employees in the department
            var employees = await _context.Employee.Where(e => e.DepartmentId == department.Id).ToListAsync();

            foreach (var employee in employees)
            {
                // Delete requests where the employee is referenced
                var requests = await _context.RequestInformation.Where(r => r.EmployeeId == employee.Id).ToListAsync();
                _context.RequestInformation.RemoveRange(requests);

                // Find associated user
                var userAccount = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employee.Id);

                if (userAccount != null)
                {
                    // Delete RequestInformation where ApproverId is the UserId of this user
                    var approverRequests = await _context.RequestInformation.Where(r => r.ApproverId == userAccount.Id).ToListAsync();
                    _context.RequestInformation.RemoveRange(approverRequests);

                    // Now delete the user
                    _context.Users.Remove(userAccount);
                }

                // Delete employee
                _context.Employee.Remove(employee);
            }

            // Step 3: Handle child departments
            var childDepartments = await _context.Department.Where(d => d.ParentDepartmentId == department.Id).ToListAsync();

            foreach (var child in childDepartments)
            {
                var childDepartmentRequests = await _context.RequestInformation.Where(r => r.DepartmentId == child.Id).ToListAsync();
                _context.RequestInformation.RemoveRange(childDepartmentRequests);

                var childEmployees = await _context.Employee.Where(e => e.DepartmentId == child.Id).ToListAsync();

                foreach (var childEmployee in childEmployees)
                {
                    var childRequests = await _context.RequestInformation.Where(r => r.EmployeeId == childEmployee.Id).ToListAsync();
                    _context.RequestInformation.RemoveRange(childRequests);

                    var childUserAccount = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == childEmployee.Id);

                    if (childUserAccount != null)
                    {
                        var childApproverRequests = await _context.RequestInformation.Where(r => r.ApproverId == childUserAccount.Id).ToListAsync();
                        _context.RequestInformation.RemoveRange(childApproverRequests);

                        _context.Users.Remove(childUserAccount);
                    }

                    _context.Employee.Remove(childEmployee);
                }

                _context.Department.Remove(child);
            }
  
            // Step 4: Finally, delete the department
            _context.Department.Remove(department);
            await _context.SaveChangesAsync();
            var userId = _userManager.GetUserId(User);
            await _userActivityService.LogActivityAsync(userId, "Delete", $"Department '{department.Name}' deleted.");
            TempData["message"] = "Department Deleted successfully!";
            TempData["MessageType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id)
        {
            return _context.Department.Any(e => e.Id == id);
        }

       
    }
}
