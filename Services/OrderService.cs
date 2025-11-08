using System;
using System.Linq;
using System.Threading.Tasks;
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DoAnTotNghiep.Services
{
    public class OrderService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CartService _cartService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(ApplicationDbContext dbContext, CartService cartService, ILogger<OrderService> logger)
        {
            _dbContext = dbContext;
            _cartService = cartService;
            _logger = logger;
        }

        public async Task<(bool Success, string ErrorMessage)> PlaceOrderAsync(Order order, ClaimsPrincipal user)
        {
            var cartItems = _cartService.Items;
            if (!cartItems.Any()) 
            {
                return (false, "Giỏ hàng của bạn đang trống.");
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            order.OrderDate = DateTime.Now;
            order.TotalAmount = _cartService.Total;
            order.Status = "Chờ xác nhận";
            order.UserId = userId;

            var productIds = cartItems.Select(item => item.ProductId).ToList();
            var productsInDb = await _dbContext.Products
                                        .Where(p => productIds.Contains(p.Id))
                                        .ToListAsync();

            foreach (var item in cartItems)
            {
                var productInDb = productsInDb.FirstOrDefault(p => p.Id == item.ProductId);
                if (productInDb == null) { /* ... */ return (false, "..."); }
                if (productInDb.StockQuantity < item.Quantity) { /* ... */ return (false, "..."); }
                
                productInDb.StockQuantity -= item.Quantity;

                // --- GHI LOG KHI BÁN HÀNG (XUẤT KHO) ---
                var log = new InventoryLog
                {
                    ProductId = productInDb.Id,
                    QuantityChange = -item.Quantity, // Ghi số âm vì là xuất kho
                    NewQuantity = productInDb.StockQuantity,
                    Reason = $"Bán hàng cho đơn hàng #{order.Id}" // Sẽ được gán ID sau khi lưu
                };
                await _dbContext.InventoryLogs.AddAsync(log);

                var orderDetail = new OrderDetail { /* ... */ };
                order.OrderDetails.Add(orderDetail);
            }

            try
            {
                await _dbContext.Orders.AddAsync(order);
                await _dbContext.SaveChangesAsync(); // Lưu đơn hàng để có ID

                // Cập nhật lại lý do trong log với ID đơn hàng chính xác
                foreach(var log in _dbContext.ChangeTracker.Entries<InventoryLog>().Where(e => e.State == EntityState.Added).Select(e => e.Entity))
                {
                    if(log.Reason.EndsWith("#"))
                    {
                        log.Reason = $"Bán hàng cho đơn hàng #{order.Id}";
                    }
                }
                await _dbContext.SaveChangesAsync(); // Lưu lại log

                _cartService.ClearCart();
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi lưu đơn hàng vào database.");
                return (false, "Đã có lỗi xảy ra khi lưu đơn hàng. Vui lòng thử lại.");
            }
        }
    }
}

