using System.Threading.Tasks;

namespace CinemaBooking.Services
{
    public class EmailService
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Task.CompletedTask;
        }
    }
}
