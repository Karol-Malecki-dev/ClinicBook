using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    /// <summary>
    /// Role - defines user roles and permission levels in the application
    /// Used to control access to features and administrative functions
    /// </summary>
    public enum UserRole
    {
        /// <summary>Standard user role with limited permissions; can read books and track progress</summary>
        User = 1,

        /// <summary>Administrator role with full system access; manages users, books, and system configuration</summary>
        SystemAdministrator = 2
    }
}
