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

using GatePass.MS.Domain.ViewModels;

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Identity.UI.Services;
using System.Text.Encodings.Web;
using GatePass.MS.Application;
using GatePass.MS.ClientApp.Service;

using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Net.Mail;
using System.Net;
using GatePass.MS.ClientApp.Migrations;
using SkiaSharp;
using ExcelDataReader;
using System.Text;
namespace GatePass.MS.ClientApp.Controllers
{


    [Authorize]
    public class RequestInformationsController : Controller
    {
        public readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IFileService _fileService;
        private readonly IWebHostEnvironment _environment;
        private readonly ReportService _activityService;
        private readonly ICurrentCompany _current;

        private readonly SmsService _smsService;


        public RequestInformationsController(ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
                    ICurrentCompany current,

        ReportService activityService,
        IWebHostEnvironment webHostEnvironment,
        IFileService fileService,

        SmsService smsService)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _context = context;
            _current = current;
            _activityService = activityService;

            _fileService = fileService;
            _environment = webHostEnvironment;

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

            var employee = _context.Employee.SingleOrDefault(e => e.Id == employeeId);
            var departmentId = employee?.DepartmentId;
            var query = _context.RequestInformation.Where(r => r.CompanyId == _current.Value.Id).AsQueryable();
            var isInSupervisorRole = await _userManager.IsInRoleAsync(currentUser, "Superviser");
            var isInAdminRole = await _userManager.IsInRoleAsync(currentUser, "Admin");
            var isInGatekeeperRole = await _userManager.IsInRoleAsync(currentUser, "Gatekeeper");


            var today = DateTime.Today;


            query = query.Where(r => ((isInAdminRole) ||
            (r.Status == "Approved" && isInGatekeeperRole) ||
            (r.EmployeeId == currentUser.EmployeeId) ||
            ((r.DepartmentId == departmentId) && isInSupervisorRole) ||
            currentUser.UserName == "superUser@gmail.com") && r.VisitDateTimeEnd >= today);


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
        public IActionResult Invite()
        {
            ViewBag.DepartmentId = 28;
            ViewBag.RequestType = new SelectList(_context.RequestType, "Id", "RequestTypeName");
            return View(nameof(Invite));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Invite(RequestInformationViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var employeeId = currentUser?.EmployeeId;

            var employee = _context.Employee.SingleOrDefault(e => e.Id == employeeId);
            var departmentId = employee?.DepartmentId;
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
                EmployeeId = employeeId,
                PurposeOfVisit = model.PurposeOfVisit,
                CompanyId = _current.Value.Id,
                DepartmentId = departmentId,
                IsIndividual = model.IsIndividual,
                ImageData = model.ImageData,
                GuestId = guest.Id,
                AdditionalGuests = model.AdditionalGuests.Select(g => new Guest
                {
                    FirstName = g.FirstName,
                    LastName = g.LastName,
                    Email = g.Email,
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
            TempData["message"] = "Request has been Sent successfully.";
            TempData["MessageType"] = "success";



            return RedirectToAction(nameof(Invite));

        }
        // GET: Invitations/ApproveDisapprove/5
        [HttpGet]
        public async Task<IActionResult> ApproveDisapprove(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestInformation = await _context.RequestInformation
               .Include(r => r.Approver)
                   .ThenInclude(r => r.Employee)
              .Include(r => r.Employee)
               .Include(r => r.Department)
              .Include(r => r.Guest) // Include Guest navigation property
              .Include(r => r.Attachments)
              .Include(d => d.Devices)
                .Include(g => g.AdditionalGuests)
              .FirstOrDefaultAsync(r => r.Id == id);
            if (requestInformation == null)
            {
                return NotFound();
            }


            RequestInformationViewDetail requestInformationViewDetail = new RequestInformationViewDetail()
            {
                Id = requestInformation.Id,
                GuestFirstName = requestInformation.Guest.FirstName,
                GuestPhotoPath = requestInformation.Guest.GuestPhotoPath,
                GuestLastName = requestInformation.Guest.LastName,
                CompanyName = requestInformation.Guest.CompanyName,
                GuestEmail = requestInformation.Guest.Email,
                GuestPhoneNumber = requestInformation.Guest.Phone,
                ApprovedDateTimeStart = requestInformation.ApprovedDateTimeStart,
                ApprovedDateTimeEnd = requestInformation.ApprovedDateTimeEnd,

                PurposeOfVisit = requestInformation.PurposeOfVisit,
                InviterId = requestInformation.Employee != null ? requestInformation.Employee.Id : null,
                Status = requestInformation.Status,
                GuestId = requestInformation.GuestId ?? 0,
                Approver = requestInformation.Approver?.Employee != null
    ? requestInformation.Approver.Employee.FirstName + " " + requestInformation.Approver.Employee.LastName
    : "N/A",
                Attachments = requestInformation.Attachments,
                VisitDateTimeStart = requestInformation.VisitDateTimeStart,
                VisitDateTimeEnd = requestInformation.VisitDateTimeEnd,
                IsIndividual = requestInformation.IsIndividual,
                IsCheckedIn = requestInformation.IsCheckedIn,
                GuestDestinationDepartment = requestInformation.Department?.Name,
                Devices = requestInformation.Devices.ToList(), // <--- This is the change
                AdditionalGuests = requestInformation.AdditionalGuests,
            };

            return View(requestInformationViewDetail);
        }


        // POST: RequestInformation/ApproveDisapprove/5
        // POST: RequestInformation/ApproveDisapprove/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDisapprove(RequestInformationViewDetail model)
        {
            // You should add ModelState.IsValid check here if you have validation rules
            // if (!ModelState.IsValid)
            // {
            //     // Re-fetch necessary data for the view (e.g., dropdowns, attachments)
            //     // and return View(model);
            // }

            var currentUser = await _userManager.GetUserAsync(User);
            var request = await _context.RequestInformation
                                        .Include(r => r.Devices) // Crucial: Include Devices to update existing ones
                                        .FirstOrDefaultAsync(r => r.Id == model.Id);

            if (request == null)
            {
                return NotFound(); // Request not found
            }

            var guestId = request.GuestId;
            var guest = _context.Guest.FirstOrDefault(x => x.Id == guestId);

            // Handle overall request approval/disapproval
            if (model.Approve)
            {
                request.Status = "Approved";
                request.ApproverId = currentUser?.Id;
                request.Feedback = model.Feedback;
                request.ApprovedDateTimeStart = model.ApprovedDateTimeStart;
                request.ApprovedDateTimeEnd = model.ApprovedDateTimeEnd;

                // Update device statuses
                foreach (var submittedDevice in model.Devices)
                {
                    var existingDevice = request.Devices.FirstOrDefault(d => d.DeviceId == submittedDevice.DeviceId);
                    if (existingDevice != null)
                    {
                        // If a checkbox was checked, submittedDevice.IsGranted/IsDenied will be true.
                        // If unchecked, it might be false or null depending on browser submission.
                        // We handle null here by assuming false if not explicitly true.
                        existingDevice.IsGranted = submittedDevice.IsGranted;
                        existingDevice.IsDenied = submittedDevice.IsDenied;
                    }
                }

                _context.RequestInformation.Update(request);
                await _context.SaveChangesAsync();

                // Your existing email/SMS sending logic
                try
                {
                    string baseUrl = "https://localhost:44389";
                    string Qrcode = $"{baseUrl}/Guest/GenerateQRCode?firstName={guest?.FirstName}&lastName={guest?.LastName}&email={guest?.Email}&id={model.Id}";
                    await SendApprovalEmail(model.GuestEmail, model.Id);
                    TempData["message"] = "Invitation has been approved successfully!";
                    TempData["MessageType"] = "success";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    TempData["message"] = "Invitation has been approved, but there was an error sending the approval email.";
                    TempData["MessageType"] = "warning";
                }

                try
                {
                    await SendApprovalSms(model.Id);
                    TempData["message"] = "Invitation has been approved successfully. SMS and Email sent!";
                    TempData["MessageType"] = "success"; // This might overwrite the previous message if email failed
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    TempData["message"] = "Invitation has been approved, but there was an error sending the approval sms.";
                    TempData["MessageType"] = "warning"; // This might overwrite the previous message
                }

                return RedirectToAction(nameof(Index));
            }
            else if (model.Disapprove)
            {
                request.Status = "Rejected";
                request.ApproverId = currentUser?.Id;
                request.Feedback = model.Feedback; // You might want to capture feedback for rejection too
                request.ApprovedDateTimeStart = null; // Clear if rejected
                request.ApprovedDateTimeEnd = null; // Clear if rejected

                // Update device statuses (optional for rejection, but good for consistency)
                foreach (var submittedDevice in model.Devices)
                {
                    var existingDevice = request.Devices.FirstOrDefault(d => d.DeviceId == submittedDevice.DeviceId);
                    if (existingDevice != null)
                    {
                        existingDevice.IsGranted = submittedDevice.IsGranted;
                        existingDevice.IsDenied = submittedDevice.IsDenied;
                    }
                }

                _context.RequestInformation.Update(request);
                await _context.SaveChangesAsync();

                // Your existing email/SMS sending logic
                try
                {
                    await SendDisApprovalEmail(model.GuestEmail, model.Id, model.SelectedReason);
                    TempData["message"] = "Invitation has been Rejected!";
                    TempData["MessageType"] = "error";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    TempData["message"] = "Invitation has been Rejected, but there was an error sending the Rejection email.";
                    TempData["MessageType"] = "warning";
                }

                try
                {
                    await SendDisApprovalSms(model.Id, model.SelectedReason);
                    TempData["message"] = "Invitation has been Rejected successfully. SMS and Email sent!";
                    TempData["MessageType"] = "success"; // This might overwrite the previous message
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    TempData["message"] = "Invitation has been Rejected, but there was an error sending the Rejection sms.";
                    TempData["MessageType"] = "warning";
                }

                return RedirectToAction(nameof(Index));
            }
            else if (model.Review)
            {
                request.Status = "Reviewed";
                request.ApproverId = currentUser?.Id;
                request.Feedback = model.Feedback; // You might want to capture feedback for rejection too
                request.ApprovedDateTimeStart = null; // Clear if rejected
                request.ApprovedDateTimeEnd = null; // Clear if rejected

                // Update device statuses (optional for rejection, but good for consistency)
                foreach (var submittedDevice in model.Devices)
                {
                    var existingDevice = request.Devices.FirstOrDefault(d => d.DeviceId == submittedDevice.DeviceId);
                    if (existingDevice != null)
                    {
                        existingDevice.IsGranted = submittedDevice.IsGranted;
                        existingDevice.IsDenied = submittedDevice.IsDenied;
                    }
                }

                _context.RequestInformation.Update(request);
                await _context.SaveChangesAsync();
                TempData["message"] = "Invitation has been Reviewed successfully!";
                TempData["MessageType"] = "success";
                return RedirectToAction(nameof(Index));
            }

            // If neither Approve nor Disapprove was clicked, re-render the view
            // You might want to re-fetch related data for the view model if validation fails
            // or if the model state is otherwise invalid.
            return View(model);
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var request = await _context.RequestInformation.FirstOrDefaultAsync(m => m.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            return PartialView("_Delete", request);
        }

        public async Task<IActionResult> DeleteConfirmed(int id)
        {

            var request = await _context.RequestInformation.FindAsync(id);
            if (request != null)
            {
                _context.RequestInformation.Remove(request);
            }
            await _context.SaveChangesAsync();
            TempData["message"] = "Request Deleted successfully!";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Index));
        }



        [HttpPost]
        public async Task<IActionResult> HandleSmsAction(string smsAction, int requestId)
        {
            // Initialize message and message type
            string notificationMessage = "";
            string messageType = "info";  // Default message type

            if (smsAction == "CheckStatus")
            {
                try
                {
                    // Handle logic for checking SMS delivery status
                    var request = await _context.RequestInformation
                        .Include(r => r.Guest)
                        .FirstOrDefaultAsync(r => r.Id == requestId);

                    if (request == null)
                    {
                        // Handle the case where the request is not found
                        throw new Exception("Request not found");
                    }

                    var response = await _smsService.GetDeliveryStatusAsync(request.Guest.Phone);

                    // If SMS delivery status is successfully retrieved
                    notificationMessage = $"Delivery Status: {response}";
                    messageType = "info";
                }
                catch (Exception ex)
                {
                    // Log exception (optional)
                    Console.WriteLine(ex.Message);

                    // Handle failure in checking SMS status
                    notificationMessage = "Cannot connect to the SMS service endpoint";
                    messageType = "error";
                }
            }
            else if (smsAction == "ResendSms")
            {
                try
                {
                    // Handle logic for resending the SMS
                    await SendApprovalSms(requestId);

                    // If SMS was successfully sent
                    notificationMessage = "SMS sent successfully!";
                    messageType = "success";
                }
                catch (Exception ex)
                {
                    // Log exception (optional)
                    Console.WriteLine(ex.Message);

                    // Handle failure in resending SMS
                    notificationMessage = "Failed to resend the SMS.";
                    messageType = "warning";
                }
            }

            // Set TempData for notification
            TempData["message"] = notificationMessage;
            TempData["MessageType"] = messageType;

            // Redirect back to the ApproveDisapprove action with the request ID
            return RedirectToAction("ApproveDisapprove", new { id = requestId });
        }


        private async Task SendApprovalSms(int requestId)
        {
            var request = await _context.RequestInformation
                                        .Include(r => r.Guest)
                                        .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                throw new Exception("Request not found");

            var guest = request.Guest;
            var phone = guest.Phone;
            var start = request.ApprovedDateTimeStart?.ToString("yyyy-MM-dd HH:mm");
            var end = request.ApprovedDateTimeEnd?.ToString("yyyy-MM-dd HH:mm");
            var company = _current.Value.Name;
            var slug = _current.Value.Slug.ToUpper();

            // ✅ Capture selected language
            var selectedLanguage = Request.Form["SelectedLanguageHidden"].ToString().Trim().ToLower();

            string message;

            switch (selectedLanguage)
            {
                case "am": // 🇪🇹 Amharic
                    message = $@"
ውድ {guest.FirstName} {guest.LastName}፣
በተቋማችን {slug} ለመስተናገድ የመግቢያ ፍቃድ ጥያቄዎ ተቀባይነት አግኝቷል::
• የፍቃድ ቁጥር፡ {request.Id}
• የፍቃድ ጊዜ፡ ከ {start} እስከ {end}
እናመሰግናለን፣
{slug}, ኢትዮጵያ።";
                    break;

                case "om": // 🇪🇹 Afan Oromo
                    message = $@"
Obboleessa/Obboleetti {guest.FirstName} {guest.LastName},
Hayyama daawwannaa keessan gara {slug} ni mirkanaa'eera!
• Lakkoofsa Hayyamaa: {request.Id}
• Yeroo Daawwannaa: {start} – {end}
Galatoomaa,
{slug}, Itoophiyaa!";
                    break;

                default: // 🇬🇧 English (default fallback)
                    message = $@"
Dear {guest.FirstName} {guest.LastName},
Your visit request to {slug} has been approved!
• Request ID: {request.Id}
• Visit Time: From {start} To {end}
Best Regards,
{slug}, Ethiopia!";
                    break;
            }

            await _smsService.SendSmsAsync(phone, message);
        }


        private async Task SendDisApprovalSms(int requestId, string selectedReason)
        {
            var request = await _context.RequestInformation
                                        .Include(r => r.Guest)
                                        .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                throw new Exception("Request not found");

            var guest = request.Guest;
            var phone = guest.Phone;
            var company = _current.Value.Name;
            var slug = _current.Value.Slug.ToUpper();

            // ✅ Capture selected language
            var selectedLanguage = Request.Form["SelectedLanguageHidden"].ToString().Trim().ToLower();

            string message;

            switch (selectedLanguage)
            {
                case "am": // 🇪🇹 Amharic
                    message = $@"
ውድ {guest.FirstName} {guest.LastName}፣
በፍቃድ ጥያቄ ቁጥር፡ {request.Id}
በተቋማችን {slug} ለመስተናገድ ያቀረቡት የመግቢያ ፍቃድ ጥያቄዎ በ ({selectedReason}) ምክንያት ለጊዜው ተቀባይነት አላገኘም።
እባክዎ ሌላ ጊዜ ይሞክሩ።
እናመሰግናለን፣
{slug}, ኢትዮጵያ።";
                    break;

                case "om": // 🇪🇹 Afan Oromo
                    message = $@"
Obboleessa/Obboleetti {guest.FirstName} {guest.LastName},
Hayyama daawwannaa keessan (Lakkoofsa: {request.Id}) gara {slug} sababa ({selectedReason}) tiin hin mirkanaa'in.
Mee yeroo biraatti yaalaa.
Galatoomaa,
{slug}, Itoophiyaa!";
                    break;

                default: // 🇬🇧 English (default fallback)
                    message = $@"
Dear {guest.FirstName} {guest.LastName},
Visit request ID: {request.Id}
Your visit request to {slug} has been rejected because of {selectedReason}.
Please try another time.
Best Regards,
{slug}, Ethiopia!";
                    break;
            }

            await _smsService.SendSmsAsync(phone, message);
        }


        private async Task SendApprovalEmail(string receiver, int id)
        {
            var request = await _context.RequestInformation.Include(r => r.Guest).FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
            {
                throw new Exception("Request not found");
            }

            var subject = "Your Request has been Approved!";
            var messageBody = $@"
<div style=""font-family:Arial, Helvetica, sans-serif; max-width:600px; margin:auto; background-color:#f9fafb; border-radius:10px; overflow:hidden; border:1px solid #e0e0e0;"">

  <!-- Header -->
  <div style=""background-color:#005B5B; color:#ffffff; padding:20px; text-align:center;"">
    <h2 style=""margin:0; font-size:22px; letter-spacing:0.5px;"">Visit Request Approved</h2>
  </div>

  <!-- Body -->
  <div style=""padding:25px; color:#333333; background-color:#ffffff;"">
    <h3 style=""color:#008C7A; margin-top:0;"">Dear, {request.Guest.FirstName}!</h3>
    <p style=""font-size:15px; line-height:1.6; margin-bottom:20px;"">
      Your visit request to <strong>{_current.Value.Slug.ToUpper()}</strong> has been 
      <span style=""color:#28A745; font-weight:bold;"">approved</span>.
    </p>

    <!-- Details Table -->
    <table style=""width:100%; border-collapse:collapse; margin:20px 0; font-size:14px;"">
      <tr style=""background-color:#f4fdf6;"">
        <td style=""padding:10px; border:1px solid #dce8dc; width:40%;""><strong>Purpose of Visit</strong></td>
        <td style=""padding:10px; border:1px solid #dce8dc;"">{request.PurposeOfVisit}</td>
      </tr>
      <tr>
        <td style=""padding:10px; border:1px solid #dce8dc;""><strong>Approved Time Window</strong></td>
        <td style=""padding:10px; border:1px solid #dce8dc;"">
          {request.ApprovedDateTimeStart:yyyy-MM-dd HH:mm} – {request.ApprovedDateTimeEnd:yyyy-MM-dd HH:mm}
        </td>
      </tr>
    </table>

    <p style=""font-size:15px; line-height:1.6; margin-top:15px;"">
      Please arrive within the approved timeframe. Upon arrival, present the attached QR code to reception for verification.
    </p>

    <p style=""margin-top:30px; font-size:15px;"">We look forward to welcoming you!</p>
    <p style=""font-weight:bold; color:#005B5B; margin-bottom:0;"">{_current.Value.Name}, Ethiopia</p>
  </div>

  <!-- Footer -->
  <div style=""background-color:#f0fdf4; text-align:center; padding:15px; font-size:12px; color:#4d805d;"">
    This message was sent automatically. Please do not reply.
  </div>

</div>";



            // Generate QR Code data
            string userData = $"{request.Id}";
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(userData, QRCodeGenerator.ECCLevel.Q);

                // Render QR code using SkiaSharp
                int pixelsPerModule = 20; // Size of each QR code module
                int qrCodeSize = qrCodeData.ModuleMatrix.Count * pixelsPerModule;

                using (var surface = SKSurface.Create(new SKImageInfo(qrCodeSize, qrCodeSize)))
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.White);

                    var moduleMatrix = qrCodeData.ModuleMatrix;
                    if (moduleMatrix != null)
                    {
                        using (var paint = new SKPaint
                        {
                            Color = SKColors.Black,
                            Style = SKPaintStyle.Fill
                        })
                        {
                            for (int y = 0; y < moduleMatrix.Count; y++)
                            {
                                for (int x = 0; x < moduleMatrix[y].Count; x++)
                                {
                                    if (moduleMatrix[y][x])
                                    {
                                        canvas.DrawRect(new SKRect(x * pixelsPerModule, y * pixelsPerModule, (x + 1) * pixelsPerModule, (y + 1) * pixelsPerModule), paint);
                                    }
                                }
                            }
                        }
                    }

                    canvas.Flush();

                    // Save QR code as a PNG
                    using (var ms = new MemoryStream())
                    {
                        surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).SaveTo(ms);
                        ms.Position = 0;

                        // Create QR code attachment
                        var qrCodeAttachment = new System.Net.Mail.Attachment(ms, "QRCode.png", "image/png");

                        // Send the email with the attachment
                        await _emailSender.SendEmailWithAttachementAsync(receiver, subject, messageBody, qrCodeAttachment);
                    }
                }
            }
        }

        private async Task SendDisApprovalEmail(string receiver, int id, string selectedReason)
        {
            var request = await _context.RequestInformation.Include(r => r.Guest).FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                throw new Exception("Request not found");
            }

            var subject = "Your Request has been Rejected!";
            var messageBody = $@"
<div style=""font-family:Arial, Helvetica, sans-serif; max-width:600px; margin:auto; background-color:#f9fafb; border-radius:10px; overflow:hidden; border:1px solid #e0e0e0;"">

  <!-- Header -->
  <div style=""background-color:#00796B; color:#ffffff; padding:20px; text-align:center;"">
    <h2 style=""margin:0; font-size:22px;"">Visit Request Update</h2>
  </div>

  <!-- Body -->
  <div style=""padding:25px; color:#333333; background-color:#ffffff;"">
   
    <p style=""font-size:15px; line-height:1.6;"">Dear {request.Guest.FirstName},</p>
    <p style=""font-size:15px; line-height:1.6;"">
      We regret to inform you that your visit request to 
      <strong>{_current.Value.Slug.ToUpper()}</strong> has been 
      <span style=""color:#C62828; font-weight:bold;"">rejected</span>.
    </p>

    <!-- Request Details -->
    <table style=""width:100%; border-collapse:collapse; margin:20px 0; font-size:14px;"">
      <tr style=""background-color:#f5f5f5;"">
        <td style=""padding:10px; border:1px solid #ddd; width:40%;""><strong>Request ID</strong></td>
        <td style=""padding:10px; border:1px solid #ddd;"">{request.Id}</td>
      </tr>
      <tr>
        <td style=""padding:10px; border:1px solid #ddd;""><strong>Request Date</strong></td>
        <td style=""padding:10px; border:1px solid #ddd;"">{request.VisitDateTimeStart:yyyy-MM-dd}</td>
      </tr>
      <tr style=""background-color:#f5f5f5;"">
        <td style=""padding:10px; border:1px solid #ddd;""><strong>Purpose of Visit</strong></td>
        <td style=""padding:10px; border:1px solid #ddd;"">{request.PurposeOfVisit}</td>
      </tr>
      <tr>
        <td style=""padding:10px; border:1px solid #ddd;""><strong>Reason for Rejection</strong></td>
        <td style=""padding:10px; border:1px solid #ddd;"">{selectedReason}</td>
      </tr>
    </table>

    <p style=""font-size:15px; line-height:1.6;"">
      We encourage you to review your request details and consider resubmitting at a later time.
    </p>

    <p style=""margin-top:30px; font-size:15px;"">Best Regards,</p>
    <p style=""font-weight:bold; color:#00796B; margin-bottom:0;"">{_current.Value.Slug.ToUpper()}, Ethiopia</p>
  </div>

  <!-- Footer -->
  <div style=""background-color:#e0f7fa; text-align:center; padding:15px; font-size:12px; color:#00695c;"">
    This is an automated message — please do not reply directly.
  </div>

</div>";


            await _emailSender.SendEmailAsync(receiver, subject, messageBody);

        }



        [HttpPost]
        public ActionResult GenerateQRCode(string firstName, string lastName, string email)
        {
            try
            {
                // 1. Generate QR Code
                string userData = $"FirstName: {firstName}\nLastName: {lastName}\nEmail: {email}";
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(userData, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);
                Bitmap qrCodeImage = qrCode.GetGraphic(20);

                // 2. Generate Dynamic Image URL
                string baseUrl = "https://localhost:44389/"; // Get base URL
                string dynamicImageUrl = $"{baseUrl}/Home/GetDynamicImage?firstName={firstName}&lastName={lastName}&email={email}";

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

        public ActionResult GetDynamicImage(string firstName, string lastName, string email, int id)
        {
            var request = _context.RequestInformation
             .FirstOrDefault(r => r.Id == id);
            var guestId = request?.GuestId;
            var guest = _context.Guest.FirstOrDefault(x => x.Id == guestId);
            var today = DateTime.Today;

            if (request == null)
            {
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
                // 1. Create a Bitmap object
                Bitmap image = new Bitmap(500, 500);

                // 2. Get graphics object
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    // 3. Fill background color (optional)
                    graphics.FillRectangle(Brushes.LightGray, 0, 0, image.Width, image.Height);

                    // 4. Define font and brush
                    Font font = new Font("Arial", 13, FontStyle.Bold);
                    Brush brush = Brushes.CadetBlue;
                    // 6. Draw the logo from the file path
                    // 5. Draw text (adjust positions as needed)
                    string logoFilePath = _current.Value.LogoPath; // Replace with the actual path to your logo image
                    Image logoImage = Image.FromFile(logoFilePath);
                    // 7. Resize the logo image (e.g., to 100x100 pixels)
                    int desiredWidth = 110;
                    int desiredHeight = 110;
                    Bitmap resizedLogo = new Bitmap(logoImage, desiredWidth, desiredHeight);
                    // 7. Draw the logo image onto the Bitmap
                    int logoX = 370; // Adjust the X-coordinate as needed
                    int logoY = 10; // Adjust the Y-coordinate as needed

                    string userData = $"{id}";
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
                    desiredWidth = 140;
                    desiredHeight = 140;
                    Bitmap resizedQrcode = new Bitmap(qrCodeImage, desiredWidth, desiredHeight);

                    // Create Bitmap object for guest information

                    // Draw QR code (adjust position as needed)
                    var qrCodeX = 330;
                    var qrCodeY = 270;
                    graphics.DrawImage(resizedQrcode, qrCodeX, qrCodeY);
                    graphics.DrawImage(resizedLogo, logoX, logoY);
                    // 6. Add your design elements here (e.g., logo, borders, etc.)
                    // ...
                    graphics.DrawString(_current.Value.Name, font, brush, 20, 60);
                    graphics.DrawString($"Call:8181 Email:{_current.Value.Email}", font, brush, 20, 90);
                    graphics.DrawString(_current.Value.website, font, brush, 20, 120);
                    graphics.DrawString($"Addis Ababa,Ethiopia", font, brush, 20, 150);

                    brush = Brushes.Black;
                    graphics.DrawString($"Full Name: {firstName} {lastName} ", font, brush, 20, 190);
                    graphics.DrawString($"Email: {email}", font, brush, 20, 230);
                    graphics.DrawString($"Purpose of Visit: {request.PurposeOfVisit}", font, brush, 20, 270);
                    graphics.DrawString($"aprroved time window : {request.ApprovedDateTimeStart}-{request.ApprovedDateTimeEnd}", font, brush, 20, 310);
                    graphics.DrawString($"Phone Number: {guest.Phone}", font, brush, 20, 390);
                    brush = Brushes.Red;

                    graphics.DrawString($"Please Return Badge Before Leaving", font, brush, 20, 450);
                    // 6. Load the logo image from the local file path

                }




                // 7. Save image to memory stream
                MemoryStream ms = new MemoryStream();
                image.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                // 8. Return image as file result
                return File(ms, "image/png");
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                return View("Error");
            }
        }
        public ActionResult GetImage(int id)
        {
            var request = _context.RequestInformation.Find(id);
            if (request != null && request.ImageData != null)
            {
                return File(request.ImageData, "image/png");
            }
            return NotFound();
        }

        //public async Task<IActionResult> Invite(RequestInformationViewModel model)
        //{       // Message accumulation to track status
        //    string notificationMessage = "Invitation Request Sent Successfully! ";
        //    bool smsSentSuccessfully = true;
        //    bool emailSentSuccessfully = true;
        //    string SmsDeliveryStatus;
        //    var currentUser = await _userManager.GetUserAsync(User);
        //    var isInSupervisorRole = await _userManager.IsInRoleAsync(currentUser, "Superviser");
        //    Console.WriteLine(model.DepartmentId);
        //    if (ModelState.IsValid)
        //    {
        //        string fullName = $"{model.Title} {model.GuestFirstName}";

        //        Guest guestInfo = new Guest
        //        {
        //            FirstName = fullName,
        //            LastName = string.IsNullOrEmpty(model.GuestLastName) ? null : model.GuestLastName,
        //            CompanyName = string.IsNullOrEmpty(model.CompanyName) ? null : model.CompanyName,
        //            Email = model.GuestEmail,
        //            Phone = model.GuestPhoneNumber,
        //        };

        //        await _context.Guest.AddAsync(guestInfo);
        //        await _context.SaveChangesAsync();
        //        var employeeId = currentUser?.EmployeeId;
        //        var employee = _context.Employee.SingleOrDefault(e => e.Id == employeeId);
        //        try
        //        {

        //            // 1. Create Bitmap
        //            Bitmap image = new Bitmap(500, 300);

        //            // 2. Get Graphics Object
        //            using (Graphics graphics = Graphics.FromImage(image))
        //            {
        //                graphics.FillRectangle(Brushes.LightGray, 0, 0, image.Width, image.Height);

        //                // Define Font & Brush
        //                Font font = new Font("Arial", 18, FontStyle.Bold);
        //                Brush brush = Brushes.Black;

        //                // Draw text
        //                graphics.DrawString($"First Name: {model.GuestFirstName}", font, brush, 50, 50);
        //                graphics.DrawString($"Last Name: {model.GuestLastName}", font, brush, 50, 100);
        //                graphics.DrawString($"Email: {model.GuestEmail}", font, brush, 50, 150);
        //            }

        //            // 3. Convert to byte array
        //            using (MemoryStream ms = new MemoryStream())
        //            {
        //                image.Save(ms, ImageFormat.Png);
        //                model.ImageData = ms.ToArray(); // Store image as byte[]
        //            }

        //            // 4. Save to database



        //        }
        //        catch (Exception ex)
        //        {
        //            ModelState.AddModelError("", "Error: " + ex.Message);
        //        }
        //        // Create request information
        //        RequestInformation requestInformation = new RequestInformation
        //        {

        //            VisitDateTimeStart = model.VisitDateTimeStart,
        //            VisitDateTimeEnd = model.VisitDateTimeEnd,
        //            PurposeOfVisit = model.PurposeOfVisit,
        //            EmployeeId = currentUser?.EmployeeId,
        //            IsIndividual = model.IsIndividual,
        //            DepartmentId = employee?.DepartmentId,
        //            Guest = guestInfo,
        //            ImageData = model.ImageData,
        //            Devices = model.Devices.Select(d => new Device
        //            {
        //                DeviceName = d.DeviceName,
        //                Identifier = d.Identifier,
        //                Description = d.Description,
        //            }).ToList(),
        //        };

        //        _context.RequestInformation.Add(requestInformation);
        //        await _context.SaveChangesAsync();






        //        // Approve the request automatically if user is superviser


        //        // Set the final message in TempData
        //        TempData["message"] = notificationMessage;
        //        TempData["MessageType"] = "success";

        //        return RedirectToAction(nameof(Index));
        //    }
        //    ViewBag.RequestType = new SelectList(_context.RequestType, "Id", "RequestTypeName", model.RequestTypeId);
        //    return View(model);
        //}

        private async Task SendInviterSms(int requestId)
        {
            var request = await _context.RequestInformation
                                        .Include(r => r.Employee)
                                        .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                throw new Exception("Request not found");
            }

            var phoneNumber = request.Employee?.Phone;
            // Construct SMS message
            var message = $@"
Dear {request.Employee.FirstName} {request.Employee.LastName}, 
A Guest with visit request ID: {request.Id} is waiting for visit approval. 
Visit Date: {request.VisitDateTimeStart:yyyy-MM-dd} 
Best Regards, 
{_current.Value.Slug.ToUpper()}, Ethiopia!";

            // Send SMS using SmsService
            await _smsService.SendSmsAsync(phoneNumber, message);

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
            A Guest with visit request ID: {request.Id} is waiting for visit approval.
            Visit Date: {request.VisitDateTimeStart.ToString("yyyy-MM-dd")}
            Best Regards, 
            {_current.Value.Name} Ethiopia!";

        }
    }
}
