using Dapper;
using SV22T1020492.DataLayers.Interfaces;
using SV22T1020492.Models.Catalog;
using SV22T1020492.Models.Common;

namespace SV22T1020492.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến mặt hàng trên SQL Server,
    /// bao gồm cả thuộc tính (ProductAttributes) và ảnh (ProductPhotos) của mặt hàng
    /// </summary>
    public class ProductRepository : BaseRepository, IProductRepository
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public ProductRepository(string connectionString) : base(connectionString)
        {
        }

        // ===================== PRODUCT =====================

        /// <summary>
        /// Truy vấn, tìm kiếm và phân trang danh sách mặt hàng
        /// theo tên, loại hàng, nhà cung cấp và khoảng giá
        /// </summary>
        /// <param name="input">Đầu vào tìm kiếm, phân trang</param>
        /// <returns>Kết quả phân trang danh sách mặt hàng</returns>
        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            using var connection = GetConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@searchValue", $"%{input.SearchValue ?? ""}%");

            parameters.Add("@categoryID", input.CategoryID == 0 ? null : input.CategoryID);
            parameters.Add("@supplierID", input.SupplierID == 0 ? null : input.SupplierID);
            parameters.Add("@minPrice", input.MinPrice == 0 ? null : input.MinPrice);
            parameters.Add("@maxPrice", input.MaxPrice == 0 ? null : input.MaxPrice);

            parameters.Add("@pageSize", input.PageSize);
            parameters.Add("@offset", input.Offset);

            var sql = @"SELECT COUNT(*)
                        FROM   Products
                        WHERE  ProductName  LIKE @searchValue
                          AND  (@categoryID IS NULL OR CategoryID = @categoryID)
                          AND  (@supplierID IS NULL OR SupplierID = @supplierID)
                          AND  (@minPrice   IS NULL OR Price >= @minPrice)
                          AND  (@maxPrice   IS NULL OR Price <= @maxPrice);

                        SELECT ProductID, ProductName, ProductDescription, SupplierID, CategoryID,
                               Unit, Price, Photo, IsSelling
                        FROM   Products
                        WHERE  ProductName  LIKE @searchValue
                          AND  (@categoryID IS NULL OR CategoryID = @categoryID)
                          AND  (@supplierID IS NULL OR SupplierID = @supplierID)
                          AND  (@minPrice   IS NULL OR Price >= @minPrice)
                          AND  (@maxPrice   IS NULL OR Price <= @maxPrice)
                        ORDER  BY ProductName
                        OFFSET @offset ROWS
                        FETCH  NEXT @pageSize ROWS ONLY;";

            using var multi = await connection.QueryMultipleAsync(sql, parameters);
            int rowCount = await multi.ReadFirstAsync<int>();
            var data = (await multi.ReadAsync<Product>()).ToList();

            return new PagedResult<Product>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data
            };
        }

        /// <summary>
        /// Lấy thông tin 1 mặt hàng theo mã
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>Thông tin mặt hàng hoặc null nếu không tồn tại</returns>
        public async Task<Product?> GetAsync(int productID)
        {
            using var connection = GetConnection();
            var sql = @"SELECT ProductID, ProductName, ProductDescription, SupplierID, CategoryID,
                               Unit, Price, Photo, IsSelling
                        FROM   Products
                        WHERE  ProductID = @productID";
            return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { productID });
        }

        /// <summary>
        /// Bổ sung một mặt hàng mới vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu mặt hàng cần bổ sung</param>
        /// <returns>Mã mặt hàng vừa được bổ sung</returns>
        public async Task<int> AddAsync(Product data)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO Products (ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling)
                        VALUES (@ProductName, @ProductDescription, @SupplierID, @CategoryID, @Unit, @Price, @Photo, @IsSelling);
                        SELECT SCOPE_IDENTITY();";
            var result = await connection.ExecuteScalarAsync<decimal>(sql, data);
            return (int)result;
        }

        /// <summary>
        /// Cập nhật thông tin mặt hàng trong CSDL
        /// </summary>
        /// <param name="data">Dữ liệu mặt hàng cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công, false nếu không có bản ghi nào được cập nhật</returns>
        public async Task<bool> UpdateAsync(Product data)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE Products
                        SET ProductName        = @ProductName,
                            ProductDescription = @ProductDescription,
                            SupplierID         = @SupplierID,
                            CategoryID         = @CategoryID,
                            Unit               = @Unit,
                            Price              = @Price,
                            Photo              = @Photo,
                            IsSelling          = @IsSelling
                        WHERE ProductID = @ProductID";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa mặt hàng có mã là productID (bao gồm cả thuộc tính và ảnh liên quan)
        /// </summary>
        /// <param name="productID">Mã mặt hàng cần xóa</param>
        /// <returns>true nếu xóa thành công, false nếu không tìm thấy bản ghi</returns>
        public async Task<bool> DeleteAsync(int productID)
        {
            using var connection = GetConnection();
            // Xóa thuộc tính và ảnh trước khi xóa mặt hàng
            var sql = @"DELETE FROM ProductAttributes WHERE ProductID = @productID;
                        DELETE FROM ProductPhotos     WHERE ProductID = @productID;
                        DELETE FROM Products          WHERE ProductID = @productID;";
            int rows = await connection.ExecuteAsync(sql, new { productID });
            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra xem mặt hàng có mã là productID có đang được sử dụng trong đơn hàng không
        /// </summary>
        /// <param name="productID">Mã mặt hàng cần kiểm tra</param>
        /// <returns>true nếu đang được sử dụng, false nếu chưa được sử dụng</returns>
        public async Task<bool> IsUsedAsync(int productID)
        {
            using var connection = GetConnection();
            var sql = "SELECT COUNT(*) FROM OrderDetails WHERE ProductID = @productID";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { productID });
            return count > 0;
        }

        // ===================== PRODUCT ATTRIBUTES =====================

        /// <summary>
        /// Lấy danh sách thuộc tính của một mặt hàng sắp xếp theo thứ tự hiển thị
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>Danh sách thuộc tính của mặt hàng</returns>
        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using var connection = GetConnection();
            var sql = @"SELECT AttributeID, ProductID, AttributeName, AttributeValue, DisplayOrder
                        FROM   ProductAttributes
                        WHERE  ProductID = @productID
                        ORDER  BY DisplayOrder, AttributeName";
            var data = await connection.QueryAsync<ProductAttribute>(sql, new { productID });
            return data.ToList();
        }

        /// <summary>
        /// Lấy thông tin của một thuộc tính theo mã thuộc tính
        /// </summary>
        /// <param name="attributeID">Mã thuộc tính</param>
        /// <returns>Thông tin thuộc tính hoặc null nếu không tồn tại</returns>
        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var connection = GetConnection();
            var sql = @"SELECT AttributeID, ProductID, AttributeName, AttributeValue, DisplayOrder
                        FROM   ProductAttributes
                        WHERE  AttributeID = @attributeID";
            return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { attributeID });
        }

        /// <summary>
        /// Bổ sung một thuộc tính mới cho mặt hàng vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu thuộc tính cần bổ sung</param>
        /// <returns>Mã thuộc tính vừa được bổ sung</returns>
        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO ProductAttributes (ProductID, AttributeName, AttributeValue, DisplayOrder)
                        VALUES (@ProductID, @AttributeName, @AttributeValue, @DisplayOrder);
                        SELECT SCOPE_IDENTITY();";
            var result = await connection.ExecuteScalarAsync<decimal>(sql, data);
            return (long)result;
        }

        /// <summary>
        /// Cập nhật thông tin thuộc tính của mặt hàng trong CSDL
        /// </summary>
        /// <param name="data">Dữ liệu thuộc tính cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công, false nếu không có bản ghi nào được cập nhật</returns>
        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE ProductAttributes
                        SET AttributeName  = @AttributeName,
                            AttributeValue = @AttributeValue,
                            DisplayOrder   = @DisplayOrder
                        WHERE AttributeID = @AttributeID";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa thuộc tính có mã là attributeID
        /// </summary>
        /// <param name="attributeID">Mã thuộc tính cần xóa</param>
        /// <returns>true nếu xóa thành công, false nếu không tìm thấy bản ghi</returns>
        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM ProductAttributes WHERE AttributeID = @attributeID";
            int rows = await connection.ExecuteAsync(sql, new { attributeID });
            return rows > 0;
        }

        // ===================== PRODUCT PHOTOS =====================

        /// <summary>
        /// Lấy danh sách ảnh của một mặt hàng sắp xếp theo thứ tự hiển thị
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>Danh sách ảnh của mặt hàng</returns>
        public async Task<List<ProductPhoto>> ListPhotoAsync(int productID)
        {
            using var connection = GetConnection();
            var sql = @"SELECT PhotoID, ProductID, Photo, Description, DisplayOrder, IsHidden
                        FROM   ProductPhotos
                        WHERE  ProductID = @productID
                        ORDER  BY DisplayOrder, PhotoID";
            var data = await connection.QueryAsync<ProductPhoto>(sql, new { productID });
            return data.ToList();
        }

        /// <summary>
        /// Lấy thông tin 1 ảnh của mặt hàng theo mã ảnh
        /// </summary>
        /// <param name="photoID">Mã ảnh</param>
        /// <returns>Thông tin ảnh hoặc null nếu không tồn tại</returns>
        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using var connection = GetConnection();
            var sql = @"SELECT PhotoID, ProductID, Photo, Description, DisplayOrder, IsHidden
                        FROM   ProductPhotos
                        WHERE  PhotoID = @photoID";
            return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { photoID });
        }

        /// <summary>
        /// Bổ sung một ảnh mới cho mặt hàng vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu ảnh cần bổ sung</param>
        /// <returns>Mã ảnh vừa được bổ sung</returns>
        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO ProductPhotos (ProductID, Photo, Description, DisplayOrder, IsHidden)
                        VALUES (@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);
                        SELECT SCOPE_IDENTITY();";
            var result = await connection.ExecuteScalarAsync<decimal>(sql, data);
            return (long)result;
        }

        /// <summary>
        /// Cập nhật thông tin ảnh của mặt hàng trong CSDL
        /// </summary>
        /// <param name="data">Dữ liệu ảnh cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công, false nếu không có bản ghi nào được cập nhật</returns>
        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE ProductPhotos
                        SET Photo        = @Photo,
                            Description  = @Description,
                            DisplayOrder = @DisplayOrder,
                            IsHidden     = @IsHidden
                        WHERE PhotoID = @PhotoID";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa ảnh có mã là photoID
        /// </summary>
        /// <param name="photoID">Mã ảnh cần xóa</param>
        /// <returns>true nếu xóa thành công, false nếu không tìm thấy bản ghi</returns>
        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM ProductPhotos WHERE PhotoID = @photoID";
            int rows = await connection.ExecuteAsync(sql, new { photoID });
            return rows > 0;
        }
    }
}