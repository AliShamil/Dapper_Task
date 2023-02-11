using Dapper;
using Dapper_Task.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Dapper_Task.Views
{
    /// <summary>
    /// Interaction logic for UpdateView.xaml
    /// </summary>
    public partial class UpdateView : Window
    {
        public Product Product { get; set; }
        SqlConnection _connection;

        public UpdateView(Product product, SqlConnection connection)
        {
            InitializeComponent();
            DataContext = this;
            _connection = connection;
            Product = product;
        }

        private async void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            var cmd = "SELECT * FROM Products WHERE Id = @id";

           var dbProduct = await _connection.QueryFirstAsync<Product>(cmd,new{Product.Id});
           
            StringBuilder builder = new StringBuilder();

            if (Product.Name != dbProduct.Name)
                builder.Append($"{nameof(Product.Name)}\n");

            if (Product.Price != dbProduct.Price)
                builder.Append($"{nameof(Product.Price)}\n");

            if (Product.Quantity != dbProduct.Quantity)
                builder.Append($"{nameof(Product.Quantity)}\n");

            if (builder.Length > 0)
            {
                builder.Append("These prperites are edited! You must click save button!");
                MessageBox.Show(builder.ToString());
                Btn_Cancel.IsEnabled = false;
                return;
            }
            else
                DialogResult = true;
        }

        private void Btn_Update_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder builder = new StringBuilder();

            if (string.IsNullOrWhiteSpace(Product.Name))
                builder.Append($"{nameof(Product.Name)} Must Be Filled\n");

            if (Product.Price <= 0)
                builder.Append($"{nameof(Product.Price)} Cannot be equal or less than 0\n");

            if (Product.Quantity < 0)
                builder.Append($"{nameof(Product.Quantity)} Cannot be less than 0\n");

            if (builder.Length > 0)
            {
                MessageBox.Show(builder.ToString());
                return;
            }

            DialogResult = true;
        }
    }
}
