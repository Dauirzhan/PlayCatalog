using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Play.Common;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<InventoryItem> inventoryitemsRepository;
        private readonly IRepository<CatalogItem> catalogitemsRepository;

        public ItemsController(IRepository<InventoryItem> inventoryitemsRepository, IRepository<CatalogItem> catalogitemsRepository)
        {
            this.inventoryitemsRepository = inventoryitemsRepository;
            this.catalogitemsRepository = catalogitemsRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var inventoryItemEntities = await inventoryitemsRepository.GetAllAsync(item => item.UserId == userId);
            var itemIds = inventoryItemEntities.Select(inventoryItem => inventoryItem.CatalogItemId);
            var catalogItemEntities = await catalogitemsRepository.GetAllAsync(item => itemIds.Contains(item.Id));

            var inventoryItemDtos = inventoryItemEntities.Select(inventoryItem =>
            {
                var catalogItem = catalogItemEntities.Single(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);
                return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
            });
            return Ok(inventoryItemDtos);
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync(GrantItemsDto grantItemsDto)
        {
            var inventoryItem = await inventoryitemsRepository.GetAsync(item => item.UserId == grantItemsDto.UserId
            && item.CatalogItemId == grantItemsDto.CatalogItemId);

            if (inventoryItem == null)
            {
                inventoryItem = new InventoryItem
                {
                    CatalogItemId = grantItemsDto.CatalogItemId,
                    UserId = grantItemsDto.UserId,
                    Quantity = grantItemsDto.Quantity,
                    AcqiredDate = DateTimeOffset.UtcNow
                };

                await inventoryitemsRepository.CreateAsync(inventoryItem);
            }
            else
            {
                inventoryItem.Quantity += grantItemsDto.Quantity;
                await inventoryitemsRepository.UpdateAsync(inventoryItem);
            }
            return Ok();
        }
    }
}