using MDK0401Pr2.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
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
    /// Логика взаимодействия для MaterialCalculateWindow.xaml
    /// </summary>
    public partial class MaterialCalculateWindow : Window
    {
        private int _productId;
        private string _productName;
        private List<MaterialInfo> _materials;
        private MaterialCalculator _calculator;
        private int _productTypeId;

        public MaterialCalculateWindow(int productId, string productName)
        {
            InitializeComponent();
            _productId = productId;
            _productName = productName;
            _calculator = new MaterialCalculator();
            _materials = new List<MaterialInfo>();
            DataContext = this;
            Loaded += MaterialCalculationWindow_Loaded;
        }

        public string WindowTitle => $"Расчет закупки материалов для: {_productName}";

        private async void MaterialCalculationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SetLoadingState(true);

                using (var context = new EntitiesDbContext())
                {
                    var product = await context.Products
                        .Include("ProductType")
                        .FirstOrDefaultAsync(p => p.ID == _productId);

                    if (product == null)
                    {
                        MessageBox.Show("Продукт не найден в базе данных", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    _productTypeId = product.IDProductType;

                    // Загружаем материалы продукта
                    _materials = _calculator.GetProductMaterials(_productId);

                    if (_materials == null || !_materials.Any())
                    {
                        MessageBox.Show("Для данного продукта не найдены материалы", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        CmbMaterial.IsEnabled = false;
                        BtnCalculate.IsEnabled = false;
                    }
                    else
                    {
                        CmbMaterial.ItemsSource = _materials;
                        CmbMaterial.SelectedIndex = 0;
                    }
                }
            } catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            } finally
            {
                SetLoadingState(false);
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            CmbMaterial.IsEnabled = !isLoading;
            TxtProductionQuantity.IsEnabled = !isLoading;
            TxtParameter1.IsEnabled = !isLoading;
            TxtParameter2.IsEnabled = !isLoading;
            TxtStockQuantity.IsEnabled = !isLoading;
            BtnCalculate.IsEnabled = !isLoading;

            if (isLoading)
            {
                TxtResult.Visibility = Visibility.Collapsed;
                BtnCalculate.Content = "Загрузка...";
            }
            else
            {
                BtnCalculate.Content = "Рассчитать";
            }
        }

        private void IntegerTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            var regex = new Regex(@"^[0-9]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = (TextBox)sender;
            string newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            if (!IsTextValid(newText))
            {
                e.Handled = true;
                return;
            }

            if (HasMultipleDecimalSeparators(newText))
            {
                e.Handled = true;
            }
        }

        private bool IsTextValid(string text)
        {
            // Пустая строка допустима (пользователь может стирать текст)
            if (string.IsNullOrEmpty(text))
                return true;

            // Проверяем, что все символы - цифры или разделители
            foreach (char c in text)
            {
                if (!char.IsDigit(c) && c != ',' && c != '.')
                    return false;
            }

            // Проверяем, что строка представляет valid число
            // Пробуем распарсить с обоими разделителями
            return double.TryParse(text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double result) && result >= 0;
        }

        private bool HasMultipleDecimalSeparators(string text)
        {
            int commaCount = text.Count(c => c == ',');
            int dotCount = text.Count(c => c == '.');

            // Если есть оба разделителя или один из них встречается больше 1 раза
            return (commaCount > 0 && dotCount > 0) || commaCount > 1 || dotCount > 1;
        }

        private void Txt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Запрещаем ввод пробела
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void CmbMaterial_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Скрываем предыдущий результат при смене материала
            TxtResult.Visibility = Visibility.Collapsed;
        }

        private bool ValidateInput()
        {
            // Проверка выбора материала
            if (CmbMaterial.SelectedItem == null)
            {
                MessageBox.Show("Выберите материал для расчета", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbMaterial.Focus();
                return false;
            }

            // Проверка количества продукции
            if (string.IsNullOrWhiteSpace(TxtProductionQuantity.Text))
            {
                MessageBox.Show("Введите количество продукции для производства", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtProductionQuantity.Focus();
                return false;
            }

            if (!int.TryParse(TxtProductionQuantity.Text, out int productionQuantity) || productionQuantity <= 0)
            {
                MessageBox.Show("Количество продукции должно быть положительным целым числом", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                TxtProductionQuantity.Focus();
                return false;
            }

            // Проверка параметра 1
            if (string.IsNullOrWhiteSpace(TxtParameter1.Text))
            {
                MessageBox.Show("Введите значение параметра 1", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtParameter1.Focus();
                return false;
            }

            if (!double.TryParse(TxtParameter1.Text, out double parameter1) || parameter1 <= 0)
            {
                MessageBox.Show("Параметр 1 должен быть положительным числом", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                TxtParameter1.Focus();
                return false;
            }

            // Проверка параметра 2
            if (string.IsNullOrWhiteSpace(TxtParameter2.Text))
            {
                MessageBox.Show("Введите значение параметра 2", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtParameter2.Focus();
                return false;
            }

            if (!double.TryParse(TxtParameter2.Text, out double parameter2) || parameter2 <= 0)
            {
                MessageBox.Show("Параметр 2 должен быть положительным числом", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                TxtParameter2.Focus();
                return false;
            }

            // Проверка количества на складе
            if (string.IsNullOrWhiteSpace(TxtStockQuantity.Text))
            {
                MessageBox.Show("Введите количество материала на складе", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtStockQuantity.Focus();
                return false;
            }

            if (!double.TryParse(TxtStockQuantity.Text, out double stockQuantity) || stockQuantity < 0)
            {
                MessageBox.Show("Количество материала на складе не может быть отрицательным", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStockQuantity.Focus();
                return false;
            }

            return true;
        }

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                var selectedMaterial = CmbMaterial.SelectedItem as MaterialInfo;
                if (selectedMaterial == null)
                {
                    MessageBox.Show("Выберите материал для расчета", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получаем значения из полей ввода
                int productionQuantity = int.Parse(TxtProductionQuantity.Text);
                double parameter1 = double.Parse(TxtParameter1.Text);
                double parameter2 = double.Parse(TxtParameter2.Text);
                double stockQuantity = double.Parse(TxtStockQuantity.Text);

                // Выполняем расчет
                int result = _calculator.CalculateRequiredMatrerial(
                    _productTypeId,
                    selectedMaterial.MaterialTypeId,
                    productionQuantity,
                    parameter1,
                    parameter2,
                    stockQuantity);

                // Обрабатываем результат
                if (result == -1)
                {
                    MessageBox.Show("Ошибка в расчетах. Проверьте правильность введенных данных " +
                                   "и существование типов продукции и материалов в системе.",
                                   "Ошибка расчета", MessageBoxButton.OK, MessageBoxImage.Error);
                    TxtResult.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Формируем информативное сообщение
                    string resultMessage = result == 0
                        ? $"✅ Достаточно материала на складе!\n" +
                          $"Текущего количества ({stockQuantity:N2}) хватает для производства {productionQuantity} единиц продукции."
                        : $"📦 Необходимо закупить: {result} единиц материала\n" +
                          $"📝 Материал: {selectedMaterial.MaterialName}\n" +
                          $"🏭 Для производства: {productionQuantity} единиц продукции\n" +
                          $"📊 На складе: {stockQuantity:N2} единиц";

                    TxtResult.Text = resultMessage;
                    TxtResult.Visibility = Visibility.Visible;
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Неверный формат числовых данных. Убедитесь, что введены корректные числа.",
                    "Ошибка формата", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (OverflowException)
            {
                MessageBox.Show("Введены слишком большие числа. Проверьте значения полей.",
                    "Ошибка переполнения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Неожиданная ошибка при расчете: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Обработчик для кнопки закрытия через крестик
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}
