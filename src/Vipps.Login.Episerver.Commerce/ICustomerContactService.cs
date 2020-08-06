using Mediachase.Commerce.Customers;

namespace Vipps.Login.Episerver.Commerce
{
    public interface ICustomerContactService
    {
        CustomerContact SaveChanges(CustomerContact contact);
    }
}