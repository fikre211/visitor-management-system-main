//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.AspNetCore.Mvc; // Change to Microsoft.AspNetCore.Mvc
//using Twilio;
//using Twilio.Rest.Api.V2010.Account;
//using Twilio.Types;
//using Twilio.TwiML;
//using Twilio.AspNet.Mvc;
//using Microsoft.Extensions.Configuration; // Add this line

//namespace GatePass.MS.ClientApp.Controllers
//{
//    public class SmsController : TwilioController
//    {
//        private readonly IConfiguration _configuration; // Add this line

//        public SmsController(IConfiguration configuration) // Inject IConfiguration
//        {
//            _configuration = configuration;
//        }

//        public ActionResult SendSms()
//        {
//            var AccountSid = _configuration["TwilioAccountSid"];
//            var authToken = _configuration["TwilioAuthToken"];
//            TwilioClient.Init(AccountSid, authToken);
//            var to = new PhoneNumber(_configuration["MyPhoneNumber"]);
//            var from = new PhoneNumber("+13203058944");
//            var message = MessageResource.Create(
//                to: to,
//                from: from,
//                body: "test message");
//            return Ok(message.Sid); 
//        }
//    }
//}