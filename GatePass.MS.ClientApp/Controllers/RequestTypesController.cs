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

namespace GatePass.MS.ClientApp.Controllers
{
    [Authorize]
    public class RequestTypesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RequestTypesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: RequestTypes
        public async Task<IActionResult> Index()
        {
            return View(await _context.RequestType.ToListAsync());
        }

        // GET: RequestTypes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestType = await _context.RequestType
                .FirstOrDefaultAsync(m => m.Id == id);
            if (requestType == null)
            {
                return NotFound();
            }

            return View(requestType);
        }

        // GET: RequestTypes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RequestTypes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,RequestTypeName")] RequestType requestType)
        {
            if (!ModelState.IsValid)
            {
                _context.Add(requestType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(requestType);
        }

        // GET: RequestTypes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestType = await _context.RequestType.FindAsync(id);
            if (requestType == null)
            {
                return NotFound();
            }
            return View(requestType);
        }

        // POST: RequestTypes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RequestTypeName")] RequestType requestType)
        {
            if (id != requestType.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(requestType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RequestTypeExists(requestType.Id))
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
            return View(requestType);
        }

        // GET: RequestTypes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestType = await _context.RequestType
                .FirstOrDefaultAsync(m => m.Id == id);
            if (requestType == null)
            {
                return NotFound();
            }

            return View(requestType);
        }

        // POST: RequestTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var requestType = await _context.RequestType.FindAsync(id);
            if (requestType != null)
            {
                _context.RequestType.Remove(requestType);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RequestTypeExists(int id)
        {
            return _context.RequestType.Any(e => e.Id == id);
        }
    }
}
