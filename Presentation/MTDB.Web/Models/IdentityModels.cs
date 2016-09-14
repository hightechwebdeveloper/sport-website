using Microsoft.AspNet.Identity.EntityFramework;
using System;
using MTDB.Core.EntityFramework;

namespace MTDB.Models {

	//public class ApplicationUser : IdentityUser {
	//	public DateTime CreationDate { get; set; }
	//	public Boolean Approved { get; set; }
	//	public DateTime LastActivityDate { get; set; }
	//	public DateTime LastLockoutDate { get; set; }
	//	public DateTime LastLoginDate { get; set; }
	//}

	public class ApplicationDbContext : IdentityDbContext<ApplicationUser> {
		public ApplicationDbContext()
			: base("DefaultConnection") {
		}
	}
}