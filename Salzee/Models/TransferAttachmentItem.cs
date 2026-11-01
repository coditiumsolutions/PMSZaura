namespace Salzee.Models
{
    /// <summary>
    /// One item in BuyerAttachments or SellerAttachments JSON array.
    /// </summary>
    public class TransferAttachmentItem
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
    }
}
