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
using Microsoft.AspNetCore.Identity;
using GatePass.MS.ClientApp.Service;
using System.Text;
using ExcelDataReader;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace GatePass.MS.ClientApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserActivityService _userActivityService;
        private readonly ICurrentCompany _current;

        public EmployeesController(ApplicationDbContext context,UserActivityService userActivityService, ICurrentCompany current,
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
            var applicationDbContext = _context.Employee
                .Where(e=>e.CompanyId==_current.Value.Id)
                .Include(e => e.Department)
                .Include(e => e.Designation);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employee
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return PartialView("_Details", employee);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
           
            ViewData["DepartmentId"] = new SelectList(_context.Department.Where(d => d.CompanyId == _current.Value.Id), "Id", "Name");
            ViewData["DesignationId"] = new SelectList(_context.Designation.Where(d => d.CompanyId == _current.Value.Id), "Id", "Name");
            return PartialView("_Create");
        }

        // POST: Employees/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,Email,Phone,Gender,Address,Age,DepartmentId,DesignationId")] Employee employee)
        {
            bool emailExists = await _context.Employee.AnyAsync(e => e.Email == employee.Email);
            if (!emailExists)
            {
                employee.CompanyId=_current.Value.Id;
                _context.Add(employee);
                await _context.SaveChangesAsync();
                // Log the activity asynchronously
                var userId = _userManager.GetUserId(User);
                await _userActivityService.LogActivityAsync(userId, "Create", $"Employee '{employee.FirstName}' Created.");

                TempData["message"] = "Employee is Created successfully!";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["message"] = "Employee  is already registered!";
                TempData["MessageType"] = "error";
            }
            return RedirectToAction(nameof(Index));


        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employee.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            ViewData["DepartmentId"] = new SelectList(_context.Department.Where(d => d.CompanyId == _current.Value.Id), "Id", "Name");
            ViewData["DesignationId"] = new SelectList(_context.Designation.Where(d => d.CompanyId == _current.Value.Id), "Id", "Name");
            return PartialView("_Edit", employee);
        }

        // POST: Employees/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
         [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Email,Phone,Gender,Address,Age,DepartmentId,DesignationId")] Employee employee)
        {
            if (id != employee.Id)
            {
                return NotFound();
            }

           
            try
            {
                _context.Update(employee);
                employee.CompanyId = _current.Value.Id;
                await _context.SaveChangesAsync();
                // Log the activity asynchronously
                var userId = _userManager.GetUserId(User);
                await _userActivityService.LogActivityAsync(userId, "Edit", $"Employee '{employee.FirstName}' information edited.");

            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(employee.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            TempData["message"] = "Employee is Updated  successfully!";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Index));
           
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employee
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return PartialView("_Delete",employee);
        }

        // POST: Employees/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employee.FindAsync(id);
            var employeeid = employee.Id;
            var userId = _userManager.GetUserId(User);
            var useraccount=await _context.Users.FirstOrDefaultAsync(m=> m.EmployeeId == employeeid);
            if (useraccount != null)
            {
                _context.Users.Remove(useraccount);
            }
            if (employee != null)
            {

               
                _context.Employee.Remove(employee);
            }

            await _context.SaveChangesAsync();
            // Log the activity asynchronously
            await _userActivityService.LogActivityAsync(userId, "Delete", $"Employee '{employee.FirstName}' deleted.");

            TempData["message"] = "Employee Deleted successfully!";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employee.Any(e => e.Id == id);
        }

        public IActionResult UploadExcel()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (file != null && file.Length > 0)
            {
                var uploadsFolder = $"{Directory.GetCurrentDirectory()}\\wwwroot\\uploads\\";

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                using (var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        int cnt = 0;
                        do
                        {
                            bool isHeaderSkipped = false;
                            
                            while (reader.Read())
                            {
                                if (!isHeaderSkipped)
                                {
                                    isHeaderSkipped = true;
                                    continue;
                                }

                                Employee e = new Employee();
                                string email="df";
                                try
                                {
                                   email = reader.GetValue(3).ToString();
                                }
                                catch (Exception)
                                {
                                    TempData["message"] = $"inappropriate email field,check the data and try again later ";
                                    TempData["MessageType"] = "error";
                                    return RedirectToAction(nameof(Index));

                                }
                                bool emailExists = await _context.Employee.AnyAsync(e => e.Email == email);
                                    if (!emailExists)
                                    {
                                        try
                                        {

                                            e.FirstName = reader.GetValue(1).ToString();
                                            e.LastName = reader.GetValue(2).ToString();
                                            e.Email = email;
                                            e.Phone = reader.GetValue(4).ToString();
                                            e.Gender = reader.GetValue(5).ToString();
                                            e.Address = reader.GetValue(6).ToString();
                                            e.Age = Convert.ToInt32(reader.GetValue(7).ToString());
                                            e.DepartmentId = Convert.ToInt32(reader.GetValue(8).ToString());
                                            e.DesignationId = Convert.ToInt32(reader.GetValue(9).ToString());

                                            _context.Add(e);
                                            await _context.SaveChangesAsync();
                                        }
                                        catch (DbUpdateException)
                                        {
                                            TempData["message"] = $"inappropriate data is inserted,check the data and try again later ";
                                            TempData["MessageType"] = "error";
                                            return RedirectToAction(nameof(Index));

                                        }
                                    }
                                    else
                                    {
                                        cnt++;

                                    }



                            }
                        } while (reader.NextResult());
                        TempData["message"] = $"uploaded successfully  {cnt} duplicates removed!";
                        TempData["MessageType"] = "success";
                    }
                }
            }
            else
            {
                TempData["message"] = " file is empty!";
                TempData["MessageType"] = "error";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}


//test

