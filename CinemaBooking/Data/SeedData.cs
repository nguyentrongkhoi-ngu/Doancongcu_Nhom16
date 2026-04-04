using CinemaBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Data
{
    public static class SeedData
    {
        public static async Task Initialize(ApplicationDbContext context)
        {
            // Đảm bảo database đã được tạo
            Console.WriteLine("Starting SeedData.Initialize...");
            await context.Database.EnsureCreatedAsync();

            // Check and Seed Roles
            if (!context.VaiTros.Any())
            {
                var vaiTros = new VaiTro[]
                {
                    new VaiTro { TenVaiTro = "Admin", MoTa = "Quản trị viên hệ thống" },
                    new VaiTro { TenVaiTro = "User", MoTa = "Người dùng thông thường" }
                };

                foreach (var vaiTro in vaiTros)
                {
                    context.VaiTros.Add(vaiTro);
                }
                await context.SaveChangesAsync();
            }

            // Check and Seed Admin User
            if (!context.NguoiDungs.Any(u => u.TenDangNhap == "admin"))
            {
                var adminUser = new NguoiDung
                {
                    TenDangNhap = "admin",
                    MatKhau = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Email = "admin@cinezore.com",
                    HoTen = "Quản trị viên",
                    SoDienThoai = "0123456789",
                    NgayTao = DateTime.Now,
                    MaVaiTro = context.VaiTros.FirstOrDefault(r => r.TenVaiTro == "Admin")?.MaVaiTro ?? 1
                };

                context.NguoiDungs.Add(adminUser);
                await context.SaveChangesAsync();
            }

            // Check and Seed Rap Phims
            if (!context.RapPhims.Any())
            {
                var rapPhims = new RapPhim[]
                {
                    new RapPhim { TenRap = "CineZore Hà Nội", DiaChi = "123 Đường ABC, Quận 1", ThanhPho = "Hà Nội" },
                    new RapPhim { TenRap = "CineZore TP.HCM", DiaChi = "456 Đường XYZ, Quận 3", ThanhPho = "TP.HCM" },
                    new RapPhim { TenRap = "CineZore Đà Nẵng", DiaChi = "789 Đường DEF, Quận Hải Châu", ThanhPho = "Đà Nẵng" }
                };

                foreach (var rap in rapPhims)
                {
                    context.RapPhims.Add(rap);
                }
                await context.SaveChangesAsync();

                // Seed Phongs and Seats only if RapPhims were just added
                await SeedRoomsAndSeats(context);
            }

            // Seed Movies (Legacy + Trending)
            Console.WriteLine("Seeding movies...");
            await SeedMovies(context);

            // Seed Promotions
            if (!context.KhuyenMais.Any())
            {
                var khuyenMais = new KhuyenMai[]
                {
                    new KhuyenMai 
                    { 
                        MaCode = "WELCOME10", 
                        PhanTramGiam = 10, 
                        NgayBatDau = DateTime.Now.AddDays(-30), 
                        NgayKetThuc = DateTime.Now.AddDays(30),
                        MoTa = "Giảm 10% cho khách hàng mới"
                    },
                    new KhuyenMai 
                    { 
                        MaCode = "SUMMER20", 
                        PhanTramGiam = 20, 
                        NgayBatDau = DateTime.Now.AddDays(-15), 
                        NgayKetThuc = DateTime.Now.AddDays(45),
                        MoTa = "Giảm 20% mùa hè"
                    }
                };

                foreach (var khuyenMai in khuyenMais)
                {
                    context.KhuyenMais.Add(khuyenMai);
                }
                await context.SaveChangesAsync();
            }

            // Finally, Ensure all movies have some showtimes
            Console.WriteLine("Seeding showtimes...");
            await SeedShowtimes(context);
            Console.WriteLine("SeedData completed successfully.");
        }

        private static async Task SeedRoomsAndSeats(ApplicationDbContext context)
        {
            var rapPhims = await context.RapPhims.ToListAsync();
            var phongChieus = new List<PhongChieu>();

            foreach (var rap in rapPhims)
            {
                phongChieus.Add(new PhongChieu { MaRap = rap.MaRap, SoPhong = 1, SucChua = 100 });
                phongChieus.Add(new PhongChieu { MaRap = rap.MaRap, SoPhong = 2, SucChua = 120 });
            }

            foreach (var phong in phongChieus)
            {
                context.PhongChieus.Add(phong);
            }
            await context.SaveChangesAsync();
            await SeedSeats(context);
        }

        private static async Task SeedMovies(ApplicationDbContext context)
        {
            var movieData = new List<Phim>
            {
                new Phim 
                { 
                    TenPhim = "Avengers: Endgame", 
                    MoTa = "Cuộc chiến cuối cùng của các siêu anh hùng để cứu lấy vũ trụ khỏi sự hủy diệt của Thanos.", 
                    ThoiLuong = 181, 
                    TheLoai = "Hành động, Khoa học viễn tưởng", 
                    NgayPhatHanh = new DateTime(2019, 4, 26),
                    UrlPoster = "https://image.tmdb.org/t/p/original/or06vSfv0uY7o98ToolkitW0v3K7.jpg",
                    DinhDang = "2D, 3D, IMAX",
                    Trailer = "https://www.youtube.com/watch?v=TcMBFSGVi1c"
                },
                new Phim 
                { 
                    TenPhim = "Những Mảnh Ghép Cảm Xúc 2", 
                    MoTa = "Inside Out 2 tiếp tục cuộc hành trình khám phá thế giới nội tâm của Riley khi cô bước sang tuổi thiếu niên với những cảm xúc mới đầy phức tạp.", 
                    ThoiLuong = 96, 
                    TheLoai = "Hoạt hình, Hài hước, Gia đình", 
                    NgayPhatHanh = new DateTime(2024, 6, 14),
                    UrlPoster = "https://image.tmdb.org/t/p/original/vpn9sy7kR40O1ZJ3A6Gv09yH6Vj.jpg",
                    DinhDang = "2D, 3D",
                    Trailer = "https://www.youtube.com/watch?v=L4DrolmDxmw"
                },
                new Phim 
                { 
                    TenPhim = "Deadpool & Wolverine", 
                    MoTa = "Deadpool hợp tác với Wolverine trong một nhiệm vụ điên rồ của TVA để cứu lấy dòng thời gian của mình.", 
                    ThoiLuong = 127, 
                    TheLoai = "Hành động, Hài hước, Sci-Fi", 
                    NgayPhatHanh = new DateTime(2024, 7, 26),
                    UrlPoster = "https://image.tmdb.org/t/p/original/8cd96f2pUAp9GmBy5C2OSWSJhNm.jpg",
                    DinhDang = "2D, 3D, IMAX",
                    Trailer = "https://www.youtube.com/watch?v=73_1biulkYk"
                },
                new Phim 
                { 
                    TenPhim = "Lốc Xoáy Tử Thần", 
                    MoTa = "Phần phim tiếp theo của tác phẩm kinh điển năm 1996, đưa người xem vào trung tâm những cơn bão lốc xoáy kinh hoàng với công nghệ săn bão hiện đại.", 
                    ThoiLuong = 122, 
                    TheLoai = "Hành động, Phiêu lưu, Kịch tính", 
                    NgayPhatHanh = new DateTime(2024, 7, 19),
                    UrlPoster = "https://image.tmdb.org/t/p/original/pjnD0S79CVC0XDY7vtSjIqvX1u2.jpg",
                    DinhDang = "2D, IMAX",
                    Trailer = "https://www.youtube.com/watch?v=vVj2itVjfd8"
                },
                new Phim 
                { 
                    TenPhim = "Robot Hoang Dã", 
                    MoTa = "Một chú robot thông minh lạc trên hoang đảo và phải học cách làm bạn với các sinh vật tự nhiên để sinh tồn.", 
                    ThoiLuong = 102, 
                    TheLoai = "Hoạt hình, Phiêu lưu, Khoa học viễn tưởng", 
                    NgayPhatHanh = new DateTime(2024, 9, 27),
                    UrlPoster = "https://image.tmdb.org/t/p/original/hr7I1tLIs090dK47K0P70wInS0G.jpg",
                    DinhDang = "2D, 3D",
                    Trailer = "https://www.youtube.com/watch?v=67vbA5ZJdUs"
                },
                new Phim 
                { 
                    TenPhim = "Anora", 
                    MoTa = "Câu chuyện về một cô gái múa thoát y ở Brooklyn có cơ hội đổi đời khi gặp gỡ và kết hôn với con trai một nhà tài phiệt Nga.", 
                    ThoiLuong = 139, 
                    TheLoai = "Hài hước, Kịch tính, Romance", 
                    NgayPhatHanh = new DateTime(2024, 10, 18),
                    UrlPoster = "https://image.tmdb.org/t/p/original/6KpaYlyQoXlP2WbH2f2Tsc69kM.jpg",
                    DinhDang = "2D",
                    Trailer = "https://www.youtube.com/watch?v=I6B0_m83RVM"
                },
                new Phim 
                { 
                    TenPhim = "Sinners", 
                    MoTa = "Trong nỗ lực bỏ qua cuộc sống khó khăn, hai anh em sinh đôi trở về thị trấn quê hương để bắt đầu lại, nhưng họ phát hiện ra một ác quỷ còn đáng sợ hơn.", 
                    ThoiLuong = 115, 
                    TheLoai = "Kinh dị, Thriller, Kịch tính", 
                    NgayPhatHanh = new DateTime(2025, 3, 7),
                    UrlPoster = "https://image.tmdb.org/t/p/original/wD1p356Xv3b8hE2m5hSleUpK3vj.jpg",
                    DinhDang = "2D",
                    Trailer = "https://www.youtube.com/watch?v=mD2f_h1fE0M"
                },
                new Phim 
                { 
                    TenPhim = "Frankenstein", 
                    MoTa = "Đạo diễn Guillermo del Toro tái hiện câu chuyện kinh điển về nhà khoa học Victor Frankenstein và tạo vật quái dị của ông.", 
                    ThoiLuong = 152, 
                    TheLoai = "Kinh dị, Sci-Fi, Kịch tính", 
                    NgayPhatHanh = new DateTime(2025, 10, 31),
                    UrlPoster = "https://image.tmdb.org/t/p/original/yD0rN0Y6yI1c9nIqy7mO5ZzPzK3.jpg",
                    DinhDang = "2D, IMAX",
                    Trailer = "https://www.youtube.com/watch?v=rX6T9Z3rR_Y"
                },
                new Phim 
                { 
                    TenPhim = "One of Them Days", 
                    MoTa = "Hai người bạn thân sống chung nhà phải đối mặt với một ngày tồi tệ nhất đời khi mọi thứ đều đi chệch quỹ đạo.", 
                    ThoiLuong = 105, 
                    TheLoai = "Hài hước", 
                    NgayPhatHanh = new DateTime(2025, 1, 24),
                    UrlPoster = "https://image.tmdb.org/t/p/original/zN0fG0A9j3w3f7vX6rPzK5I9Gv.jpg",
                    DinhDang = "2D",
                    Trailer = "https://www.youtube.com/watch?v=OneOfThemDaysTrailer"
                },
                new Phim 
                { 
                    TenPhim = "Happy Gilmore 2", 
                    MoTa = "Sự trở lại đầy hài hước của tay chơi golf huyền thoại Adam Sandler trong phần tiếp theo của tác phẩm ăn khách năm 1996.", 
                    ThoiLuong = 110, 
                    TheLoai = "Hài hước, Thể thao", 
                    NgayPhatHanh = new DateTime(2025, 8, 15),
                    UrlPoster = "https://image.tmdb.org/t/p/original/happy_gilmore_two_poster.jpg",
                    DinhDang = "2D",
                    Trailer = "https://www.youtube.com/watch?v=HappyGilmore2"
                },
                new Phim 
                { 
                    TenPhim = "My Oxford Year", 
                    MoTa = "Một sinh viên trẻ người Mỹ được học bổng tại Đại học Oxford và đem lòng yêu một giảng viên tại đây, nhưng một bí mật đe dọa tương lai của họ.", 
                    ThoiLuong = 113, 
                    TheLoai = "Romance, Drama", 
                    NgayPhatHanh = new DateTime(2025, 2, 14),
                    UrlPoster = "https://image.tmdb.org/t/p/original/my_oxford_year_poster.jpg",
                    DinhDang = "2D",
                    Trailer = "https://www.youtube.com/watch?v=MyOxfordYear"
                },
                new Phim 
                { 
                    TenPhim = "Spider-Man: No Way Home", 
                    MoTa = "Peter Parker phải đối mặt với những kẻ thù từ các vũ trụ khác", 
                    ThoiLuong = 148, 
                    TheLoai = "Hành động, Phiêu lưu", 
                    NgayPhatHanh = new DateTime(2021, 12, 17),
                    UrlPoster = "https://image.tmdb.org/t/p/original/1g0dhvRzfwvqp1Z6BLpvmUfAdpI.jpg",
                    DinhDang = "2D, 3D, IMAX",
                    Trailer = "https://www.youtube.com/watch?v=JfVOs4VSpmA"
                },
                new Phim 
                { 
                    TenPhim = "Top Gun: Maverick", 
                    MoTa = "Maverick trở lại với nhiệm vụ mới", 
                    ThoiLuong = 130, 
                    TheLoai = "Hành động, Drama", 
                    NgayPhatHanh = new DateTime(2022, 5, 27),
                    UrlPoster = "https://image.tmdb.org/t/p/original/628SwSjtR7ovR6RM9uFEEAbpqo.jpg",
                    DinhDang = "2D, IMAX",
                    Trailer = "https://www.youtube.com/watch?v=qSqVVswa420"
                }
            };

            foreach (var phim in movieData)
            {
                if (!context.Phims.Any(p => p.TenPhim == phim.TenPhim))
                {
                    Console.WriteLine($"Adding movie: {phim.TenPhim}");
                    context.Phims.Add(phim);
                    await context.SaveChangesAsync();
                }
                else {
                    Console.WriteLine($"Movie already exists: {phim.TenPhim}");
                }
            }

            // Seed NgonNguPhim for newly added movies
            var allPhims = await context.Phims.ToListAsync();
            foreach (var phim in allPhims)
            {
                if (!context.NgonNguPhims.Any(n => n.MaPhim == phim.MaPhim))
                {
                    context.NgonNguPhims.Add(new NgonNguPhim { MaPhim = phim.MaPhim, NgonNgu = "Tiếng Anh", PhuDe = "Tiếng Việt" });
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedSeats(ApplicationDbContext context)
        {
            var phongChieus = await context.PhongChieus.ToListAsync();
            
            foreach (var phong in phongChieus)
            {
                if (context.Ghes.Any(g => g.MaPhong == phong.MaPhong)) continue;

                int soHang = phong.SucChua / 10; 
                for (int hang = 0; hang < soHang; hang++)
                {
                    char tenHang = (char)('A' + hang);
                    for (int ghe = 1; ghe <= 10; ghe++)
                    {
                        var gheNew = new Ghe
                        {
                            MaPhong = phong.MaPhong,
                            SoGhe = $"{tenHang}{ghe}",
                            LoaiGhe = (ghe >= 4 && ghe <= 7) ? "VIP" : "Thường"
                        };
                        context.Ghes.Add(gheNew);
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedShowtimes(ApplicationDbContext context)
        {
            var phims = await context.Phims.ToListAsync();
            var phongChieus = await context.PhongChieus.ToListAsync();
            var ngonNguPhims = await context.NgonNguPhims.ToListAsync();

            foreach (var phim in phims)
            {
                foreach (var phong in phongChieus.Take(2)) 
                {
                    if (!context.LichChieus.Any(l => l.MaPhim == phim.MaPhim && l.MaPhong == phong.MaPhong && l.NgayChieu == DateTime.Today))
                    {
                        for (int day = 0; day < 3; day++)
                        {
                            var ngayChieu = DateTime.Today.AddDays(day);
                            var gioChieus = new TimeSpan[] 
                            { 
                                new TimeSpan(10, 0, 0), 
                                new TimeSpan(14, 0, 0), 
                                new TimeSpan(19, 0, 0) 
                            };

                            foreach (var gioChieu in gioChieus)
                            {
                                var ngonNgu = ngonNguPhims.FirstOrDefault(n => n.MaPhim == phim.MaPhim);
                                var lichChieu = new LichChieu
                                {
                                    MaPhim = phim.MaPhim,
                                    MaPhong = phong.MaPhong,
                                    NgayChieu = ngayChieu,
                                    GioChieu = gioChieu,
                                    GiaVe = 95000,
                                    MaNgonNgu = ngonNgu?.MaNgonNgu
                                };
                                context.LichChieus.Add(lichChieu);
                            }
                        }
                    }
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
