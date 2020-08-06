using Mediachase.Commerce.Customers;

namespace Vipps.Login.Episerver.Commerce
{
    public class CustomerContactService : ICustomerContactService
    {
        public CustomerContact SaveChanges(CustomerContact contact)
        {
            return contact.SaveChanges();
        }
    }
}