﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Core.Models.Table;
using System.Net;
using Bit.Core.Models.Mail;

namespace Bit.Core.Services
{
    public class SendGridTemplateMailService : IMailService
    {
        private const string WelcomeTemplateId = "045f8ad5-5547-4fa2-8d3d-6d46e401164d";
        private const string ChangeEmailAlreadyExistsTemplateId = "b69d2038-6ad9-4cf6-8f7f-7880921cba43";
        private const string ChangeEmailTemplateId = "ec2c1471-8292-4f17-b6b6-8223d514f86e";
        private const string NoMasterPasswordHintTemplateId = "136eb299-e102-495a-88bd-f96736eea159";
        private const string MasterPasswordHintTemplateId = "be77cfde-95dd-4cb9-b5e0-8286b53885f1";
        private const string OrganizationInviteTemplateId = "1eff5512-e36c-49a8-b9e2-2b215d6bbced";
        private const string OrganizationAcceptedTemplateId = "28f7f741-598e-449c-85fe-601e1cc32ba3";
        private const string OrganizationConfirmedTemplateId = "a8afe2a0-6161-4eb9-b40c-08a7f520ec50";

        private const string AdministrativeCategoryName = "Administrative";
        private const string MarketingCategoryName = "Marketing";

        private readonly GlobalSettings _globalSettings;
        private readonly IMailDeliveryService _mailDeliveryService;

        public SendGridTemplateMailService(
            GlobalSettings globalSettings,
            IMailDeliveryService mailDeliveryService)
        {
            _globalSettings = globalSettings;
            _mailDeliveryService = mailDeliveryService;
        }

        public async Task SendWelcomeEmailAsync(User user)
        {
            var message = CreateDefaultMessage(
                "Welcome",
                user.Email,
                WelcomeTemplateId);
            
            AddCategories(message, new List<string> { AdministrativeCategoryName, "Welcome" });

            await _mailDeliveryService.SendEmailAsync(message);
        }

        public async Task SendChangeEmailAlreadyExistsEmailAsync(string fromEmail, string toEmail)
        {
            var message = CreateDefaultMessage(
                "Your Email Change",
                toEmail,
                ChangeEmailAlreadyExistsTemplateId);

            AddSubstitution(message, "{{fromEmail}}", fromEmail);
            AddSubstitution(message, "{{toEmail}}", toEmail);
            AddCategories(message, new List<string> { AdministrativeCategoryName, "Change Email Alrady Exists" });

            await _mailDeliveryService.SendEmailAsync(message);
        }

        public async Task SendChangeEmailEmailAsync(string newEmailAddress, string token)
        {
            var message = CreateDefaultMessage(
               "Your Email Change",
               newEmailAddress,
               ChangeEmailTemplateId);

            AddSubstitution(message, "{{token}}", Uri.EscapeDataString(token));
            AddCategories(message, new List<string> { AdministrativeCategoryName, "Change Email" });
            message.MetaData.Add("SendGridBypassListManagement", true);

            await _mailDeliveryService.SendEmailAsync(message);
        }

        public async Task SendNoMasterPasswordHintEmailAsync(string email)
        {
            var message = CreateDefaultMessage(
                "Your Master Password Hint",
                email,
                NoMasterPasswordHintTemplateId);
            AddCategories(message, new List<string> { AdministrativeCategoryName, "No Master Password Hint" });

            await _mailDeliveryService.SendEmailAsync(message);
        }

        public async Task SendMasterPasswordHintEmailAsync(string email, string hint)
        {
            var message = CreateDefaultMessage(
                "Your Master Password Hint",
                email,
                MasterPasswordHintTemplateId);

            message.Subject = "Your Master Password Hint";
            AddSubstitution(message, "{{hint}}", hint);
            AddCategories(message, new List<string> { AdministrativeCategoryName, "Master Password Hint" });

            await _mailDeliveryService.SendEmailAsync(message);
        }

        public async Task SendOrganizationInviteEmailAsync(string organizationName, OrganizationUser orgUser, string token)
        {
            var message = CreateDefaultMessage(
                $"Join {organizationName}",
                orgUser.Email,
                OrganizationInviteTemplateId);

            AddSubstitution(message, "{{organizationName}}", organizationName);
            AddSubstitution(message, "{{organizationId}}", orgUser.OrganizationId.ToString());
            AddSubstitution(message, "{{organizationUserId}}", orgUser.Id.ToString());
            AddSubstitution(message, "{{token}}", token);
            AddSubstitution(message, "{{email}}", WebUtility.UrlEncode(orgUser.Email));
            AddSubstitution(message, "{{organizationNameUrlEncoded}}", WebUtility.UrlEncode(organizationName));
            AddCategories(message, new List<string> { AdministrativeCategoryName, "Organization User Invite" });

            await _mailDeliveryService.SendEmailAsync(message);
        }

        public async Task SendOrganizationAcceptedEmailAsync(string organizationName, string userEmail,
            IEnumerable<string> adminEmails)
        {
            var message = CreateDefaultMessage(
                $"User {userEmail} Has Accepted Invite",
                adminEmails,
                OrganizationAcceptedTemplateId);

            AddSubstitution(message, "{{userEmail}}", userEmail);
            AddSubstitution(message, "{{organizationName}}", organizationName);
            AddCategories(message, new List<string> { AdministrativeCategoryName, "Organization User Accepted" });

            await _mailDeliveryService.SendEmailAsync(message);
        }

        public async Task SendOrganizationConfirmedEmailAsync(string organizationName, string email)
        {
            var message = CreateDefaultMessage(
                $"You Have Been Confirmed To {organizationName}",
                email,
                OrganizationConfirmedTemplateId);

            AddSubstitution(message, "{{organizationName}}", organizationName);
            AddCategories(message, new List<string> { AdministrativeCategoryName, "Organization User Confirmed" });

            await _mailDeliveryService.SendEmailAsync(message);
        }

        private MailMessage CreateDefaultMessage(string subject, string toEmail, string templateId)
        {
            return CreateDefaultMessage(subject, new List<string> { toEmail }, templateId);
        }

        private MailMessage CreateDefaultMessage(string subject, IEnumerable<string> toEmails, string templateId)
        {
            var message = new MailMessage
            {
                HtmlContent = " ",
                TextContent = " ",
                MetaData = new Dictionary<string, object>(),
                ToEmails = toEmails,
                Subject = subject
            };

            if(!string.IsNullOrWhiteSpace(templateId))
            {
                message.MetaData.Add("SendGridTemplateId", templateId);
            }

            AddSubstitution(message, "{{siteName}}", _globalSettings.SiteName);
            AddSubstitution(message, "{{baseVaultUri}}", _globalSettings.BaseVaultUri);

            return message;
        }

        private void AddSubstitution(MailMessage message, string key, string value)
        {
            Dictionary<string, string> dict;
            if(!message.MetaData.ContainsKey("SendGridSubstitutions"))
            {
                dict = new Dictionary<string, string>();
            }
            else
            {
                dict = message.MetaData["SendGridSubstitutions"] as Dictionary<string, string>;
            }

            dict.Add(key, value);
            message.MetaData["SendGridSubstitutions"] = dict;
        }

        private void AddCategories(MailMessage message, List<string> categories)
        {
            List<string> cats;
            if(!message.MetaData.ContainsKey("SendGridCategories"))
            {
                cats = categories;
            }
            else
            {
                cats = message.MetaData["SendGridCategories"] as List<string>;
                cats.AddRange(categories);
            }

            message.MetaData["SendGridCategories"] = cats;
        }
    }
}
