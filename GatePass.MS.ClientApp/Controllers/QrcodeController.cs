using QRCoder;
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



public class QrcodeController : Controller
{
   

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
            string baseUrl = "https://localhost:44389"; // Get base URL
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

    public ActionResult GetDynamicImage(string firstName, string lastName, string email)
    {
        try
        {
            // 1. Create a Bitmap object
            Bitmap image = new Bitmap(500, 300);

            // 2. Get graphics object
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // 3. Fill background color (optional)
                graphics.FillRectangle(Brushes.LightGray, 0, 0, image.Width, image.Height);

                // 4. Define font and brush
                Font font = new Font("Arial", 18, FontStyle.Bold);
                Brush brush = Brushes.Black;

                // 5. Draw text (adjust positions as needed)
                graphics.DrawString($"First Name: {firstName}", font, brush, 50, 50);
                graphics.DrawString($"Last Name: {lastName}", font, brush, 50, 100);
                graphics.DrawString($"Email: {email}", font, brush, 50, 150);

                // 6. Add your design elements here (e.g., logo, borders, etc.)
                // ...

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
}