using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace StockFlowPro.ViewModels.Admin
{
    public class UserRoleViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public List<string> Roles { get; set; }
    }
}
