using System;
using System.Collections.Generic;
using System.Globalization;

namespace WpfApp.AI
{
    public sealed class ChatTurn
    {
        public string User { get; set; }
        public string Assistant { get; set; }
    }

    // The AI selects only this fixed contract; it never supplies SQL or a user ID.
    public sealed class QueryPlan
    {
        public string intent { get; set; }
        public string start { get; set; }
        public string end_exclusive { get; set; }
        public string compare_start { get; set; }
        public string compare_end_exclusive { get; set; }
        public bool include_goals { get; set; }
        public string clarification { get; set; }
    }

    public sealed class DateRange
    {
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }

        public DateRange(DateTime start, DateTime end)
        {
            if (start < new DateTime(1900, 1, 1) || end > new DateTime(2101, 1, 1)
                || end <= start || end > start.AddYears(2))
                throw new ArgumentException("Vui lòng chọn khoảng thời gian hợp lệ, tối đa 2 năm mỗi lần.");
            Start = start.Date;
            End = end.Date;
        }

        public static DateRange Parse(string start, string end)
        {
            DateTime a, b;
            if (!DateTime.TryParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out a)
                || !DateTime.TryParseExact(end, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out b))
                throw new ArgumentException("Chưa xác định được ngày. Bạn hãy ghi rõ khoảng ngày hoặc tháng/năm.");
            return new DateRange(a, b);
        }

        public string Label { get { return Start.ToString("dd/MM/yyyy") + " – " + End.AddDays(-1).ToString("dd/MM/yyyy"); } }
    }

    public sealed class FinanceSnapshot
    {
        public string TuNgay { get; set; }
        public string DenNgay { get; set; }
        public string DonVi { get; set; }
        public decimal TongThu { get; set; }
        public decimal TongChi { get; set; }
        public decimal ChenhLechThuChi { get { return TongThu - TongChi; } }
        public int SoGiaoDich { get; set; }
        public List<object> TheoDanhMuc { get; set; }
        public bool DanhMucDaRutGon { get; set; }
        public string ThangNganSach { get; set; }
        public List<object> NganSachCaThang { get; set; }
        public bool NganSachDaRutGon { get; set; }
        public List<object> MucTieuHienTai { get; set; }
        public bool MucTieuDaRutGon { get; set; }
        public bool DaLayMucTieu { get; set; }
        public int GiaoDichKhongHopLe { get; set; }
    }

    public sealed class AssistantReply
    {
        public string Text { get; set; }
        public string ReportBody { get; set; }
        public DateTime? ReportMonth { get; set; }
    }

    public static class AiClock
    {
        // Vietnam has no daylight-saving adjustment. Do not depend on PC time zone.
        public static DateTime Today { get { return DateTime.UtcNow.AddHours(7).Date; } }
    }
}
