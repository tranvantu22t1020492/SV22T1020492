using SV22T1020492.DataLayers.Interfaces;
using SV22T1020492.DataLayers.SQLServer;
using SV22T1020492.Models.Catalog;
using SV22T1020492.Models.Common;

namespace SV22T1020492.BusinessLayers
{
    /// <summary>
    /// Xử lý nghiệp vụ liên quan đến danh mục hàng hóa
    /// </summary>
    public static class CatalogDataService
    {
        private static readonly IProductRepository productDB;
        private static readonly IGenericRepository<Category> categoryDB;

        static CatalogDataService()
        {
            productDB = new ProductRepository(Configuration.ConnectionString);
            categoryDB = new CategoryRepository(Configuration.ConnectionString);
        }

        #region Product

        public static async Task<PagedResult<Product>> ListProductsAsync(ProductSearchInput input)
        {
            return await productDB.ListAsync(input);
        }

        public static async Task<Product?> GetProductAsync(int productID)
        {
            return await productDB.GetAsync(productID);
        }

        public static async Task<int> AddProductAsync(Product data)
        {
            return await productDB.AddAsync(data);
        }

        public static async Task<bool> UpdateProductAsync(Product data)
        {
            return await productDB.UpdateAsync(data);
        }

        public static async Task<bool> DeleteProductAsync(int productID)
        {
            if (await productDB.IsUsedAsync(productID))
                return false;

            return await productDB.DeleteAsync(productID);
        }

        public static async Task<bool> IsUsedProductAsync(int productID)
        {
            return await productDB.IsUsedAsync(productID);
        }

        #endregion

        #region Category

        public static async Task<PagedResult<Category>> ListCategoriesAsync(PaginationSearchInput input)
        {
            return await categoryDB.ListAsync(input);
        }

        public static async Task<Category?> GetCategoryAsync(int categoryID)
        {
            return await categoryDB.GetAsync(categoryID);
        }

        public static async Task<int> AddCategoryAsync(Category data)
        {
            return await categoryDB.AddAsync(data);
        }

        public static async Task<bool> UpdateCategoryAsync(Category data)
        {
            return await categoryDB.UpdateAsync(data);
        }

        public static async Task<bool> DeleteCategoryAsync(int categoryID)
        {
            if (await categoryDB.IsUsedAsync(categoryID))
                return false;

            return await categoryDB.DeleteAsync(categoryID);
        }

        public static async Task<bool> IsUsedCategoryAsync(int categoryID)
        {
            return await categoryDB.IsUsedAsync(categoryID);
        }

        #endregion

        #region ProductAttribute

        public static async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            return await productDB.ListAttributesAsync(productID);
        }

        public static async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            return await productDB.GetAttributeAsync(attributeID);
        }

        public static async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            return await productDB.AddAttributeAsync(data);
        }

        public static async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            return await productDB.UpdateAttributeAsync(data);
        }

        public static async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            return await productDB.DeleteAttributeAsync(attributeID);
        }

        #endregion

        #region ProductPhoto

        // 🔥 FIX QUAN TRỌNG: phải là ListPhotosAsync (có S)
        public static async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            return await productDB.ListPhotoAsync(productID);
        }

        public static async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            return await productDB.GetPhotoAsync(photoID);
        }

        public static async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            return await productDB.AddPhotoAsync(data);
        }

        public static async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            return await productDB.UpdatePhotoAsync(data);
        }

        public static async Task<bool> DeletePhotoAsync(long photoID)
        {
            return await productDB.DeletePhotoAsync(photoID);
        }

        #endregion
    }
}