using System;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GatePass.MS.ClientApp.Data;
using GatePass.MS.Domain;
using System.Text.RegularExpressions;
using System.IO;
using GatePass.MS.Domain.ViewModels;

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Identity.UI.Services;
using System.Text.Encodings.Web;
using GatePass.MS.Application;
using QRCoder;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using ZXing;
using ZXing.QrCode;
using GatePass.MS.ClientApp.Migrations;
using GatePass.MS.ClientApp.Service;
using SkiaSharp;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace GatePass.MS.ClientApp.Controllers
{
    public class GuestController : Controller
    {
        private readonly ILogger<GuestController> _logger;
        public readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IFileService _fileService;
        private readonly IWebHostEnvironment _environment;
        private readonly GuestActivityService _guestActivityService;
        private readonly SmsService _smsService;
        private readonly ICurrentCompany _current;
        private readonly IConfiguration _configuration;
        public GuestController(ApplicationDbContext context,
        ILogger<GuestController> logger,
         IConfiguration configuration,
       UserManager<ApplicationUser> userManager,
       IEmailSender emailSender,
       ICurrentCompany current,
       IWebHostEnvironment webHostEnvironment,
       IFileService fileService,GuestActivityService guestActivityService, SmsService smsService)
        {
            _configuration = configuration;
            _userManager = userManager;
            _emailSender = emailSender;
            _context = context;
            _current = current;

            _logger = logger;
            _fileService = fileService;
            _environment = webHostEnvironment;
            _guestActivityService = guestActivityService;
            _smsService = smsService;
         

        }
        [HttpGet]
        public async Task<IActionResult> Index(string? status)
        {

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound();
            }
            _context.Entry(currentUser).Reference(x => x.Employee).Load();

            int? gustId = currentUser?.GuestId;
            int? employeeId = currentUser?.EmployeeId;

            int? CurrentUserDepartmentId = currentUser?.Employee?.DepartmentId;
            var query = _context.RequestInformation.AsQueryable();
            var isInSupervisorRole = await _userManager.IsInRoleAsync(currentUser, "Superviser");
            var today = DateTime.Today;

            if (employeeId.HasValue)
            {
                query = query.Where(r => (r.EmployeeId == employeeId || (r.Employee.DepartmentId == CurrentUserDepartmentId && isInSupervisorRole)) && r.VisitDateTimeEnd >= today);
            }

            if (gustId.HasValue)
            {
                query = query.Where(r => r.GuestId == gustId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var requestInformation = await query
                .Include(r => r.Approver)
                  .ThenInclude(r => r.Employee)
                .Include(r => r.Employee)
                .Include(r => r.Guest)
                .Include(r => r.Attachments) // Include Attachments
                .ToListAsync();

            return View(requestInformation);
        }

        [HttpGet]
        public IActionResult Invite()
        {
            ViewData["DepartmentId"] = new SelectList(_context.Department.Where(d=>d.CompanyId==_current.Value.Id), "Id", "Name");
            ViewData["DesignationId"] = new SelectList(_context.Designation, "Id", "Name");
            ViewData["DepartmentListJson"] = JsonSerializer.Serialize(
                _context.Department.Select(d => new { value = d.Id, text = d.Name }).ToList()
            );

            ViewBag.RequestType = new SelectList(_context.RequestType, "Id", "RequestTypeName");
            return View(nameof(Invite));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        private async Task<bool> VerifyRecaptcha(string token)
        {
            var secretKey = _configuration["GoogleReCaptcha:SecretKey"];

            if (string.IsNullOrWhiteSpace(secretKey))
                return false;

            using var client = new HttpClient();
            var response = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}",
                null
            );

            var json = await response.Content.ReadAsStringAsync();
            dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

            return result.success == true;
        }


        [HttpPost]
        public async Task<IActionResult> Invite(RequestInformationViewModel model)
        {
            var captchaResponse = Request.Form["g-recaptcha-response"];

            if (string.IsNullOrEmpty(captchaResponse))
            {
                ModelState.AddModelError("", "Please verify that you are not a robot.");
                return View(nameof(Invite), model);
            }

            var isCaptchaValid = await VerifyRecaptcha(captchaResponse);
            if (!isCaptchaValid)
            {
                ModelState.AddModelError("", "reCAPTCHA validation failed.");
                return View(nameof(Invite), model);
            }

            try
            {

                // 1. Create Bitmap
                Bitmap image = new Bitmap(500, 300);

                // 2. Get Graphics Object
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.FillRectangle(Brushes.LightGray, 0, 0, image.Width, image.Height);

                    // Define Font & Brush
                    Font font = new Font("Arial", 18, FontStyle.Bold);
                    Brush brush = Brushes.Black;

                    // Draw text
                    graphics.DrawString($"First Name: {model.GuestFirstName}", font, brush, 50, 50);
                    graphics.DrawString($"Last Name: {model.GuestLastName}", font, brush, 50, 100);
                    graphics.DrawString($"Email: {model.GuestEmail}", font, brush, 50, 150);
                }

                // 3. Convert to byte array
                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, ImageFormat.Png);
                    model.ImageData = ms.ToArray(); // Store image as byte[]
                }

                // 4. Save to database



            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
            }

            var guest = new Guest
                    {
                        FirstName = $"{model.Title} {model.GuestFirstName}",
                        LastName = model.GuestLastName,
                        Email = model.GuestEmail,
                        Phone = model.GuestPhoneNumber,
                        CompanyName = model.CompanyName
                    };


                    await _context.Guest.AddAsync(guest);
                    await _context.SaveChangesAsync();

                    var entity = new RequestInformation
                    {
                        VisitDateTimeStart = model.VisitDateTimeStart,
                        VisitDateTimeEnd = model.VisitDateTimeEnd,
                        EmployeeId= model.EmployeeId,
                        PurposeOfVisit = model.PurposeOfVisit,
                        CompanyId=_current.Value.Id,
                        DepartmentId = model.DepartmentId,
                        IsIndividual = model.IsIndividual,
                        ImageData=model.ImageData,
                        GuestId = guest.Id,
                        AdditionalGuests = model.AdditionalGuests.Select(g => new Guest
                        {
                            FirstName = g.FirstName,
                            LastName = g.LastName,
                            Email=g.Email,
                            CompanyName = model.CompanyName,
                            Phone = string.IsNullOrWhiteSpace(g.Phone) ? model.GuestPhoneNumber : g.Phone
                        }).ToList(),
                        Devices = model.Devices.Select(d => new Device
                        {
                            DeviceName = d.DeviceName,
                            Identifier = d.Identifier,
                            Description = d.Description
                        }).ToList()
                    };

                    _context.RequestInformation.Add(entity);
                await _context.SaveChangesAsync();

            try
            {
                var employee = _context.Employee.FirstOrDefault(e => e.Id == model.EmployeeId);

                if (employee != null && !string.IsNullOrEmpty(employee.Email))
                {
                    await SendInviterEmail(employee.Email, entity.Id);

                    TempData["message"] = "Request has been sent successfully!";
                    TempData["MessageType"] = "success";
                }
                else
                {
                    TempData["message"] = "Employee email not found, but request processed.";
                    TempData["MessageType"] = "warning";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                TempData["message"] = "Request has been sent.";
                TempData["MessageType"] = "warning";
            }



            try
            {
                    await SendInviterSms(entity.Id);

                    TempData["message"] = "Request has been Sent successfully.";
                    TempData["MessageType"] = "success";
                }
                catch (Exception ex)
                {
                    // Log the exception
                    Console.WriteLine(ex.Message);

                    // Update the message for email failure
                    TempData["message"] = "Request has been Sent.";
                    TempData["MessageType"] = "warning";
                    TempData["MessageType"] = "warning";
                }
            



                return RedirectToAction(nameof(Invite));
           
        }
        public IActionResult GetGuestImage(int id)
        {
            var request = _context.RequestInformation.FirstOrDefault(r => r.Id == id);

            if (request?.ImageData != null)
            {
                return File(request.ImageData, "image/png");
            }

            // return placeholder if no image
            var placeholderPath = Path.Combine(_environment.WebRootPath, "images", "default-user.png");
            var bytes = System.IO.File.ReadAllBytes(placeholderPath);
            return File(bytes, "image/png");
        }

        [HttpGet]
        public async Task<IActionResult> GetSupervisorsByDepartment(int departmentId)
        {
            // Get users in Supervisor role
            var supervisorUsers = await _userManager.GetUsersInRoleAsync("SUPERVISER");

            // Get the employee IDs of those users
            var supervisorEmployeeIds = supervisorUsers
                .Where(u => u.EmployeeId != null)
                .Select(u => u.EmployeeId.Value)
                .ToList();

            // Get unique supervisors in department and only active ones
            var supervisors = _context.Employee
                .Where(e => e.DepartmentId == departmentId
                            && supervisorEmployeeIds.Contains(e.Id)
                            ) // only active employees
                .Select(e => new {
                    id = e.Id,
                    FirstName = e.FirstName + " " + e.LastName
                })
                .Distinct() // ensures unique combinations of id + FirstName
                .ToList();

            return Json(supervisors);
        }



        private async Task SendApprovalEmail(string receiver, int id)
        {
            var request = await _context.RequestInformation.Include(r => r.Guest).FirstOrDefaultAsync(r => r.Id == id); // Include Guest navigation property// Include Guest navigation propertyFindAsync(id);

            if (request == null)
            {
                // Handle the case where the request is not found
                throw new Exception("Request not found");
            }

            var subject = "Your Request has been Approved!";
            var message = $@"
    <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #e0f7fa; padding: 20px; border-radius: 10px;'>
        <h2 style='color: #00796b;'>Your Request has been Approved!</h2>
        <p>Dear {request.Guest.FirstName},</p>
        <p>We are pleased to inform you that your recent request has been approved. Here are the details of your request:</p>
        <ul>
            <li><strong>Request ID:</strong> {request.Id}</li>
            <li><strong>Request Date:</strong> {request.VisitDateTimeStart} </li>
            <li><strong>Request Description:</strong> {request.PurposeOfVisit}</li>
            <!-- Add any other relevant details here -->
        </ul>
        <p>Thank you for your patience and cooperation.</p>
        <p>If you have any questions or need further assistance, please do not hesitate to reach out to our support team.</p>
        <p>Best Regards,<br> {_current.Value.Name}, Ethiopia!</p>
    </div>";

            // Send email using IEmailSender
            await _emailSender.SendEmailAsync(receiver, subject, message);
        }


        public ActionResult GenerateQRCode(string firstName, string lastName, string email,int id)
        {
            var request = _context.RequestInformation
            .FirstOrDefault(r => r.Id == id);
            var guestId = request?.GuestId;
            var guest = _context.Guest.FirstOrDefault(x => x.Id == guestId);
            var today = DateTime.Today;
            if (request == null )  {
                return NotFound();
            }
            if (guest == null)
            {
                return NotFound();
            }
            if (guest.FirstName != firstName || guest.LastName != lastName || guest.Email != email || request.Status != "Approved" || request.VisitDateTimeEnd < today)
            {
                return NotFound();
            }

            try
            {
                // 1. Generate QR Code
                string userData = $"FirstName: {firstName}\nLastName: {lastName}\nEmail: {email}\nId: {id}";
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(userData, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);
                Bitmap qrCodeImage = qrCode.GetGraphic(20);

                // 2. Generate Dynamic Image URL
                string baseUrl = "https://localhost:44389"; // Get base URL
                string dynamicImageUrl = $"{baseUrl}/Guest/GetDynamicImage?firstName={firstName}&lastName={lastName}&email={email}&id={id}";

                // 3. Encode Dynamic Image URL in QR Code
                qrCodeData = qrGenerator.CreateQrCode(dynamicImageUrl, QRCodeGenerator.ECCLevel.Q);
                qrCode = new QRCode(qrCodeData);
                qrCodeImage = qrCode.GetGraphic(20);


                // 4. Return QR Code Image
                MemoryStream ms = new MemoryStream();
                qrCodeImage.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                return File(ms, "image/png");

            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                return View("Error");
            }
        }

        public ActionResult GetDynamicImage(int id)
        {
            var request = _context.RequestInformation.FirstOrDefault(r => r.Id == id);
            if (request == null) return NotFound();

            var guest = _context.Guest.FirstOrDefault(x => x.Id == request.GuestId);
            if (guest == null) return NotFound();

            // Setup Dimensions (Portrait Card)
            int width = 600;
            int height = 900;

            // Create Bitmap
            Bitmap image = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(image))
            {
                // 0. High Quality Settings
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // 1. Colors & Fonts
                Color primaryBlue = Color.FromArgb(13, 71, 161); // Deep Blue
                Color accentBlue = Color.FromArgb(227, 242, 253);  // Light Blue
                Color textDark = Color.FromArgb(33, 37, 41);
                Color textGray = Color.FromArgb(108, 117, 125);

                Font fontTitle = new Font("Segoe UI", 32, FontStyle.Bold); // "VISITOR"
                Font fontName = new Font("Segoe UI", 28, FontStyle.Bold);  // Guest Name
                Font fontLabel = new Font("Segoe UI", 14, FontStyle.Bold); // "Host:", "Date:"
                Font fontValue = new Font("Segoe UI", 16, FontStyle.Regular); // Actual values
                Font fontSmall = new Font("Segoe UI", 12, FontStyle.Regular); // Footer text

                // 2. Background
                g.Clear(Color.White);

                // 3. Draw Header (Blue Top Bar)
                int headerHeight = 180;
                Rectangle headerRect = new Rectangle(0, 0, width, headerHeight);
                using (SolidBrush headerBrush = new SolidBrush(primaryBlue))
                {
                    g.FillRectangle(headerBrush, headerRect);
                }

                // 4. Draw Logo (White Circle Container in Header)
                string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", _current.Value.LogoPath.TrimStart('/'));
                if (System.IO.File.Exists(logoPath))
                {
                    using (Image logo = Image.FromFile(logoPath))
                    {
                        int logoSize = 120;
                        int logoX = (width - logoSize) / 2;
                        int logoY = 20;

                        // Draw white circle behind logo
                        g.FillEllipse(Brushes.White, logoX - 5, logoY - 5, logoSize + 10, logoSize + 10);

                        // Draw Logo
                        g.DrawImage(logo, logoX, logoY, logoSize, logoSize);
                    }
                }

                // 5. Header Title "VISITOR PASS"
                StringFormat centerFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("VISITOR PASS", fontTitle, Brushes.White, new Rectangle(0, 140, width, 50), centerFormat);

                // 6. Visitor Name (Centered & Large)
                string visitorName = $"{guest.FirstName} {guest.LastName}";
                Rectangle nameRect = new Rectangle(20, 210, width - 40, 60);
                g.DrawString(visitorName, fontName, new SolidBrush(textDark), nameRect, centerFormat);

                // Underline the name
                using (Pen linePen = new Pen(primaryBlue, 3))
                {
                    g.DrawLine(linePen, 150, 275, width - 150, 275);
                }

                // 7. Details Table (Grid Layout)
                int startY = 320;
                int rowHeight = 50;
                int labelX = 80;
                int valueX = 220;

                DrawDetailRow(g, "Date:", DateTime.Now.ToString("MMM dd, yyyy"), startY, labelX, valueX, fontLabel, fontValue, textGray, textDark);
                DrawDetailRow(g, "Host:", $"{request.Employee?.FirstName} {request.Employee?.LastName}", startY + rowHeight, labelX, valueX, fontLabel, fontValue, textGray, textDark);
                DrawDetailRow(g, "Purpose:", request.PurposeOfVisit, startY + (rowHeight * 2), labelX, valueX, fontLabel, fontValue, textGray, textDark);
                DrawDetailRow(g, "Time:", $"{request.ApprovedDateTimeStart:HH:mm} - {request.ApprovedDateTimeEnd:HH:mm}", startY + (rowHeight * 3), labelX, valueX, fontLabel, fontValue, textGray, textDark);

                // 8. Generate & Draw QR Code (Bottom Center)
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode($"{id}", QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);
                Bitmap qrCodeImage = qrCode.GetGraphic(20); // Get raw high res

                int qrSize = 200;
                int qrX = (width - qrSize) / 2;
                int qrY = 600;
                g.DrawImage(qrCodeImage, qrX, qrY, qrSize, qrSize);

                // 9. Footer Warning
                string footerText = "Please return this badge to security\nbefore leaving the premises.";
                Rectangle footerRect = new Rectangle(0, 830, width, 70);
                using (SolidBrush footerBg = new SolidBrush(accentBlue))
                {
                    g.FillRectangle(footerBg, footerRect);
                }
                g.DrawString(footerText, fontSmall, new SolidBrush(primaryBlue), footerRect, centerFormat);

                // 10. Outer Border
                using (Pen borderPen = new Pen(primaryBlue, 10))
                {
                    borderPen.Alignment = PenAlignment.Inset;
                    g.DrawRectangle(borderPen, 0, 0, width, height);
                }
            }

            MemoryStream ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            return File(ms, "image/png");
        }

        // Helper method to keep code clean
        private void DrawDetailRow(Graphics g, string label, string value, int y, int labelX, int valueX, Font fLabel, Font fValue, Color cLabel, Color cValue)
        {
            g.DrawString(label, fLabel, new SolidBrush(cLabel), labelX, y);
            g.DrawString(value, fValue, new SolidBrush(cValue), valueX, y);
        }

        [HttpGet]
        public IActionResult CheckIn()
        {
            ViewBag.RequestType = new SelectList(_context.RequestType, "Id", "RequestTypeName");
            return View(nameof(CheckIn));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CheckIn(int requestId)
        {
            // Query the database for the request ID
           var request = _context.RequestInformation
    .Include(r => r.AdditionalGuests)
    .Include(r => r.Devices)
    .Include(r=>r.Department)

    .SingleOrDefault(r => r.Id == requestId);
            
            if (request != null && (request.VisitDateTimeEnd >= DateTime.Today) && (request.Status == "Approved"))
            {
                
                var guest = _context.Guest.SingleOrDefault(g => g.Id == request.GuestId);
                
                if (request.CompanyId!=_current.Value.Id)
                {
                    ViewBag.ErrorMessage = "Request ID not found.";
                    return View("CheckIn");
                }
                ViewBag.FirstName = guest.FirstName;
                ViewBag.LastName = guest.LastName;
                ViewBag.Company = guest.CompanyName;
                ViewBag.Email = guest.Email;
                ViewBag.Phone = guest.Phone;
                ViewBag.requestId = requestId;
                ViewBag.PurposeOfVisit = request.PurposeOfVisit;
                ViewBag.Feedback=request.Feedback;
                ViewBag.VisitDateTimeStart = request.VisitDateTimeStart;
                ViewBag.VisitDateTimeEnd = request.VisitDateTimeEnd;
                ViewBag.IsCheckedIn = request.IsCheckedIn;
                ViewBag.Department = request.Department?.Name;
                ViewBag.AdditionalGuests = request.AdditionalGuests;
                ViewBag.Devices = request.Devices;
                ViewBag.ApprovedTimeStart = request.ApprovedDateTimeStart;
                ViewBag.ApprovedTimeEnd = request.ApprovedDateTimeEnd;     

            }

            else if (request?.VisitDateTimeEnd < DateTime.Today)
            {
                ViewBag.ErrorMessage = "your visit date is expired.";
            }
            else
            {
                ViewBag.ErrorMessage = "Request ID not found.";
            }

            return View("CheckIn");
        }

        public async Task<IActionResult> Check(int checkId)
        {
            var request = await _context.RequestInformation
                .Include(r => r.Guest)
                .FirstOrDefaultAsync(r => r.Id == checkId);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Guest == null)
            {
                TempData["message"] = "Guest not found.";
                TempData["MessageType"] = "danger";
                return RedirectToAction(nameof(CheckIn));
            }

            if (!request.IsCheckedIn)
            {
                request.IsCheckedIn = true;

                _context.RequestInformation.Update(request);
                await _context.SaveChangesAsync();

                // ✅ ACTIVITY LOG
                await _guestActivityService.LogActivityAsync(
                    request.Guest.Id,
                    "Check In",
                    $"Guest {request.Guest.FirstName} {request.Guest.LastName} checked in"
                );

                TempData["message"] = "You are checked in successfully!";
                TempData["MessageType"] = "success";
            }

            return RedirectToAction(nameof(CheckIn));
        }



        public IActionResult CheckOut()
        {
            // ViewBag.RequestType is not used in the search/checkout display part,
            // so you might not need it here unless you have other functionality on this page.
            // ViewBag.RequestType = new SelectList(_context.RequestType, "Id", "RequestTypeName");
            return View(); // Directly return the view, not nameof(CheckOut)
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int requestId) // Make it async if you plan to await DB calls
        {
            var request = await _context.RequestInformation
                .Include(r => r.AdditionalGuests)
                .Include(r => r.Department)
                .Include(r => r.Devices)
                .Include(r => r.Guest)
                .Include(r => r.Company)// Include Guest to access guest details directly
                .SingleOrDefaultAsync(r => r.Id == requestId);

            // Reset feedback form visibility for initial search
            ViewBag.ShowFeedbackForm = false;

            if (request != null && request.Status == "Approved") // Removed date check here, as checkout can happen anytime for approved requests
            {
                // Check if the request's company ID matches the current company
                if (request.CompanyId != _current.Value.Id) // Using the new _currentCompanyId property
                {
                    ViewBag.ErrorMessage = "Request ID not found or does not belong to your company.";
                    return View("CheckOut");
                }

                // Populate ViewBag for display
                ViewBag.requestId = requestId;
                ViewBag.FirstName = request.Guest?.FirstName; // Use null-conditional operator
                ViewBag.LastName = request.Guest?.LastName;
                ViewBag.Email = request.Guest?.Email;
                ViewBag.Phone = request.Guest?.Phone;
                ViewBag.PurposeOfVisit = request.PurposeOfVisit;
                ViewBag.Feedback = request.Feedback; // This is the existing feedback property on RequestInformation
                ViewBag.ApprovedTimeStart = request.ApprovedDateTimeStart?.ToString("g"); // Using 'g' for general date/time pattern
                ViewBag.ApprovedTimeEnd = request.ApprovedDateTimeEnd?.ToString("g");
                ViewBag.Department = request.Department?.Name;
                ViewBag.AdditionalGuests = request.AdditionalGuests;
                ViewBag.Devices = request.Devices;
                ViewBag.IsCheckedIn = request.IsCheckedIn;
                ViewBag.Company = request.Company?.Name; // Pass the actual IsCheckedIn status

                // If the request is already checked out, set appropriate message
                if (!request.IsCheckedIn)
                {
                    ViewBag.SuccessMessage = "This request is already checked out.";
                }
            }
            else
            {
                ViewBag.ErrorMessage = "Request ID not found or not approved.";
            }

            return View("CheckOut");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Check_Out(int checkId)
        {
            var request = await _context.RequestInformation
                .Include(r => r.Guest)
                .SingleOrDefaultAsync(r => r.Id == checkId);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Request not found for checkout.";
                return RedirectToAction(nameof(CheckOut));
            }

            if (request.IsCheckedIn)
            {
                request.IsCheckedIn = false;
                _context.Update(request);
                await _context.SaveChangesAsync();

                if (request.Guest != null)
                {
                    await _guestActivityService.LogActivityAsync(request.Guest.Id, "Check Out", $"Guest {request.Guest.FirstName} {request.Guest.LastName} checked out for Request ID: {request.Id}");
                }

                TempData["SuccessMessage"] = "You are checked out successfully! Please provide your feedback.";

                var feedbackViewModel = new FeedbackViewModel
                {
                    RequestId = checkId,
                    GuestName = $"{request.Guest?.FirstName} {request.Guest?.LastName}".Trim(),
                    GuestEmail = request.Guest?.Email
                };

                // *** IMPORTANT CHANGE HERE: Serialize the object to JSON string ***
                TempData["FeedbackViewModel"] = JsonSerializer.Serialize(feedbackViewModel);

                return RedirectToAction("Feedback");
            }
            else
            {
                TempData["SuccessMessage"] = "This request is already checked out.";
                return RedirectToAction(nameof(CheckOut));
            }
        }
        [HttpGet]
        public IActionResult Feedback()
        {
            // *** IMPORTANT CHANGE HERE: Deserialize the JSON string back to object ***
            FeedbackViewModel? model = null; // Make it nullable

            if (TempData["FeedbackViewModel"] is string jsonModel)
            {
                try
                {
                    model = JsonSerializer.Deserialize<FeedbackViewModel>(jsonModel);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing FeedbackViewModel from TempData.");
                    TempData["ErrorMessage"] = "An error occurred while loading feedback data.";
                    return RedirectToAction(nameof(CheckOut));
                }
            }

            if (model == null || model.RequestId == 0) // Also check RequestId as a sanity check
            {
                TempData["ErrorMessage"] = "Invalid access to feedback page. Please search for a request and check out first.";
                return RedirectToAction(nameof(CheckOut));
            }
           
            return View(model);
        } // NEW ACTION: SubmitFeedback (POST - to handle feedback submission)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback([FromForm] FeedbackViewModel model) // Use FromForm to ensure binding
        {
            // Re-populate GuestName and GuestEmail from the model for the view in case of errors
            // The hidden fields will ensure these come back with the submission.

            if (ModelState.IsValid)
            {
                var feedback = new Feedback
                {
                    Name = model.Name,
                    Email = model.Email,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    CreatedAt = DateTime.Now,
                    CompanyId = _current.Value.Id, // Assuming you have a way to get the current company ID

                    // If you add RequestId to your Feedback model, uncomment this:
                    // RequestId = model.RequestId
                };

                _context.Feedback.Add(feedback);
                await _context.SaveChangesAsync();

                // Optional: If you want to link the feedback to the request directly (e.g., store feedback ID or summary)
                // var request = await _context.RequestInformation.FindAsync(model.RequestId);
                // if (request != null)
                // {
                //     // Example: Store the overall rating or last comment on the request itself
                //     // request.LastFeedbackRating = model.Rating;
                //     // request.LastFeedbackComment = model.Comment;
                //     // _context.Update(request);
                //     // await _context.SaveChangesAsync();
                // }

                ViewBag.SuccessMessage = "Thank you for your feedback! Your response has been recorded.";

                // You can optionally redirect to a "Thank You" page or back to the search page.
                // For now, let's just show the message on the feedback page itself.
                TempData["message"] = "your Feedback has been submitted successfully!";
                TempData["MessageType"] = "success";
                return View("CheckOut");
            }
            else
            {
                ViewBag.ErrorMessage = "Please correct the errors above.";
                return View("Feedback", model); // Stay on the feedback page to show error message
            }
        }
        private async Task SendInviterSms(int? requestId)
        {
            try
            {
                if (requestId == null)
                {
                    Console.WriteLine("Request ID is null, skipping SMS.");
                    return;
                }

                var request = await _context.RequestInformation
                                            .Include(r => r.Employee)
                                            .FirstOrDefaultAsync(r => r.Id == requestId.Value);

                if (request == null)
                {
                    Console.WriteLine($"No request found for ID {requestId}, skipping SMS.");
                    return;
                }

                var phoneNumber = request.Employee?.Phone;

                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    Console.WriteLine("Employee phone number missing, skipping SMS.");
                    return;
                }

                var message = $@"
Dear {request.Employee?.FirstName} {request.Employee?.LastName}, 
A Guest with visit request ID: {request.Id} is waiting for visit approval. 
Visit Date: {request.VisitDateTimeStart:yyyy-MM-dd} 
Best Regards, 
{_current.Value.Slug.ToUpper()}, Ethiopia!";

                await _smsService.SendSmsAsync(phoneNumber, message);
            }
            catch (Exception ex)
            {
                // Swallow exception, just log
                Console.WriteLine($"Failed to send SMS: {ex.Message}");
            }
        }


        private async Task SendInviterEmail(string receiver, int id)
        {
            var request = await _context.RequestInformation.Include(r => r.Employee).FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
            {
                throw new Exception("Request not found");
            }

            var subject = "Visitor is Waiting for Approval!";

            var messageBody = $@"
Dear {request.Employee?.FirstName} {request.Employee?.LastName}, 
A Guest with visit request ID: {request.Id} 
is waiting for visit approval. 
Visit Date: {request.VisitDateTimeStart:yyyy-MM-dd} 
Best Regards,
{_current.Value.Name}, Ethiopia!";


        }
    }
}
