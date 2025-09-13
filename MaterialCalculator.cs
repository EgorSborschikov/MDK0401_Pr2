using MDK0401Pr2.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace MDK0401Pr2
{
    public class MaterialCalculator
    {
        public int CalculateRequiredMatrerial(
            int productTypeId,
            int materialTypeId,
            int productionQuantity,
            double parameter1,
            double parameter2,
            double stockQuantity)
        {
            if (productionQuantity <= 0 || parameter1 <= 0 || parameter2 <= 0 || stockQuantity < 0)
            {
                return -1;
            }

            try
            {
                using (var context = new EntitiesDbContext())
                {
                    var productType = context.ProductType
                        .FirstOrDefault(pt => pt.ID == productTypeId);

                    if (productType == null || productType.CoefficienProductType <= 0)
                    {
                        return -1;
                    }

                    var materialType = context.MaterialType
                        .FirstOrDefault(mt => mt.ID == materialTypeId);

                    if (materialType == null || materialType.PercentageOfDefect < 0)
                    {
                        return -1;
                    }

                    var material = context.Materials
                        .Include(m => m.UnitsOfMeasurement)
                        .FirstOrDefault(m => m.IDMaterialTYpe == materialTypeId);

                    if (material == null || material.UnitsOfMeasurement == null)
                    {
                        return -1;
                    }

                    string unitName = material.UnitsOfMeasurement.Unit?.ToLower()?.Trim();

                    double materialPerUnit;

                    switch (unitName)
                    {
                        case "кг":
                            materialPerUnit = parameter1 * parameter2 * (double)productType.CoefficienProductType;
                            break;

                        case "л":
                            materialPerUnit = parameter1 * parameter2 * (double)productType.CoefficienProductType;
                            break;

                        case "рул":
                            materialPerUnit = (double)productType.CoefficienProductType;
                            break;

                        default:
                            materialPerUnit = (double)productType.CoefficienProductType;
                            break;
                    }

                    double totalMaterialNeeded = materialPerUnit * productionQuantity;
                    double defectPercentage = (double)materialType.PercentageOfDefect;
                    double totalWithDefect = totalMaterialNeeded * (1.0 + defectPercentage);
                    double materialToPurchase = totalWithDefect - stockQuantity;

                    int result = (int)Math.Ceiling(materialToPurchase);
                    return Math.Max(0, result);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка расчета: {ex.Message}");
                return -1;
            }
        }

        public System.Collections.Generic.List<MaterialInfo> GetProductMaterials(int productId)
        {
            try
            {
                using (var context = new EntitiesDbContext())
                {
                    var materials = (from pm in context.ProductsMaterial
                                     where pm.IDProduct == productId
                                     join material in context.Materials on pm.IDMaterial equals material.ID
                                     join materialType in context.MaterialType on material.IDMaterialTYpe equals materialType.ID 
                                     join unit in context.UnitsOfMeasurement on material.IDUnitOfMeasurement equals unit.ID
                                     where unit.Unit != null
                                     select new MaterialInfo
                                     {
                                         MaterialId = material.ID,
                                         MaterialTypeId = material.IDMaterialTYpe,
                                         MaterialName = material.NameMaterial,
                                         MaterialTypeName = materialType.Type,
                                         UnitName = unit.Unit,
                                         RequiredQuantity = pm.RequredMaterialQuantity
                                     }).ToList();

                    return materials;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки материалов: {ex.Message}");
                return new List<MaterialInfo>();
            }
        }
    }

    public class MaterialInfo
    {
        public int MaterialId { get; set; }
        public int MaterialTypeId { get; set; }
        public string MaterialName { get; set; }
        public string MaterialTypeName { get; set; }
        public string UnitName { get; set; }
        public decimal RequiredQuantity { get; set; }

        public string DisplayText => $"{MaterialName} ({UnitName})";
    }
}
