using Dapper_Task.Models;
using System;
using System.Collections.Generic;
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

    public partial class AddView : Window
    {
        public Product Product { get; set; }

        public AddView()
        {
            InitializeComponent();
            DataContext = this;
            Product = new();
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;


        private void Btn_Add_Click(object sender, RoutedEventArgs e)
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
