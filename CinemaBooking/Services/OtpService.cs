using System.Threading.Tasks;

namespace CinemaBooking.Services
{
    public class OtpService
    {
        public Task<bool> VerifyOtp(string email, string otp, string action)
        {
            return Task.FromResult(true); // Luôn đúng để skip OTP
        }

        public Task InvalidateOldOtps(string email, string action)
        {
            return Task.CompletedTask;
        }

        public Task<string> CreateOtp(string email, string action, string name)
        {
            return Task.FromResult("123456"); // Mã ảo
        }
    }
}
