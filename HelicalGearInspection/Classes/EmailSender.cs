using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO.Packaging;

namespace HelicalGearInspection
{
    public class EmailSender
    {
        private string smtpServer;
        private int smtpPort;
        private string smtpUsername;
        private string smtpPassword;

        public EmailSender(string smtpServer, int smtpPort, string smtpUsername, string smtpPassword)
        {
            this.smtpServer = smtpServer;
            this.smtpPort = smtpPort;
            this.smtpUsername = smtpUsername;
            this.smtpPassword = smtpPassword;
        }


        //string smtpServer = "172.16.12.168";
        //int smtpPort = 25;
        //string smtpUsername = "notifications.msr@sonacomstar.com";
        ////string smtpPassword = "ezhi kehi fomn axkz";
        //string smtpPassword = "@#Alerts*5373";

        public static void SendEmail(string server, int port, string username, string password, string[] recipientEmails, string[] ccEmails, string filename )
        {
            using (SmtpClient smtpClient = new SmtpClient(server, port))
            {
                smtpClient.UseDefaultCredentials = false;
                smtpClient.EnableSsl = false;
                smtpClient.Credentials = new NetworkCredential(username, password);

                using (MailMessage mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(username);
                    foreach (string email in recipientEmails)
                    {
                        mailMessage.To.Add(email);
                    }
                    foreach (string email in ccEmails)
                    {
                        mailMessage.CC.Add(email);
                    }
                    //mailMessage.Bcc.Add("gaurnikunj116@gmail.com");
                    mailMessage.Subject = "Vision Gear Defect Inspection Daily Report";
                    mailMessage.Body = "Daily Report";
                    System.Net.Mail.Attachment attachment;
                    attachment = new System.Net.Mail.Attachment(filename);
                    mailMessage.Attachments.Add(attachment);

                    try
                    {
                        smtpClient.Send(mailMessage);
                        Console.WriteLine("Email sent successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending email: {ex.Message}");
                    }
                }
            }
        }

        public void Send() 
        {
            MailMessage mailMessage = new MailMessage("notifications.msr@sonacomstar.com", "gaurnikunj116@gmail.com");
            mailMessage.Priority = System.Net.Mail.MailPriority.High;
            mailMessage.Body = "message";
            mailMessage.IsBodyHtml = false;
            SmtpClient smtpClient = new SmtpClient("172.16.12.168", 25);
            NetworkCredential credentials = new NetworkCredential("notifications.msr@sonacomstar.com", "@#Alerts*5373");
            smtpClient.Credentials = credentials;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.Credentials = credentials;
            smtpClient.Send(mailMessage);
        }

        public void SendEmail(string toEmail, string subject, string body)
        {
            using (SmtpClient client = new SmtpClient(smtpServer, smtpPort))
            {
                client.EnableSsl = true;  // Use SSL/TLS
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                using (MailMessage mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(smtpUsername);
                    mailMessage.To.Add(toEmail);
                    mailMessage.Subject = subject;
                    mailMessage.Body = body;
                    //System.Net.Mail.Attachment attachment;
                    //attachment = new System.Net.Mail.Attachment(@"C:\Users\gaurn\Downloads\fast-cat-cat-excited.gif");
                    //mailMessage.Attachments.Add(attachment);

                    try
                    {
                        client.Send(mailMessage);
                        Console.WriteLine("Email sent successfully!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            }
        }
    }
}
