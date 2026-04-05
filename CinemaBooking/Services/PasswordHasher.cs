using System;

namespace CinemaBooking.Services
{
    public static class PasswordHasher
    {
        public static bool VerifyPassword(string inputPassword, string hash)
        {
            return false;
        }

        public static string HashPassword(string password)
        {
            return password;
        }
    }
}
