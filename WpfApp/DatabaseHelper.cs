using System;
using System.Data;
using System.Data.SqlClient;    // ⚠️ ĐỔI TỪ Microsoft.Data.SqlClient → System.Data.SqlClient

namespace WpfApp
{
    public class DatabaseHelper
    {
        private static string connectionString =
            @"Server=(LocalDB)\MSSQLLocalDB;Database=QuanLyTaiChinhAI;Trusted_Connection=True;";

        // ========== SELECT — không tham số ==========
        public static DataTable GetData(string query)
        {
            return GetData(query, null);
        }

        // ========== SELECT — có tham số ==========
        public static DataTable GetData(string query, SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }

        // ========== INSERT/UPDATE/DELETE — không tham số ==========
        public static int ExecuteQuery(string query)
        {
            return ExecuteQuery(query, null);
        }

        // ========== INSERT/UPDATE/DELETE — có tham số ==========
        public static int ExecuteQuery(string query, SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);

                    return cmd.ExecuteNonQuery();
                }
            }
        }
    }
}