using MDK0401Pr2.Entities;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MDK0401Pr2
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private EntitiesDbContext _context;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _context = new EntitiesDbContext();
                LoadProducts();
            } catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProducts()
        {
            try
            {
                var products = _context.Products.ToList();
                var productTypes = _context.ProductType.ToList();
                var productsMaterials = _context.ProductsMaterial.ToList();
                var materials = _context.Materials.ToList();

                var productsDisplay = new List<ProductsDisplay>();

                foreach (var product in products)
                {
                    // Находим тип продукта
                    var productType = productTypes.FirstOrDefault(pt => pt.ID == product.IDProductType);

                    // Находим материалы продукта
                    var productMats = productsMaterials.Where(pm => pm.IDProduct == product.ID).ToList();

                    decimal totalCost = 0;

                    // Рассчитываем стоимость материалов
                    foreach (var pm in productMats)
                    {
                        var material = materials.FirstOrDefault(m => m.ID == pm.IDMaterial);
                        if (material != null && pm.RequredMaterialQuantity > 0)
                        {
                            totalCost += material.UnitPriceOfMaterial * pm.RequredMaterialQuantity;
                        }
                    }

                    // Применяем коэффициент типа продукта
                    if (productType != null && productType.CoefficienProductType > 0)
                    {
                        totalCost *= productType.CoefficienProductType;
                    }

                    totalCost = Math.Max(0, Math.Round(totalCost, 2));

                    productsDisplay.Add(new ProductsDisplay
                    {
                        ID = product.ID,
                        Type = productType?.Type ?? "Не указан",
                        ProductName = product.ProductName,
                        Articul = product.Articul,
                        MinCostForPartner = product.MinCostForPartner,
                        RollWidthInMetr = product.RollWidthInMetr,
                        CalculatedCost = totalCost
                    });
                }
                ProductsListBox.ItemsSource = productsDisplay;

            } 
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки продуктов: {ex.Message}");
            }
        }

        private decimal CalculateProductCost(Products product)
        {
            if (product?.ProductsMaterial == null)
                return 0;

            decimal totalCost = 0;

            foreach (var productMaterial in product.ProductsMaterial)
            {
                if (productMaterial.Materials != null &&
                    productMaterial.RequredMaterialQuantity > 0)
                {
                    decimal materialCost = productMaterial.Materials.UnitPriceOfMaterial
                        * productMaterial.RequredMaterialQuantity;

                    totalCost += materialCost;
                }
            }

            if (product.ProductType != null &&
                    product.ProductType.CoefficienProductType > 0)
            {
                totalCost *= product.ProductType.CoefficienProductType;
            }

            return Math.Max(0, Math.Round(totalCost, 2));
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public class ProductsDisplay
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public string ProductName { get; set; }
        public decimal Articul { get; set; }
        public decimal MinCostForPartner { get; set; }
        public decimal RollWidthInMetr { get; set; }
        public decimal CalculatedCost { get; set; }
    }
}
