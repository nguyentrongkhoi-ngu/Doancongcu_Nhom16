using CinemaBooking.Models;

namespace CinemaBooking.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalMovies { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<DatVe> RecentBookings { get; set; } = new List<DatVe>();
    }
}
