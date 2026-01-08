/*
 * FILE: ApplicationUser.cs
 * PURPOSE: Extended Identity User model for the application.
 * COMMUNICATES WITH: Identity Framework
 */
using Microsoft.AspNetCore.Identity;

namespace HMS.Web.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
}

