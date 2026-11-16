// Nested namespace for enums so they stay separate from entity classes.
namespace Ecommerce.Api.Models.Enumerations;

// An enum is a value type with a fixed set of named constants. The default
// underlying type is int, so each member is stored as an integer in memory and in EF Core.
public enum Roles
{
    // Explicit value 0. A new User whose Role is not set would otherwise also be 0,
    // so User is the zero value and the default role.
    User = 0,

    // Integer 1. Compared in code as Roles.Admin; persisted as the number 1.
    Admin = 1,

    // Integer 2. Compared in code as Roles.Staff; persisted as the number 2.
    Staff = 2
}
