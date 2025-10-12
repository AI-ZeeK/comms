using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendWelcomeEmailAsync(string to, string name);
    Task SendPasswordResetEmailAsync(string to, string name, string resetToken);
    Task SendVerificationEmailAsync(string to, string name, string verificationToken);
}

public class SmtpEmailService : IEmailService
{
    private readonly SmtpClient _smtpClient;
    private readonly string _fromAddress;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly IConfiguration _config;
    private readonly Dictionary<string, string> _templates;
    private readonly string _frontendUrl;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        var host = config["Email:Smtp:Host"] ?? "";
        var port = int.Parse(config["Email:Smtp:Port"] ?? "587");
        var username = config["Email:Smtp:Username"] ?? "";
        var password = config["Email:Smtp:Password"] ?? "";
        _fromAddress = config["Email:Smtp:From"] ?? username;
        _frontendUrl = config["Frontend:Url"] ?? "";

        _smtpClient = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true,
        };
        _logger = logger;

        _templates = new Dictionary<string, string>
        {
            { "welcome", LoadTemplate("welcome.html") },
            { "resetPassword", LoadTemplate("reset-password.html") },
            { "verifyEmail", LoadTemplate("verify-email.html") },
        };
    }

    private string LoadTemplate(string filename)
    {
        try
        {
            var templatePath = Path.Combine(
                AppContext.BaseDirectory,
                "mail",
                "templates",
                filename
            );
            return File.ReadAllText(templatePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load template {filename}");
            throw;
        }
    }

    private string RenderTemplate(string template, Dictionary<string, string> data)
    {
        var result = template;
        foreach (var kv in data)
        {
            result = result.Replace($"{{{{{kv.Key}}}}}", kv.Value ?? "");
        }
        return result;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        var mail = new MailMessage(_fromAddress, to, subject, body) { IsBodyHtml = isHtml };
        try
        {
            await _smtpClient.SendMailAsync(mail);
            _logger.LogInformation($"Email sent to {to} with subject '{subject}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {to}");
            throw;
        }
    }

    public async Task SendWelcomeEmailAsync(string to, string name)
    {
        var data = new Dictionary<string, string>
        {
            { "name", name },
            { "dashboardUrl", $"{_frontendUrl}/dashboard" },
            { "helpCenterUrl", $"{_frontendUrl}/help" },
        };
        var html = RenderTemplate(_templates["welcome"], data);
        await SendEmailAsync(to, "Welcome to DJENGO!", html, true);
    }

    public async Task SendPasswordResetEmailAsync(string to, string name, string resetToken)
    {
        var data = new Dictionary<string, string>
        {
            { "name", name },
            { "resetUrl", $"{_frontendUrl}/reset-password?token={resetToken}" },
        };
        var html = RenderTemplate(_templates["resetPassword"], data);
        await SendEmailAsync(to, "Reset Your DJENGO Password", html, true);
    }

    public async Task SendVerificationEmailAsync(string to, string name, string verificationToken)
    {
        var data = new Dictionary<string, string>
        {
            { "name", name },
            { "verificationUrl", $"{_frontendUrl}/verify-email?token={verificationToken}" },
        };
        var html = RenderTemplate(_templates["verifyEmail"], data);
        await SendEmailAsync(to, "Verify Your DJENGO Email", html, true);
    }
}
