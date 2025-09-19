using GatePass.MS.Domain;
using Microsoft.Extensions.Options;

namespace GatePass.MS.ClientApp.Service
{
    public class SmsService
    {
        private readonly HttpClient _httpClient;
        private readonly SmsSettings _smsSettings;
        public SmsService(HttpClient httpClient, IOptions<SmsSettings> smsSettings)
        {
            _httpClient = httpClient;
            _smsSettings = smsSettings.Value; // Get the actual SmsSettings instance
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                // Properly encode the message to avoid issues with special characters
                string encodedMessage = Uri.EscapeDataString(message);

                // Fix the URL formatting: Use '?' for first parameter, '&' for the second one
                var url = $"http://{_smsSettings.Ip}:{_smsSettings.Port}/sendsms?key=WzOvYNX1uh7aJgL4&&phonenumber={phoneNumber}&&message={encodedMessage}";

                Console.WriteLine($"Sending SMS to: {phoneNumber}");
                Console.WriteLine($"Request URL: {url}");

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine(" SMS sent successfully.");
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"SMS failed. Status: {response.StatusCode}, Response: {errorResponse}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending SMS: {ex.Message}");
            }
        }


        public async Task SendBulkSmsAsync(string[] phoneNumbers, string message)
        {
            var numbers = string.Join(",", phoneNumbers.Select(num => $"\"{num}\""));
            var url = $"http://{_smsSettings.Ip}:{_smsSettings.Port}/sendbulksms?phonenumbers=[{numbers}]&message={Uri.EscapeDataString(message)}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode(); // Throws if the response status code is not successful
        }

        public async Task<string> GetDeliveryStatusAsync(string trackingNumber)
        {
            var url = $"http://{_smsSettings.Ip}:{_smsSettings.Port}/deliverystatus?trackingnumber={trackingNumber}";
            var response = await _httpClient.GetStringAsync(url);
            return response; // Assuming the API returns a string with the status
        }
    }
}
