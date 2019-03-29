using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EFCoreSnapshotIsolation.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace EFCoreSnapshotIsolation
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.Configure<CookiePolicyOptions>(options =>
			{
				// This lambda determines whether user consent for non-essential cookies is needed for a given request.
				options.CheckConsentNeeded = context => true;
				options.MinimumSameSitePolicy = SameSiteMode.None;
			});

			//Attach method to DbContext
			services.AddDbContext<ApplicationDbContext>(SetUpConnection);

			services.AddDefaultIdentity<IdentityUser>()
				.AddEntityFrameworkStores<ApplicationDbContext>();


			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
		}

		/// <summary>
		/// To catch the Connection change state and change the transaction isolation 
		///		when new connection gets initiated
		/// </summary>
		/// <param name="builder"></param>
		private void SetUpConnection(DbContextOptionsBuilder builder)
		{
			string connectionString = Configuration.GetConnectionString("DefaultConnection");
			SqlConnection connection = new SqlConnection(connectionString);

			connection.StateChange += (o, e) =>
			{
				Console.WriteLine($"Changing connection state...!e.OriginalState={e.OriginalState}");
				Console.WriteLine($"Changing connection state...!e.CurrentState={e.CurrentState}");
				var connId = connection.ClientConnectionId;
				Console.WriteLine($"Client Connection ={connId}");
				if (e.OriginalState == System.Data.ConnectionState.Closed && e.CurrentState == System.Data.ConnectionState.Open)
				{
					Console.WriteLine("Setting isoltion level");
					var cmd = connection.CreateCommand();
					cmd.CommandText = "SET TRANSACTION ISOLATION LEVEL SNAPSHOT;";
					cmd.ExecuteNonQuery();
				}
			};

			builder.UseSqlServer(connection);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseDatabaseErrorPage();
			}
			else
			{
				app.UseExceptionHandler("/Error");
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseCookiePolicy();

			app.UseAuthentication();

			app.UseMvc();
		}
	}
}
