using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Collections.ObjectModel;


namespace WpfApp
{
    public partial class DanhMuc : Page
    {
        ObservableCollection<DanhMucModel> danhSach;

        public DanhMuc()
        {
            InitializeComponent();

            danhSach = new ObservableCollection<DanhMucModel>
            {
                new DanhMucModel
                {
                    STT = 1,
                    MaDanhMuc = "DM001",
                    TenDanhMuc = "Điện thoại"
                },

                new DanhMucModel
                {
                    STT = 2,
                    MaDanhMuc = "DM002",
                    TenDanhMuc = "Laptop"
                },

                new DanhMucModel
                {
                    STT = 3,
                    MaDanhMuc = "DM003",
                    TenDanhMuc = "Phụ kiện"
                },

                new DanhMucModel
                {
                    STT = 4,
                    MaDanhMuc = "DM004",
                    TenDanhMuc = "Máy tính bảng"
                }
            };

            dataGridDanhMuc.ItemsSource = danhSach;
        }


        private void btnThem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Mở form thêm danh mục",
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }


        private void btnSua_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridDanhMuc.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn danh mục cần sửa!");
                return;
            }

            var item = dataGridDanhMuc.SelectedItem as DanhMucModel;

            MessageBox.Show(
                $"Sửa danh mục: {item.TenDanhMuc}",
                "Thông báo");
        }


        private void btnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridDanhMuc.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn danh mục cần xóa!");
                return;
            }

            var item = dataGridDanhMuc.SelectedItem as DanhMucModel;

            MessageBoxResult result = MessageBox.Show(
                $"Bạn có chắc muốn xóa \"{item.TenDanhMuc}\" không?",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                danhSach.Remove(item);
            }
        }


        private void btnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiem.Clear();
            dataGridDanhMuc.ItemsSource = null;
            dataGridDanhMuc.ItemsSource = danhSach;
        }


        private void txtTimKiem_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            string keyword = txtTimKiem.Text.ToLower();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                dataGridDanhMuc.ItemsSource = danhSach;
                return;
            }

            var ketQua = danhSach.Where(x =>
                x.MaDanhMuc.ToLower().Contains(keyword) ||
                x.TenDanhMuc.ToLower().Contains(keyword)
            ).ToList();

            dataGridDanhMuc.ItemsSource = ketQua;
        }

        private void btnThemDanhMuc_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSuaDanhMuc_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnXoaDanhMuc_Click(object sender, RoutedEventArgs e)
        {

        }
    }


    public class DanhMucModel
    {
        public int STT { get; set; }

        public string MaDanhMuc { get; set; }

        public string TenDanhMuc { get; set; }
    }
}