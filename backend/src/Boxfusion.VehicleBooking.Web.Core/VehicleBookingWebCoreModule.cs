using Abp.Modules;
using Abp.Reflection.Extensions;
using Castle.MicroKernel.Registration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Boxfusion.VehicleBooking.Application;
using Boxfusion.VehicleBooking.Common.Authorization;
using Boxfusion.VehicleBooking.Domain;
using Shesha;
using Shesha.Authentication.JwtBearer;
using Shesha.Authorization;
using Shesha.Configuration;
using Shesha.Configuration.Startup;
using Shesha.Elmah;
using Shesha.Import;
using Shesha.Sms.Clickatell;
using Shesha.Web.FormsDesigner;
using System;
using System.Text;

namespace Boxfusion.VehicleBooking
{
    /// <summary>
    /// ReSharper disable once InconsistentNaming
    /// </summary>
    [DependsOn(
        // Adding all the VehicleBooking Modules
        typeof(SheshaFrameworkModule),
        typeof(SheshaApplicationModule),
        typeof(SheshaFormsDesignerModule),
        typeof(SheshaElmahModule),
        typeof(SheshaImportModule),
        typeof(SheshaClickatellModule),
        typeof(VehicleBookingModule),
        typeof(VehicleBookingApplicationModule)
	 )]
    public class VehicleBookingWebCoreModule : AbpModule
    {
        private readonly IConfigurationRoot _appConfiguration;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="env"></param>
        public VehicleBookingWebCoreModule(IWebHostEnvironment env)
        {
            _appConfiguration = env.GetAppConfiguration();
        }

        /// <summary>
        /// 
        /// </summary>
        public override void PreInitialize()
        {
            var config = Configuration.Modules.ShaNHibernate();
            
            config.UseDbms(c => c.GetDbmsType(), c => c.GetDefaultConnectionString());

            //config.UseMsSql();
            //config.UsePostgreSql();

            ConfigureTokenAuth();
        }

        private void ConfigureTokenAuth()
        {
            IocManager.Register<TokenAuthConfiguration>();
            var tokenAuthConfig = IocManager.Resolve<TokenAuthConfiguration>();

            tokenAuthConfig.SecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_appConfiguration["Authentication:JwtBearer:SecurityKey"]));
            tokenAuthConfig.Issuer = _appConfiguration["Authentication:JwtBearer:Issuer"];
            tokenAuthConfig.Audience = _appConfiguration["Authentication:JwtBearer:Audience"];
            tokenAuthConfig.SigningCredentials = new SigningCredentials(tokenAuthConfig.SecurityKey, SecurityAlgorithms.HmacSha256);
            tokenAuthConfig.Expiration = TimeSpan.FromDays(5);
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(VehicleBookingWebCoreModule).GetAssembly());

            IocManager.IocContainer.Register(
            Component.For<ICustomPermissionChecker>().Forward<IVehicleBookingPermissionChecker>().Forward<VehicleBookingPermissionChecker>().ImplementedBy<VehicleBookingPermissionChecker>().LifestyleTransient()                );
        }
    }
}
