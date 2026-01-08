using Microsoft.AspNetCore.Identity;
/*
 * FILE: ApplicationDbContext.cs
 * PURPOSE: Entity Framework Core database context for managing Identity and User authentication data.
 * COMMUNICATES WITH: SQL Server Database
 */
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HMS.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
}
