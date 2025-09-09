using MDK0401Pr2.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Логика взаимодействия для AddAndEditProductWindow.xaml
    /// </summary>
    public partial class AddAndEditProductWindow : Window
    {
        private EntitiesDbContext _context;
        private Products _editingProduct;
        private bool _isEditMode;

        public AddAndEditProductWindow()
        {
            InitializeComponent();
            _isEditMode = false;
            InitializeWindow();
        }

        public AddAndEditProductWindow(int productId) : this()
        {
            if (productId <= 0)
            {
                throw new ArgumentException("ID продукта должен быть положительным числом", nameof(productId));
            }

            _isEditMode = true;
            _editingProduct = new Products { ID = productId };
        }

        private async void InitializeWindow()
        {
            try
            {
                _context = new EntitiesDbContext();

                var productTypes = await _context.ProductType.ToListAsync();
                CmbProductType.ItemsSource = productTypes;

                Title = _isEditMode ? "Редактирование продукта" : "Добавление продукта";

                if (_isEditMode)
                {
                    await LoadProductData();
                }

            } catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async System.Threading.Tasks.Task LoadProductData()
        {
            try
            {
                _editingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.ID == _editingProduct.ID);

                if (_editingProduct == null)
                {
                    MessageBox.Show("Продукт не найден в базе данных", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                TxtArticul.Text = _editingProduct.Articul.ToString("N2");
                TxtProductName.Text = _editingProduct.ProductName;
                TxtMinCost.Text = _editingProduct.MinCostForPartner.ToString("N2");
                TxtRollWidth.Text = _editingProduct.RollWidthInMetr.ToString("N2");

                if (_editingProduct.IDProductType > 0)
                {
                    var productTypes = CmbProductType.ItemsSource as System.Collections.IList;
                    var selectedType = productTypes?
                        .OfType<ProductType>()
                        .FirstOrDefault(pt => pt.ID == _editingProduct.IDProductType);

                    CmbProductType.SelectedItem = selectedType;
                }
            } catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных продукта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                // Получаем выбранный тип продукта
                var selectedProductType = (ProductType)CmbProductType.SelectedItem;

                if (_isEditMode)
                {
                    // РЕЖИМ РЕДАКТИРОВАНИЯ: обновляем существующий продукт
                    _editingProduct.Articul = decimal.Parse(TxtArticul.Text);
                    _editingProduct.ProductName = TxtProductName.Text.Trim();
                    _editingProduct.MinCostForPartner = decimal.Parse(TxtMinCost.Text);
                    _editingProduct.RollWidthInMetr = decimal.Parse(TxtRollWidth.Text);
                    _editingProduct.IDProductType = selectedProductType.ID;

                    // Помечаем запись как измененную
                    _context.Entry(_editingProduct).State = EntityState.Modified;
                }
                else
                {
                    // РЕЖИМ ДОБАВЛЕНИЯ: создаем новый продукт
                    var newProduct = new Products
                    {
                        Articul = decimal.Parse(TxtArticul.Text),
                        ProductName = TxtProductName.Text.Trim(),
                        MinCostForPartner = Math.Round(decimal.Parse(TxtMinCost.Text), 2),
                        RollWidthInMetr = Math.Round(decimal.Parse(TxtRollWidth.Text), 2),
                        IDProductType = selectedProductType.ID
                    };

                    _context.Products.Add(newProduct);
                }

                // Сохраняем изменения в базе данных
                _context.SaveChangesAsync();

                // Закрываем окно с положительным результатом
                DialogResult = true;
                Close();
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbEx)
            {
                MessageBox.Show($"Ошибка сохранения в базе данных: {dbEx.InnerException?.Message ?? dbEx.Message}",
                    "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Неожиданная ошибка при сохранении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtMinCost_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Валидация: разрешаем только цифры и точку для десятичных чисел
            var textBox = sender as System.Windows.Controls.TextBox;
            var newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            var regex = new Regex(@"^[0-9]*(?:\.[0-9]{0,2})?$");
            e.Handled = !regex.IsMatch(newText);
        }

        private bool ValidateInput()
        {
            // Проверка артикула
            if (string.IsNullOrWhiteSpace(TxtArticul.Text))
            {
                MessageBox.Show("Поле 'Артикул' обязательно для заполнения.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtArticul.Focus();
                return false;
            }

            // Проверка типа продукта
            if (CmbProductType.SelectedItem == null)
            {
                MessageBox.Show("Необходимо выбрать тип продукта из списка.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbProductType.Focus();
                return false;
            }

            // Проверка наименования
            if (string.IsNullOrWhiteSpace(TxtProductName.Text))
            {
                MessageBox.Show("Поле 'Наименование' обязательно для заполнения.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtProductName.Focus();
                return false;
            }

            // Валидация минимальной стоимости
            if (!decimal.TryParse(TxtMinCost.Text, out decimal minCost) || minCost < 0)
            {
                MessageBox.Show("Минимальная стоимость должна быть положительным числом.\n" +
                               "Формат: 123.45 (дробная часть через точку)",
                               "Неверный формат", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtMinCost.Focus();
                return false;
            }

            // Валидация ширины рулона
            if (!decimal.TryParse(TxtRollWidth.Text, out decimal rollWidth) || rollWidth < 0)
            {
                MessageBox.Show("Ширина рулона должна быть положительным числом.\n" +
                               "Формат: 1.50 (дробная часть через точку)",
                               "Неверный формат", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtRollWidth.Focus();
                return false;
            }

            return true;
        }
    }
}
