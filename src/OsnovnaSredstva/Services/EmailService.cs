using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace OsnovnaSredstva.Services;

public class EmailService
{
    private readonly EmailPodesavanjaService _podesavanjaService;

    public EmailService(EmailPodesavanjaService podesavanjaService)
    {
        _podesavanjaService = podesavanjaService;
    }

    public async Task PosaljiAsync(
        IEnumerable<string> primaoci,
        string predmet,
        string tekst,
        byte[] pdfBytes,
        string pdfNaziv,
        CancellationToken ct = default)
    {
        var cfg = _podesavanjaService.DajPodesavanja();

        if (string.IsNullOrWhiteSpace(cfg.SmtpServer))
            throw new InvalidOperationException("SMTP server nije podesen. Idite u Podesavanja.");

        if (string.IsNullOrWhiteSpace(cfg.PosiljaoceEmail))
            throw new InvalidOperationException("E-mail adresa posiljaoca nije podesena.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(cfg.PosiljaoceIme, cfg.PosiljaoceEmail));

        foreach (var adresa in primaoci)
        {
            var trimmed = adresa.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
                message.To.Add(new MailboxAddress(string.Empty, trimmed));
        }

        if (message.To.Count == 0)
            throw new ArgumentException("Nijedna valjana e-mail adresa primaoca nije uneta.");

        message.Subject = predmet;

        var bodyBuilder = new BodyBuilder { TextBody = tekst };
        bodyBuilder.Attachments.Add(pdfNaziv, pdfBytes, new ContentType("application", "pdf"));
        message.Body = bodyBuilder.ToMessageBody();

        var secOpt = cfg.KoristiTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;

        using var client = new SmtpClient();
        await client.ConnectAsync(cfg.SmtpServer, cfg.SmtpPort, secOpt, ct);

        if (!string.IsNullOrWhiteSpace(cfg.KorisnickoIme))
            await client.AuthenticateAsync(cfg.KorisnickoIme, cfg.Lozinka, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
