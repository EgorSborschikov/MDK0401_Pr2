using MDK0401Pr2.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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

namespace MDK0401Pr2
{
    /// <summary>
    /// Логика взаимодействия для ProductsMaterialWindow.xaml
    /// </summary>
    public partial class ProductsMaterialWindow : Window
    {
        private int _productId;
        private string _productName;
        private decimal _articul;
        private string _productType;

        public ProductsMaterialWindow(int productId, string productName, decimal articul, string productType)
        {
            InitializeComponent();
            _productId = productId;
            _productName = productName;
            _articul = articul;
            _productType = productType;

            DataContext = this;

            Loaded += ProductMaterialsWindow_Loaded;
        }

        public string WindowTitle => $"Материалы для: {_productName}";
        public string ProductName => _productName;
        public decimal Articul => _articul;
        public string ProductType => _productType;

        private async void ProductMaterialsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new EntitiesDbContext())
                {
                    // Безопасная загрузка без Include
                    var productMaterials = await context.ProductsMaterial
                        .Where(pm => pm.IDProduct == _productId)
                        .ToListAsync();

                    var materialList = new List<MaterialDisplay>();

                    foreach (var pm in productMaterials)
                    {
                        var material = await context.Materials
                            .FirstOrDefaultAsync(m => m.ID == pm.IDMaterial);

                        if (material != null)
                        {
                            var materialType = await context.MaterialType
                                .FirstOrDefaultAsync(mt => mt.ID == material.IDMaterialTYpe); 

                            var unitOfMeasure = await context.UnitsOfMeasurement 
                                .FirstOrDefaultAsync(u => u.ID == material.IDUnitOfMeasurement);

                            materialList.Add(new MaterialDisplay
                            {
                                MaterialName = material.NameMaterial,
                                MaterialType = materialType?.Type ?? "Не указан",
                                UnitOfMeasure = unitOfMeasure?.Unit ?? "не указана",
                                RequiredQuantity = pm.RequredMaterialQuantity,
                                UnitPrice = material.UnitPriceOfMaterial,
                                TotalCost = pm.RequredMaterialQuantity * material.UnitPriceOfMaterial
                            });
                        }
                    }

                    MaterialsGrid.ItemsSource = materialList;

                    if (!materialList.Any())
                    {
                        MessageBox.Show("Для данного продукта не найдены материалы",
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки материалов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var calcWindow = new MaterialCalculateWindow(_productId, _productName);
                calcWindow.Owner = this;
                calcWindow.ShowDialog();
            } catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия калькулятора: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class MaterialDisplay
    {
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string UnitOfMeasure { get; set; }
        public decimal RequiredQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalCost { get; set; }
    }
}
