using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp // Lưu ý đổi tên namespace nếu project của bạn tên khác
{
    public class DatabaseHelper
    {
        // Chuỗi kết nối đến SQL Server (LocalDB) của bạn
        private static string connectionString = @"Server=(LocalDB)\MSSQLLocalDB;Database=QuanLyTaiChinhAI;Trusted_Connection=True;";

        // Hàm lấy dữ liệu (Dùng cho lệnh SELECT)
        public static DataTable GetData(string query)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }

        // Hàm thực thi dữ liệu (Dùng cho lệnh INSERT, UPDATE, DELETE)
        // Trả về số dòng bị ảnh hưởng
        public static int ExecuteQuery(string query)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    return cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
