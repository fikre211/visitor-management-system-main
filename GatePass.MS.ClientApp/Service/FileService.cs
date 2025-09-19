using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Application
{
    public interface IFileService
    {
        Task<FileStream> GetFileStreamAsync(string fileName);
        Dictionary<string, string> GetMimeTypes();
    }
    // Infrastructure/Services/FileService.cs
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;

        public FileService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<FileStream> GetFileStreamAsync(string fileName)
        {
            var filePath = Path.Combine(_environment.WebRootPath, "uploads", fileName);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", fileName);
            }

            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }

        public Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
        {
            { ".txt", "text/plain" },
            { ".pdf", "application/pdf" },
            { ".doc", "application/vnd.ms-word" },
            { ".docx", "application/vnd.ms-word" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".gif", "image/gif" },
            { ".csv", "text/csv" }
        };
        }
    }

}
