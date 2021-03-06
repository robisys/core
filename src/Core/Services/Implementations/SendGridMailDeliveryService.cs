﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;
using Bit.Core.Models.Mail;
using System.Linq;

namespace Bit.Core.Services
{
    public class SendGridMailDeliveryService : IMailDeliveryService
    {
        private readonly GlobalSettings _globalSettings;
        private readonly SendGridClient _client;

        public SendGridMailDeliveryService(GlobalSettings globalSettings)
        {
            if(globalSettings.Mail?.SendGridApiKey == null)
            {
                throw new ArgumentNullException(nameof(globalSettings.Mail.SendGridApiKey));
            }

            _globalSettings = globalSettings;
            _client = new SendGridClient(_globalSettings.Mail.SendGridApiKey);
        }

        public async Task SendEmailAsync(MailMessage message)
        {
            var sendGridMessage = new SendGridMessage
            {
                Subject = message.Subject,
                From = new EmailAddress(_globalSettings.Mail.ReplyToEmail, _globalSettings.SiteName),
                HtmlContent = message.HtmlContent,
                PlainTextContent = message.TextContent,
            };

            sendGridMessage.AddTos(message.ToEmails.Select(e => new EmailAddress(e)).ToList());

            if(message.MetaData?.ContainsKey("SendGridTemplateId") ?? false)
            {
                sendGridMessage.HtmlContent = " ";
                sendGridMessage.PlainTextContent = " ";
                sendGridMessage.TemplateId = message.MetaData["SendGridTemplateId"].ToString();
            }

            if(message.MetaData?.ContainsKey("SendGridSubstitutions") ?? false)
            {
                var subs = message.MetaData["SendGridSubstitutions"] as Dictionary<string, string>;
                sendGridMessage.AddSubstitutions(subs);
            }

            if(message.MetaData?.ContainsKey("SendGridCategories") ?? false)
            {
                var cats = message.MetaData["SendGridCategories"] as List<string>;
                sendGridMessage.AddCategories(cats);
            }

            if(message.MetaData?.ContainsKey("SendGridBypassListManagement") ?? false)
            {
                var bypass = message.MetaData["SendGridBypassListManagement"] as bool?;
                sendGridMessage.SetBypassListManagement(bypass.GetValueOrDefault(false));
            }

            await _client.SendEmailAsync(sendGridMessage);
        }
    }
}
