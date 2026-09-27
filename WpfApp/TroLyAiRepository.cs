using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace WpfApp.AI
{
    public sealed class TroLyAiRepository
    {
        private readonly int _userId;

        public TroLyAiRepository(int userId)
        {
            if (userId <= 0) throw new ArgumentException("Bạn cần đăng nhập trước khi dùng trợ lý.");
            _userId = userId;
        }

        private SqlParameter UserParameter()
        {
            return new SqlParameter("@UserId", SqlDbType.Int) { Value = _userId };
        }

        private SqlParameter[] RangeParameters(DateRange range)
        {
            return new[] {
                UserParameter(),
                new SqlParameter("@Start", SqlDbType.Date) { Value = range.Start },
                new SqlParameter("@End", SqlDbType.Date) { Value = range.End }
            };
        }

        public List<ChatTurn> LoadHistory()
        {
            var table = DatabaseHelper.GetData(@"
                SELECT TOP (20) MaChat, TinNhanNguoiDung, PhanHoiAI
                FROM LichSuChatAI WHERE MaNguoiDung = @UserId
                ORDER BY MaChat DESC", new[] { UserParameter() });
            var turns = new List<ChatTurn>();
            for (int i = table.Rows.Count - 1; i >= 0; i--)
                turns.Add(new ChatTurn
                {
                    User = Convert.ToString(table.Rows[i]["TinNhanNguoiDung"]),
                    Assistant = Convert.ToString(table.Rows[i]["PhanHoiAI"])
                });
            return turns;
        }

        public void SaveTurn(string user, string assistant)
        {
            DatabaseHelper.ExecuteQuery(@"
                INSERT INTO LichSuChatAI (MaNguoiDung, TinNhanNguoiDung, PhanHoiAI)
                VALUES (@UserId, @Question, @Answer)", new[] {
                    UserParameter(),
                    new SqlParameter("@Question", SqlDbType.NVarChar, -1) { Value = user },
                    new SqlParameter("@Answer", SqlDbType.NVarChar, -1) { Value = assistant }
                });
        }

        public void SaveReport(DateTime month, string body)
        {
            // A fresh analysis creates a new version. No fabricated health score.
            DatabaseHelper.ExecuteQuery(@"
                INSERT INTO BaoCaoPhanTichAI
                    (MaNguoiDung, Thang, Nam, NoiDungDanhGia, LoiKhuyen, DiemSucKhoe)
                VALUES (@UserId, @Month, @Year, @Body, NULL, NULL)", new[] {
                    UserParameter(),
                    new SqlParameter("@Month", SqlDbType.Int) { Value = month.Month },
                    new SqlParameter("@Year", SqlDbType.Int) { Value = month.Year },
                    new SqlParameter("@Body", SqlDbType.NVarChar, -1) { Value = body }
                });
        }

        public FinanceSnapshot GetSnapshot(DateRange range, bool includeGoals)
        {
            var account = DatabaseHelper.GetData(
                "SELECT TienTeMacDinh FROM NguoiDung WHERE MaNguoiDung = @UserId",
                new[] { UserParameter() });
            if (account.Rows.Count != 1) throw new InvalidOperationException("Tài khoản không còn tồn tại.");
            string currency = Convert.ToString(account.Rows[0]["TienTeMacDinh"]);
            if (String.IsNullOrWhiteSpace(currency)) currency = "VND";
            var snapshot = new FinanceSnapshot
            {
                TuNgay = range.Start.ToString("yyyy-MM-dd"),
                DenNgay = range.End.AddDays(-1).ToString("yyyy-MM-dd"),
                DonVi = currency,
                TheoDanhMuc = new List<object>(),
                NganSachCaThang = new List<object>(),
                MucTieuHienTai = new List<object>(),
                DaLayMucTieu = includeGoals
            };

            // Refuse misleading totals if legacy rows contain inconsistent ownership/type/amount.
            var invalid = DatabaseHelper.GetData(@"
                SELECT COUNT(*) AS SoLuong FROM GiaoDich g
                LEFT JOIN DanhMuc d ON d.MaDanhMuc = g.MaDanhMuc AND d.MaNguoiDung = @UserId
                WHERE g.MaNguoiDung = @UserId AND g.NgayGiaoDich >= @Start AND g.NgayGiaoDich < @End
                AND (d.MaDanhMuc IS NULL OR d.LoaiDanhMuc NOT IN (N'Thu nhập', N'Chi tiêu') OR g.SoTien <= 0)",
                RangeParameters(range));
            snapshot.GiaoDichKhongHopLe = Convert.ToInt32(invalid.Rows[0]["SoLuong"]);
            if (snapshot.GiaoDichKhongHopLe > 0)
                throw new InvalidOperationException("Có giao dịch có danh mục hoặc số tiền không hợp lệ trong khoảng này. Hãy kiểm tra tại trang Giao dịch trước khi phân tích.");

            var categories = DatabaseHelper.GetData(@"
                SELECT d.MaDanhMuc, d.TenDanhMuc,
                       CASE WHEN d.LoaiDanhMuc = N'Thu nhập' THEN N'Thu nhập' ELSE N'Chi tiêu' END AS LoaiDanhMuc,
                       SUM(g.SoTien) AS TongTien, COUNT(*) AS SoLuong
                FROM GiaoDich g
                INNER JOIN DanhMuc d ON d.MaDanhMuc = g.MaDanhMuc AND d.MaNguoiDung = @UserId
                WHERE g.MaNguoiDung = @UserId AND g.NgayGiaoDich >= @Start AND g.NgayGiaoDich < @End
                GROUP BY d.MaDanhMuc, d.TenDanhMuc, d.LoaiDanhMuc
                ORDER BY TongTien DESC, d.MaDanhMuc", RangeParameters(range));
            foreach (DataRow row in categories.Rows)
            {
                decimal amount = Convert.ToDecimal(row["TongTien"]);
                string type = Convert.ToString(row["LoaiDanhMuc"]);
                int count = Convert.ToInt32(row["SoLuong"]);
                if (type == "Thu nhập") snapshot.TongThu += amount;
                else snapshot.TongChi += amount;
                snapshot.SoGiaoDich += count;
                if (snapshot.TheoDanhMuc.Count < 100)
                    snapshot.TheoDanhMuc.Add(new
                    {
                        Ten = Convert.ToString(row["TenDanhMuc"]),
                        Loai = type,
                        TongTien = amount,
                        SoGiaoDich = count
                    });
            }
            snapshot.DanhMucDaRutGon = categories.Rows.Count > 100;

            // Budgets always cover the WHOLE last calendar month touched by the requested range.
            DateTime lastDay = range.End.AddDays(-1);
            DateTime month = new DateTime(lastDay.Year, lastDay.Month, 1);
            snapshot.ThangNganSach = month.ToString("MM/yyyy");
            var budgets = DatabaseHelper.GetData(@"
                SELECT n.MaNganSach, d.TenDanhMuc, n.HanMuc,
                       ISNULL(t.DaChi, 0) AS DaChi, ISNULL(t.SoGiaoDichLoi, 0) AS SoGiaoDichLoi,
                       COUNT(*) OVER (PARTITION BY n.MaDanhMuc) AS SoBanGhiNganSach
                FROM NganSach n
                INNER JOIN DanhMuc d ON d.MaDanhMuc = n.MaDanhMuc AND d.MaNguoiDung = @UserId
                LEFT JOIN (
                    SELECT g.MaDanhMuc, SUM(g.SoTien) AS DaChi,
                           SUM(CASE WHEN g.SoTien <= 0 THEN 1 ELSE 0 END) AS SoGiaoDichLoi
                    FROM GiaoDich g
                    WHERE g.MaNguoiDung = @UserId
                      AND g.NgayGiaoDich >= @Start AND g.NgayGiaoDich < @End
                    GROUP BY g.MaDanhMuc
                ) t ON t.MaDanhMuc = n.MaDanhMuc
                WHERE n.MaNguoiDung = @UserId AND d.LoaiDanhMuc = N'Chi tiêu'
                  AND n.Thang = @Month AND n.Nam = @Year
                ORDER BY n.MaNganSach", new[] {
                    UserParameter(),
                    new SqlParameter("@Start", SqlDbType.Date) { Value = month },
                    new SqlParameter("@End", SqlDbType.Date) { Value = month.AddMonths(1) },
                    new SqlParameter("@Month", SqlDbType.Int) { Value = month.Month },
                    new SqlParameter("@Year", SqlDbType.Int) { Value = month.Year }
                });
            foreach (DataRow row in budgets.Rows)
            {
                if (snapshot.NganSachCaThang.Count == 100) break;
                decimal limit = Convert.ToDecimal(row["HanMuc"]);
                decimal spent = Convert.ToDecimal(row["DaChi"]);
                bool valid = limit > 0 && Convert.ToInt32(row["SoBanGhiNganSach"]) == 1
                    && Convert.ToInt32(row["SoGiaoDichLoi"]) == 0;
                snapshot.NganSachCaThang.Add(new
                {
                    DanhMuc = Convert.ToString(row["TenDanhMuc"]),
                    HanMuc = limit,
                    DaChi = spent,
                    HopLe = valid,
                    ConLai = valid ? (decimal?)(limit - spent) : null,
                    PhanTramDaDung = valid ? (decimal?)Math.Round(spent / limit * 100, 1) : null,
                    GhiChu = valid ? "" : "Hạn mức, giao dịch không hợp lệ hoặc ngân sách trùng; cần kiểm tra lại dữ liệu."
                });
            }
            snapshot.NganSachDaRutGon = budgets.Rows.Count > 100;

            if (includeGoals)
            {
                var goals = DatabaseHelper.GetData(@"
                    SELECT TOP (31) TenMucTieu, SoTienMucTieu, DaTichLuy, NgayHoanThanh, TrangThai
                    FROM MucTieuTietKiem WHERE MaNguoiDung = @UserId ORDER BY MaMucTieu DESC",
                    new[] { UserParameter() });
                snapshot.MucTieuDaRutGon = goals.Rows.Count > 30;
                foreach (DataRow row in goals.Rows)
                {
                    if (snapshot.MucTieuHienTai.Count == 30) break;
                    decimal target = Convert.ToDecimal(row["SoTienMucTieu"]);
                    decimal saved = row.IsNull("DaTichLuy") ? 0 : Convert.ToDecimal(row["DaTichLuy"]);
                    snapshot.MucTieuHienTai.Add(new
                    {
                        Ten = Convert.ToString(row["TenMucTieu"]),
                        MucTieu = target,
                        DaTichLuy = saved,
                        ConThieu = Math.Max(0, target - saved),
                        PhanTram = target > 0 ? (decimal?)Math.Round(saved / target * 100, 1) : null,
                        Han = row.IsNull("NgayHoanThanh") ? null : Convert.ToDateTime(row["NgayHoanThanh"]).ToString("yyyy-MM-dd"),
                        TrangThai = Convert.ToString(row["TrangThai"])
                    });
                }
            }
            return snapshot;
        }
    }
}
