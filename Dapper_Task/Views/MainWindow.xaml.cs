using Dapper_Task.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using Z.Dapper.Plus;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace Dapper_Task.Views;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    #region NotificationSender

    Notifier notifier = new Notifier(cfg =>
    {
        cfg.PositionProvider = new WindowPositionProvider(
            parentWindow: Application.Current.MainWindow,
            corner: Corner.TopRight,
            offsetX: 10,
            offsetY: 10);

        cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
            notificationLifetime: TimeSpan.FromSeconds(2),
            maximumNotificationCount: MaximumNotificationCount.FromCount(5));

        cfg.Dispatcher = Application.Current.Dispatcher;
    });

    #endregion


    readonly string _dbName;
    private string _query;
    SqlConnectionStringBuilder _builder;
    private SqlConnection? _connection;
    private IConfigurationRoot? _configuration;
    private List<Product>? _products;
    private bool _isDbExist;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] String propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


    public List<Product>? Products
    {
        get => _products;
        set
        {
            _products = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsDbExist
    {
        get => _isDbExist;
        set
        {
            _isDbExist = value;
            NotifyPropertyChanged();
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        _dbName = "OnlineShopDb"; // This place is optional. You can give any name to DB
        _builder = new SqlConnectionStringBuilder();
        _query = string.Empty;
        Configuration();
    }


    private async void Configuration()
    {
        _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var conStr = _configuration.GetConnectionString("ConStrDb");

        _connection = new SqlConnection(conStr);

        await CheckDb(conStr);
    }

    private async Task CheckDb(string? conStr)
    {
        _query = @"
DECLARE @isDatabaseExist bit = 0

IF EXISTS(SELECT * FROM sys.databases WHERE NAME = @dbname)
	SET @isDatabaseExist = 1

SELECT @isDatabaseExist";


        IsDbExist = await _connection.ExecuteScalarAsync<bool>(_query, new { dbname = _dbName });


        if (IsDbExist)
        {

            _builder.ConnectionString = conStr;
            _builder["Database"]=_dbName;
            _connection.ConnectionString = _builder.ConnectionString;
        }
    }

    private async void Btn_GenerateDb_Click(object sender, RoutedEventArgs e)
    {
        if (IsDbExist)
        {
            notifier.ShowInformation($"{_dbName} Already Exist !");
            return;
        }

        _query = $@"IF NOT EXISTS(SELECT * FROM sys.databases WHERE NAME = '{_dbName}')
CREATE DATABASE {_dbName}";



        await _connection.ExecuteAsync(_query);



        _query = $@"IF EXISTS(SELECT * FROM sys.databases WHERE NAME = '{_dbName}')
BEGIN
USE {_dbName}
IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Products')
    BEGIN
    CREATE TABLE Products (
        Id int PRIMARY KEY IDENTITY (1, 1),
        Name nvarchar(40) NOT NULL,
        Country nvarchar(40) NULL,
        Price money NOT NULL,
        Quantity int NOT NULL DEFAULT(0)
    );
    IF EXISTS(SELECT * FROM sys.databases WHERE NAME = 'Northwind')
    BEGIN
        INSERT INTO {_dbName}.dbo.Products(Name,Price,Quantity)
        SELECT [ProductName] AS Name
        ,[UnitPrice] AS Price
        ,[UnitsInStock] AS Quantity
        FROM [Northwind].[dbo].[Products]
    END
    END
END";

        await _connection.OpenAsync();

        using (var _transaction = _connection.BeginTransaction())
        {
            try
            {

                await _connection.ExecuteAsync(_query, transaction: _transaction);

                await _transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                notifier.ShowError(ex.Message);
                _transaction?.RollbackAsync();
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }


        _builder.ConnectionString = _connection.ConnectionString;
        _builder["Database"]=_dbName;
        _connection.ConnectionString =_builder.ConnectionString;
        IsDbExist = true;




        notifier.ShowSuccess($"{_dbName} created successfuly!");

    }

    private async void Refresh(object sender, RoutedEventArgs e)
    {

        if (!string.IsNullOrWhiteSpace(SearchTxt.Text))
            _query = $"SELECT * FROM Products WHERE Name LIKE '%{SearchTxt.Text}%'";
        else
            _query = "SELECT * FROM Products";



        var collection = await _connection.QueryAsync<Product>(_query);

        Products= collection.ToList();
    }

    private async void Btn_Search_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SearchTxt.Text))
            return;

        _query = $"SELECT * FROM Products WHERE Name LIKE '%{SearchTxt.Text}%'";

        var collection = await _connection.QueryAsync<Product>(_query);

        Products = collection.ToList();
    }

    private void Btn_Remove_Click(object sender, RoutedEventArgs e)
    {
        DapperPlusManager.Entity<Product>().Table("Products");


        var collection = ProductList.SelectedItems.Cast<Product>().ToList();

        if (collection.Count == 0 || Products is null)
            return;

        _connection.BulkDelete(collection);

        foreach (var item in collection)
            Products.Remove(item);

        Refresh(sender, e);
    }

    private async void ProductList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ProductList.SelectedItem is Product product)
        {
            UpdateView updateWindow = new(product,_connection);

            updateWindow.ShowDialog();


            if (updateWindow.DialogResult == true)
            {
                _query = "UPDATE Products SET Name = @name, Country = @country, Price = @price, Quantity = @quantity WHERE Id = @id";

                await _connection.OpenAsync();

                using (var _transaction = _connection.BeginTransaction())
                {
                    try
                    {

                        await _connection.ExecuteAsync(_query,
                            new { product.Name, product.Country, product.Price, product.Quantity, product.Id },
                            transaction: _transaction);


                        await _transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        notifier.ShowError(ex.Message);
                        _transaction?.RollbackAsync();
                    }
                    finally
                    {
                        await _connection.CloseAsync();
                    }
                }

            }
        }
    }

    private async void Btn_Add_Click(object sender, RoutedEventArgs e)
    {
        AddView addView = new();

        addView.ShowDialog();

        if (addView.DialogResult == true)
        {
            var p = addView.Product;


            _query = "INSERT INTO Products VALUES(@name,@country,@price,@quantity)";
            await _connection.OpenAsync();

            using (var _transaction = _connection.BeginTransaction())
            {
                try
                {
                    _connection.Execute(_query, new { p.Name, p.Country, p.Price, p.Quantity }, transaction: _transaction);
                    await _transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    notifier.ShowError(ex.Message);
                    _transaction?.RollbackAsync();
                }
                finally
                {
                    await _connection.CloseAsync();
                }
            }
            Refresh(sender,e);


        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if(e.Key == Key.Enter)
            Btn_Search_Click(sender, e);
    }


    private async void BtnSearch_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        _query = "SELECT * FROM Products";
        var collection = await _connection.QueryAsync<Product>(_query);

        Products= collection.ToList();
    }
}
